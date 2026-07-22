using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.DTOs.CardBenefits;
using MoneyTracker.Application.DTOs.Reports;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers.CardBenefits;
using MoneyTracker.Application.Mappers.Reports;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Ai;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Json;
using System.Text.Json;

namespace MoneyTracker.Application.Services
{
    public class ReportService : IReportService
    {
        private static readonly HashSet<PaymentMethodEnum> CardPaymentMethods = new()
        {
            PaymentMethodEnum.CreditCard,
            PaymentMethodEnum.DebitCard,
            PaymentMethodEnum.GiftCard,
            PaymentMethodEnum.googlePay,
            PaymentMethodEnum.applePay
        };

        private readonly ITransactionRepository _transactionRepository;
        private readonly ICardBenefitRepository _cardBenefitRepository;
        private readonly ITimeZoneService _timeZoneService;
        private readonly ILogger<ReportService> _logger;
        private readonly GoogleAiSettings _googleAiSettings;
        private readonly IAiReportCacheRepository _aiReportCacheRepository;
        private readonly IAiRangeReportCacheRepository _aiRangeReportCacheRepository;
        private readonly IAiCallLogRepository _aiCallLogRepository;
        private readonly IAiTrainingDataRepository _aiTrainingDataRepository;

        public ReportService(
            ITransactionRepository transactionRepository,
            ICardBenefitRepository cardBenefitRepository,
            ITimeZoneService timeZoneService,
            ILogger<ReportService> logger,
            IOptions<GoogleAiSettings> googleAiSettings,
            IAiReportCacheRepository aiReportCacheRepository,
            IAiRangeReportCacheRepository aiRangeReportCacheRepository,
            IAiCallLogRepository aiCallLogRepository,
            IAiTrainingDataRepository aiTrainingDataRepository)
        {
            _transactionRepository = transactionRepository;
            _cardBenefitRepository = cardBenefitRepository;
            _timeZoneService = timeZoneService;
            _logger = logger;
            _googleAiSettings = googleAiSettings.Value;
            _aiReportCacheRepository = aiReportCacheRepository;
            _aiRangeReportCacheRepository = aiRangeReportCacheRepository;
            _aiCallLogRepository = aiCallLogRepository;
            _aiTrainingDataRepository = aiTrainingDataRepository;
        }

