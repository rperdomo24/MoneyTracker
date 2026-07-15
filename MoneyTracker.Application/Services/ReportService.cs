using Microsoft.Extensions.Configuration;
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
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Transaction;
using MoneyTracker.Domain.Interfaces;
using OpenAI;
using OpenAI.Chat;
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
        private readonly OpenAiSettings _openAiSettings;
        private readonly GoogleAiSettings _googleAiSettings;
        private readonly string _activeProvider;

        public ReportService(
            ITransactionRepository transactionRepository,
            ICardBenefitRepository cardBenefitRepository,
            ITimeZoneService timeZoneService,
            ILogger<ReportService> logger,
            IOptions<OpenAiSettings> openAiSettings,
            IOptions<GoogleAiSettings> googleAiSettings,
            IConfiguration configuration)
        {
            _transactionRepository = transactionRepository;
            _cardBenefitRepository = cardBenefitRepository;
            _timeZoneService = timeZoneService;
            _logger = logger;
            _openAiSettings = openAiSettings.Value;
            _googleAiSettings = googleAiSettings.Value;
            _activeProvider = configuration["AiProvider"] ?? "Google";
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

        public async Task<OperationResult<string>> GetAiAnalysisAsync(MonthlyReportDto report)
        {
            var reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var prompt = $"""
                Eres un asesor financiero personal. Analiza el siguiente reporte mensual JSON y proporciona un análisis directo en español con estas secciones:

                1. **Resumen del mes**: 2-3 oraciones sobre el panorama general (ingresos, gastos, balance).
                2. **Hábitos de gasto**: Categorías y comercios que consumieron más. ¿El patrón es preocupante?
                3. **Uso de tarjetas**: Basándote en `benefitRules`, ¿usé las tarjetas correctas? Calcula pérdidas concretas si usé la tarjeta equivocada. Si `cardDisplayName` está disponible, úsalo en lugar del nombre de cuenta.
                4. **Comercios frecuentes**: Top 3 comercios donde más gasté. ¿Alguno merece revisión o tiene una tarjeta óptima?
                5. **Movimientos a revisar**: Transacciones inusuales por monto, categoría o comercio desconocido que merezcan atención.
                6. **3 acciones concretas**: Qué hacer diferente el próximo mes. Sé específico con nombres de tarjetas y categorías.

                Reglas:
                - Máximo 400 palabras.
                - No repitas datos del JSON literalmente, interprétalos.
                - Si `cardDisplayName` existe en una transacción, úsalo en vez del campo `accountName`.
                - Usa `merchantSummary` para el análisis de comercios.
                - Detecta si algún gasto en `merchant` parece duplicado o inusualmente alto.

                Reporte:
                {reportJson}
                """;

            return _activeProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
                ? await CallOpenAiAsync(prompt)
                : await CallGoogleAiAsync(prompt);
        }

        private async Task<OperationResult<string>> CallOpenAiAsync(string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_openAiSettings.ApiKey))
                    return OperationResult<string>.Fail("OpenAI not configured. Add OpenAiSettings:ApiKey to appsettings.");

                var client = new OpenAIClient(_openAiSettings.ApiKey);
                var chatClient = client.GetChatClient(_openAiSettings.Model);

                var completion = await chatClient.CompleteChatAsync(
                    new List<ChatMessage> { new UserChatMessage(prompt) },
                    new ChatCompletionOptions { MaxOutputTokenCount = _openAiSettings.MaxTokens });

                return OperationResult<string>.Ok(completion.Value.Content[0].Text, "Analysis complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
                return OperationResult<string>.Fail("AI analysis failed. Check logs for details.");
            }
        }

        private async Task<OperationResult<string>> CallGoogleAiAsync(string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_googleAiSettings.ApiKey))
                    return OperationResult<string>.Fail("Google AI not configured. Add GoogleAiSettings:ApiKey to appsettings.");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_googleAiSettings.Model}:generateContent?key={_googleAiSettings.ApiKey}";

                var body = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new { maxOutputTokens = _googleAiSettings.MaxTokens }
                };

                using var http = new HttpClient();
                var response = await http.PostAsJsonAsync(url, body);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Google AI API error {Status}: {Body}", (int)response.StatusCode, errorBody);
                    return OperationResult<string>.Fail($"AI API returned {(int)response.StatusCode}. Check logs.");
                }

                var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
                var text = json!.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                return OperationResult<string>.Ok(text, "Analysis complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Google AI API");
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