        public async Task<OperationResult<MonthlyReportDto>> GetMonthlyReportAsync(MonthlyReportFilterDto filter)
        {
            try
            {
                var (fromUtc, toUtc) = GetMonthBoundariesUtc(filter.Year, filter.Month);

                var transactions = await _transactionRepository.GetFilteredAsync(
                    fromUtc, toUtc, accountIds: new List<int>(), transactionTypeIds: new List<int>());

                var benefits = await _cardBenefitRepository.GetAllAsync();
                var activeBenefits = benefits.Where(b => b.IsActive).ToList();

                var expenseTransactions = transactions
                    .Where(t => t.Category?.Type == CategoryTypeEnum.Expense
                             && t.TransferPairId == null
                             && t.Category?.Type != CategoryTypeEnum.Transfer)
                    .ToList();

                var incomeTransactions = transactions
                    .Where(t => t.Category?.Type == CategoryTypeEnum.Income
                             && t.TransferPairId == null
                             && t.Category?.Type != CategoryTypeEnum.Transfer)
                    .ToList();

                var report = new MonthlyReportDto
                {
                    Year = filter.Year,
                    Month = filter.Month,
                    Summary = BuildSummary(expenseTransactions, incomeTransactions),
                    Transactions = transactions
                        .Where(t => t.TransferPairId == null && t.Category?.Type != CategoryTypeEnum.Transfer)
                        .Select(t => t.MapToReportDto(_timeZoneService))
                        .OrderByDescending(t => t.Date)
                        .ToList(),
                    CategorySummary = BuildCategorySummary(expenseTransactions),
                    IncomeCategorySummary = BuildIncomeSummary(incomeTransactions),
                    CardSummary = BuildCardSummary(expenseTransactions, activeBenefits),
                    BenefitRules = activeBenefits.Select(b => b.MapToDto()).ToList(),
                    Recommendations = BuildRecommendations(expenseTransactions, activeBenefits),
                    MerchantSummary = BuildMerchantSummary(expenseTransactions)
                };

                return OperationResult<MonthlyReportDto>.Ok(report, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly report for {Year}/{Month}", filter.Year, filter.Month);
                return OperationResult<MonthlyReportDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<DateRangeReportDto>> GetRangeReportAsync(DateRangeReportFilterDto filter)
        {
            try
            {
                var (fromUtc, toUtc) = GetDateRangeBoundariesUtc(filter.From, filter.To);

                var transactions = await _transactionRepository.GetFilteredAsync(
                    fromUtc, toUtc, accountIds: new List<int>(), transactionTypeIds: new List<int>());

                var filtered = transactions
                    .Where(t => t.TransferPairId == null && t.Category?.Type != CategoryTypeEnum.Transfer)
                    .ToList();

                if (filter.CategoryNames.Count > 0)
                    filtered = filtered
                        .Where(t => filter.CategoryNames.Contains(t.Category?.Name ?? string.Empty))
                        .ToList();

                var expenseTransactions = filtered
                    .Where(t => t.Category?.Type == CategoryTypeEnum.Expense)
                    .ToList();

                var incomeTransactions = filtered
                    .Where(t => t.Category?.Type == CategoryTypeEnum.Income)
                    .ToList();

                var report = new DateRangeReportDto
                {
                    From = filter.From,
                    To = filter.To,
                    Summary = BuildSummary(expenseTransactions, incomeTransactions),
                    Transactions = filtered
                        .Select(t => t.MapToReportDto(_timeZoneService))
                        .OrderByDescending(t => t.Date)
                        .ToList(),
                    CategorySummary = BuildCategorySummary(expenseTransactions),
                    IncomeCategorySummary = BuildIncomeSummary(incomeTransactions)
                };

                return OperationResult<DateRangeReportDto>.Ok(report, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating range report for {From} – {To}", filter.From, filter.To);
                return OperationResult<DateRangeReportDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<string>> GetCachedAiAnalysisAsync(int year, int month)
        {
            var cached = await _aiReportCacheRepository.GetAsync(year, month);
            return cached is not null
                ? OperationResult<string>.Ok(cached.Content)
                : OperationResult<string>.Fail("No cached analysis.");
        }

        public async Task<OperationResult<string>> GetCachedRangeAiAnalysisAsync(DateOnly from, DateOnly to)
        {
            var cached = await _aiRangeReportCacheRepository.GetAsync(from, to);
            return cached is not null
                ? OperationResult<string>.Ok(cached.Content)
                : OperationResult<string>.Fail("No cached analysis.");
        }

        public async Task<OperationResult<string>> GetRangeAiAnalysisAsync(DateRangeReportDto report, bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                var cached = await _aiRangeReportCacheRepository.GetAsync(report.From, report.To);
                if (cached is not null)
                    return OperationResult<string>.Ok(cached.Content);
            }

            var topMerchants = report.Transactions
                .Where(t => t.IsExpense && !string.IsNullOrWhiteSpace(t.Merchant))
                .GroupBy(t => t.Merchant)
                .Select(g => new { merchant = g.Key, total = g.Sum(t => t.Amount), count = g.Count() })
                .OrderByDescending(m => m.total)
                .Take(8);

            var aiPayload = new
            {
                period = $"{report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy}",
                summary = new
                {
                    income = report.Summary.TotalIncome,
                    expenses = report.Summary.TotalExpenses,
                    balance = report.Summary.FinalBalance,
                    transactionCount = report.Summary.TransactionCount
                },
                topExpenseCategories = report.CategorySummary.Take(8).Select(c => new
                {
                    category = c.CategoryName,
                    total = c.TotalSpent,
                    count = c.TransactionCount
                }),
                topIncomeCategories = report.IncomeCategorySummary.Take(5).Select(c => new
                {
                    category = c.CategoryName,
                    total = c.TotalSpent
                }),
                topMerchants,
                unusualTransactions = report.Transactions
                    .OrderByDescending(t => t.Amount)
                    .Take(5)
                    .Select(t => new
                    {
                        date = t.Date.ToString("MM-dd"),
                        name = t.Name,
                        amount = t.Amount,
                        category = t.CategoryName,
                        merchant = t.Merchant
                    })
            };

            var reportJson = JsonSerializer.Serialize(aiPayload, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var prompt = $"""
                Eres un asesor financiero personal. Analiza el siguiente resumen financiero JSON de rango de fechas y proporciona un análisis directo en español con estas secciones:

                1. **Resumen del período**: 2-3 oraciones sobre el panorama general (ingresos, gastos, balance) para el período {report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy}.
                2. **Hábitos de gasto**: Categorías que consumieron más. ¿El patrón es preocupante?
                3. **Comercios frecuentes**: Top 3 comercios donde más se gastó. ¿Alguno merece revisión?
                4. **Movimientos a revisar**: Transacciones inusuales por monto o categoría desconocida.
                5. **3 acciones concretas**: Qué hacer diferente. Sé específico con categorías y montos.

                Reglas:
                - Máximo 350 palabras.
                - No repitas datos del JSON literalmente, interprétalos.

                Datos:
                {reportJson}
                """;

            var result = await CallGoogleAiAsync(prompt, "RangeReport", reportJson, AiServiceType.RangeReport);
            if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
                await _aiRangeReportCacheRepository.UpsertAsync(report.From, report.To, result.Data);
            return result;
        }

        public async Task<OperationResult<string>> GetAiAnalysisAsync(MonthlyReportDto report, bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                var cached = await _aiReportCacheRepository.GetAsync(report.Year, report.Month);
                if (cached is not null)
                    return OperationResult<string>.Ok(cached.Content);
            }

            var aiPayload = new
            {
                period = report.MonthName,
                summary = new
                {
                    income = report.Summary.TotalIncome,
                    expenses = report.Summary.TotalExpenses,
                    balance = report.Summary.FinalBalance,
                    transactionCount = report.Summary.TransactionCount
                },
                topExpenseCategories = report.CategorySummary.Take(8).Select(c => new
                {
                    category = c.CategoryName,
                    total = c.TotalSpent,
                    count = c.TransactionCount
                }),
                topIncomeCategories = report.IncomeCategorySummary.Take(5).Select(c => new
                {
                    category = c.CategoryName,
                    total = c.TotalSpent
                }),
                topMerchants = report.MerchantSummary.Take(10).Select(m => new
                {
                    merchant = m.MerchantName,
                    total = m.TotalSpent,
                    count = m.TransactionCount,
                    category = m.TopCategory
                }),
                cardUsage = report.CardSummary.Select(c => new
                {
                    card = c.CardName,
                    bank = c.BankName,
                    total = c.TotalSpent,
                    estimatedBenefit = c.EstimatedBenefit
                }),
                benefitRules = report.BenefitRules.Where(b => b.IsActive).Take(10).Select(b => new
                {
                    card = b.AccountName,
                    category = b.CategoryName,
                    merchant = b.MerchantPattern,
                    type = b.BenefitType.ToString(),
                    rate = b.BenefitRateFormatted
                }),
                unusualTransactions = report.Transactions
                    .OrderByDescending(t => t.Amount)
                    .Take(5)
                    .Select(t => new
                    {
                        date = t.Date.ToString("MM-dd"),
                        name = t.Name,
                        amount = t.Amount,
                        category = t.CategoryName,
                        merchant = t.Merchant,
                        card = t.CardDisplayName ?? t.CardName
                    })
            };

            var reportJson = JsonSerializer.Serialize(aiPayload, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var prompt = $"""
                Eres un asesor financiero personal. Analiza el siguiente resumen financiero mensual y proporciona un análisis directo en español con estas secciones:

                1. **Resumen del mes**: 2-3 oraciones sobre el panorama general (ingresos, gastos, balance).
                2. **Hábitos de gasto**: Categorías y comercios que consumieron más. ¿El patrón es preocupante?
                3. **Uso de tarjetas**: Basándote en `benefitRules`, ¿usé las tarjetas correctas? Calcula pérdidas concretas si usé la tarjeta equivocada.
                4. **Comercios frecuentes**: Top 3 comercios donde más gasté. ¿Alguno merece revisión o tiene una tarjeta óptima?
                5. **Movimientos a revisar**: Las 5 transacciones más altas — ¿alguna es inusual o duplicada?
                6. **3 acciones concretas**: Qué hacer diferente el próximo mes. Sé específico con nombres de tarjetas y categorías.

                Reglas:
                - Máximo 400 palabras.
                - No repitas datos literalmente, interprétalos.

                Datos:
                {reportJson}
                """;

            var result = await CallGoogleAiAsync(prompt, "MonthlyReport", reportJson, AiServiceType.MonthlyReport);
            if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
                await _aiReportCacheRepository.UpsertAsync(report.Year, report.Month, result.Data);
            return result;
        }

        private async Task<OperationResult<string>> CallGoogleAiAsync(string prompt, string service, string inputJson, AiServiceType serviceType)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                bool useVertexAi = !string.IsNullOrWhiteSpace(_googleAiSettings.ProjectId);

                string url;
                using var http = new HttpClient();

                if (useVertexAi)
                {
                    var location = string.IsNullOrWhiteSpace(_googleAiSettings.Location)
                        ? "us-central1"
                        : _googleAiSettings.Location;

                    var apiHost = location == "global"
                        ? "aiplatform.googleapis.com"
                        : $"{location}-aiplatform.googleapis.com";
                    url = $"https://{apiHost}/v1/projects/{_googleAiSettings.ProjectId}/locations/{location}/publishers/google/models/{_googleAiSettings.Model}:generateContent";

                    GoogleCredential credential;
                    if (!string.IsNullOrWhiteSpace(_googleAiSettings.ServiceAccountJsonPath)
                        && File.Exists(_googleAiSettings.ServiceAccountJsonPath))
                    {
                        credential = GoogleCredential
                            .FromFile(_googleAiSettings.ServiceAccountJsonPath)
                            .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                    }
                    else
                    {
                        credential = (await GoogleCredential.GetApplicationDefaultAsync())
                            .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                    }

                    var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                    http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_googleAiSettings.ApiKey))
                        return OperationResult<string>.Fail("Google AI not configured. Set GoogleAiSettings:ProjectId (Vertex AI) or GoogleAiSettings:ApiKey.");

                    url = $"https://generativelanguage.googleapis.com/v1beta/models/{_googleAiSettings.Model}:generateContent?key={_googleAiSettings.ApiKey}";
                }

                var body = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new { maxOutputTokens = _googleAiSettings.MaxTokens }
                };

                var response = await http.PostAsJsonAsync(url, body);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Google AI API error {Status}: {Body}", (int)response.StatusCode, errorBody);
                    return OperationResult<string>.Fail($"AI API returned {(int)response.StatusCode}. Check logs.");
                }

                var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
                sw.Stop();

                var text = json!.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                var usage = json.RootElement.TryGetProperty("usageMetadata", out var meta) ? meta : (JsonElement?)null;
                var inputTokens = usage?.TryGetProperty("promptTokenCount", out var inp) == true ? inp.GetInt32() : 0;
                var outputTokens = usage?.TryGetProperty("candidatesTokenCount", out var out_) == true ? out_.GetInt32() : 0;
                var cachedTokens = usage?.TryGetProperty("cachedContentTokenCount", out var cch) == true ? cch.GetInt32() : 0;

                _ = _aiCallLogRepository.AddAsync(new AiCallLog
                {
                    Service = service,
                    Model = _googleAiSettings.Model ?? string.Empty,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    CachedTokens = cachedTokens,
                    WasCacheHit = false,
                    DurationMs = sw.ElapsedMilliseconds,
                    CalledAtUtc = DateTime.UtcNow
                });

                _ = _aiTrainingDataRepository.AddAsync(new AiTrainingData
                {
                    ServiceType = serviceType,
                    Model = _googleAiSettings.Model ?? string.Empty,
                    InputType = AiInputType.ReportJsonCompact,
                    RawInput = inputJson,
                    OutputType = AiOutputType.AnalysisMarkdown,
                    RawOutput = text,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    CalledAtUtc = DateTime.UtcNow
                });

                _logger.LogInformation("AI call [{Service}] model={Model} in={Input} out={Output} cached={Cached} ms={Ms}",
                    service, _googleAiSettings.Model, inputTokens, outputTokens, cachedTokens, sw.ElapsedMilliseconds);

                return OperationResult<string>.Ok(text, "Analysis complete.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Error calling Google AI API [{Service}]", service);
                return OperationResult<string>.Fail("AI analysis failed. Check logs for details.");
            }
        }

        private (DateTime fromUtc, DateTime toUtc) GetMonthBoundariesUtc(int year, int month)
        {
            var startLocal = new DateTime(year, month, 1);
            var endLocal = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);
            return (_timeZoneService.ConvertToUtc(startLocal), _timeZoneService.ConvertToUtc(endLocal));
        }

        private (DateTime fromUtc, DateTime toUtc) GetDateRangeBoundariesUtc(DateOnly from, DateOnly to)
        {
            var startLocal = from.ToDateTime(TimeOnly.MinValue);
            var endLocal = to.ToDateTime(new TimeOnly(23, 59, 59));
            return (_timeZoneService.ConvertToUtc(startLocal), _timeZoneService.ConvertToUtc(endLocal));
        }

        private static ReportSummaryDto BuildSummary(
            List<Transaction> expenses,
            List<Transaction> incomes)
        {
            var totalExpenses = expenses.Sum(t => Math.Abs(t.Amount));
            var totalIncome = incomes.Sum(t => Math.Abs(t.Amount));

            var totalCard = expenses
                .Where(t => t.PaymentMethod.HasValue && CardPaymentMethods.Contains(t.PaymentMethod.Value))
                .Sum(t => Math.Abs(t.Amount));

            var totalCash = expenses
                .Where(t => t.PaymentMethod == PaymentMethodEnum.Cash || t.PaymentMethod == PaymentMethodEnum.Check)
                .Sum(t => Math.Abs(t.Amount));

            return new ReportSummaryDto
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                TotalCard = totalCard,
                TotalCash = totalCash,
                TransactionCount = expenses.Count + incomes.Count
            };
        }

        private static List<ReportCategorySummaryDto> BuildCategorySummary(List<Transaction> expenses)
        {
            return expenses
                .GroupBy(t => t.Category?.Name ?? "Uncategorized")
                .Select(g => new ReportCategorySummaryDto
                {
                    CategoryName = g.Key,
                    TotalSpent = g.Sum(t => Math.Abs(t.Amount)),
                    TransactionCount = g.Count(),
                    Icon = Enum.TryParse<CategoryIcon>(g.First().Category?.Icon, out var parsedIcon) ? parsedIcon : default,
                    Color = g.First().Category?.Color
                })
                .OrderByDescending(c => c.TotalSpent)
                .ToList();
        }

        private static List<ReportCategorySummaryDto> BuildIncomeSummary(List<Transaction> incomes)
        {
            return incomes
                .GroupBy(t => t.Category?.Name ?? "Uncategorized")
                .Select(g => new ReportCategorySummaryDto
                {
                    CategoryName = g.Key,
                    TotalSpent = g.Sum(t => Math.Abs(t.Amount)),
                    TransactionCount = g.Count(),
                    Icon = Enum.TryParse<CategoryIcon>(g.First().Category?.Icon, out var parsedIcon) ? parsedIcon : default,
                    Color = g.First().Category?.Color
                })
                .OrderByDescending(c => c.TotalSpent)
                .ToList();
        }

        private static List<ReportCardSummaryDto> BuildCardSummary(
            List<Transaction> expenses,
            List<CardBenefit> activeBenefits)
        {
            return expenses
                .GroupBy(t => t.Account)
                .Where(g => g.Key != null)
                .Select(g =>
                {
                    var account = g.Key!;
                    var totalSpent = g.Sum(t => Math.Abs(t.Amount));
                    var estimatedBenefit = CalculateBenefitForAccount(account.Id, g.ToList(), activeBenefits);

                    return new ReportCardSummaryDto
                    {
                        AccountId = account.Id,
                        AccountName = account.Name.Trim(),
                        CardName = account.CardDisplayName?.Trim() ?? account.Name.Trim(),
                        BankName = account.BankName,
                        TotalSpent = totalSpent,
                        TransactionCount = g.Count(),
                        EstimatedBenefit = estimatedBenefit
                    };
                })
                .OrderByDescending(c => c.TotalSpent)
                .ToList();
        }

        private static decimal CalculateBenefitForAccount(
            int accountId,
            List<Transaction> transactions,
            List<CardBenefit> allBenefits)
        {
            var accountBenefits = allBenefits.Where(b => b.AccountId == accountId).ToList();
            if (!accountBenefits.Any()) return 0;

            decimal totalBenefit = 0;
            var benefitAccumulator = new Dictionary<int, decimal>();

            foreach (var transaction in transactions)
            {
                var matchingBenefit = FindMatchingBenefit(transaction, accountBenefits);
                if (matchingBenefit is null) continue;

                var transactionAmount = Math.Abs(transaction.Amount);
                var rawBenefit = transactionAmount * matchingBenefit.BenefitRate;

                if (matchingBenefit.MaxMonthlyCap.HasValue)
                {
                    benefitAccumulator.TryGetValue(matchingBenefit.Id, out var accumulated);
                    var remaining = matchingBenefit.MaxMonthlyCap.Value - accumulated;
                    if (remaining <= 0) continue;
                    rawBenefit = Math.Min(rawBenefit, remaining);
                    benefitAccumulator[matchingBenefit.Id] = accumulated + rawBenefit;
                }

                totalBenefit += rawBenefit;
            }

            return Math.Round(totalBenefit, 2);
        }

        private static CardBenefit? FindMatchingBenefit(Transaction transaction, List<CardBenefit> benefits)
        {
            return benefits.FirstOrDefault(b =>
                (b.CategoryId.HasValue && b.CategoryId == transaction.CategoryId)
                || (!string.IsNullOrWhiteSpace(b.MerchantPattern)
                    && transaction.Name.Contains(b.MerchantPattern, StringComparison.OrdinalIgnoreCase)));
        }

        private static List<ReportRecommendationDto> BuildRecommendations(
            List<Transaction> expenses,
            List<CardBenefit> activeBenefits)
        {
            var recommendations = new List<ReportRecommendationDto>();
            var missedBenefitByCard = new Dictionary<int, decimal>();

            foreach (var transaction in expenses)
            {
                var usedAccountId = transaction.AccountId;
                var amount = Math.Abs(transaction.Amount);

                var matchingBenefits = activeBenefits
                    .Where(b => (b.CategoryId.HasValue && b.CategoryId == transaction.CategoryId)
                             || (!string.IsNullOrWhiteSpace(b.MerchantPattern)
                                 && transaction.Name.Contains(b.MerchantPattern, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (!matchingBenefits.Any()) continue;

                var bestBenefit = matchingBenefits.MaxBy(b => b.BenefitRate);
                if (bestBenefit is null) continue;

                var usedBenefit = matchingBenefits.FirstOrDefault(b => b.AccountId == usedAccountId);
                var usedRate = usedBenefit?.BenefitRate ?? 0;

                if (bestBenefit.AccountId != usedAccountId && bestBenefit.BenefitRate > usedRate)
                {
                    var missed = amount * (bestBenefit.BenefitRate - usedRate);
                    missedBenefitByCard.TryGetValue(bestBenefit.AccountId, out var existing);
                    missedBenefitByCard[bestBenefit.AccountId] = existing + missed;
                }
            }

            foreach (var (accountId, totalMissed) in missedBenefitByCard.OrderByDescending(x => x.Value))
            {
                var benefit = activeBenefits.First(b => b.AccountId == accountId);
                recommendations.Add(new ReportRecommendationDto
                {
                    Type = RecommendationType.WrongCardUsed,
                    Message = $"Using {benefit.Account?.Name ?? "another card"} for matching purchases could have earned {totalMissed:C2} more in benefits.",
                    EstimatedLoss = Math.Round(totalMissed, 2),
                    SuggestedCard = benefit.Account?.Name
                });
            }

            if (!recommendations.Any() && expenses.Any())
            {
                var topCategory = expenses
                    .GroupBy(t => t.Category?.Name ?? "Unknown")
                    .OrderByDescending(g => g.Sum(t => Math.Abs(t.Amount)))
                    .First();

                recommendations.Add(new ReportRecommendationDto
                {
                    Type = RecommendationType.TopSpendingCategory,
                    Message = $"Your highest spending category was {topCategory.Key} with {topCategory.Sum(t => Math.Abs(t.Amount)):C2}. Consider setting a budget for it.",
                });
            }

            return recommendations;
        }

        private static List<ReportMerchantSummaryDto> BuildMerchantSummary(List<Transaction> expenses)
        {
            return expenses
                .GroupBy(t => t.Merchant?.Name ?? t.Name)
                .Select(g => new ReportMerchantSummaryDto
                {
                    MerchantName = g.Key,
                    TotalSpent = g.Sum(t => Math.Abs(t.Amount)),
                    TransactionCount = g.Count(),
                    TopCategory = g.GroupBy(t => t.Category?.Name ?? "Unknown")
                                   .OrderByDescending(x => x.Count())
                                   .FirstOrDefault()?.Key
                })
                .OrderByDescending(m => m.TotalSpent)
                .Take(15)
                .ToList();
        }
    }
}
