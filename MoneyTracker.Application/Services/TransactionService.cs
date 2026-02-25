using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Common.Extensions;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Application.Mappers.Transactions;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _repository;
        private readonly IAccountRepository _accountRepository;
        private readonly IValidator<TransactionDto> _validator;
        private readonly IValidator<CreateTransferDto> _transferValidator;
        private readonly ILogger<TransactionService> _logger;
        private readonly ITimeZoneService _timeZoneService;
        private readonly ITimeRangeService _timeRangeService;
        private readonly ICategoryService _categoryService;

        public TransactionService(
            ITransactionRepository repository,
            IAccountRepository accountRepository,
            IValidator<TransactionDto> validator,
            IValidator<CreateTransferDto> transferValidator,
            ILogger<TransactionService> logger,
            ITimeZoneService timeZoneService,
            ITimeRangeService timeRangeService,
            ICategoryRepository categoryRepository,
            ICategoryService categoryService)
        {
            _repository = repository;
            _accountRepository = accountRepository;
            _validator = validator;
            _transferValidator = transferValidator;
            _logger = logger;
            _timeZoneService = timeZoneService;
            _timeRangeService = timeRangeService;
            _categoryService = categoryService;
        }

        public async Task<OperationResult<List<TransactionDto>>> GetAllAsync()
        {
            try
            {
                var expenses = await _repository.GetAllAsync();
                var dtoList = expenses.Select(x => x.MapToDto(_timeZoneService)).ToList();
                return OperationResult<List<TransactionDto>>.Ok(dtoList, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<TransactionDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<TransactionDto>> GetByIdAsync(int id)
        {
            try
            {
                var expense = await _repository.GetByIdAsync(id);
                if (expense == null)
                    return OperationResult<TransactionDto>.Fail(OperationMessages.NotFound);

                return OperationResult<TransactionDto>.Ok(expense.MapToDto(_timeZoneService), OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<TransactionDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<TransactionWithPairDto>> GetByIdWithPairAsync(int id)
        {
            try
            {
                var transaction = await _repository.GetByIdAsync(id);
                if (transaction == null)
                    return OperationResult<TransactionWithPairDto>.Fail(OperationMessages.NotFound);

                // Load pair if exists
                Transaction? paired = null;
                if (transaction.TransferPairId.HasValue)
                {
                    paired = await _repository.GetByIdAsync(transaction.TransferPairId.Value);
                }

                // Use mapper
                var result = transaction.MapToTransactionWithPair(paired, _timeZoneService);

                return OperationResult<TransactionWithPairDto>.Ok(result, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<TransactionWithPairDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> CreateAsync(TransactionDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            try
            {
                if (dto.Category is null)
                {
                    var categoryResult = await _categoryService.GetByIdAsync(dto.CategoryId);
                    if (categoryResult.Success)
                        dto.Category = categoryResult.Data;
                }

                var transaction = dto.MapToEntity(_timeZoneService);
                transaction.CreatedAt = _timeZoneService.GetNowInUtc();

                await _repository.AddAsync(transaction);

                var balanceResult = await UpdateAccountBalanceOnlyAsync(transaction.AccountId, transaction.Amount);
                if (!balanceResult.Success)
                    return OperationResult<bool>.Fail(balanceResult.Message ?? "Error updating account balance");

                return OperationResult<bool>.Ok(true, OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> CreateTransferAsync(CreateTransferDto dto)
        {
            var validation = await _transferValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            try
            {
                var fromAccount = await _accountRepository.GetByIdAsync(dto.FromAccountId);
                if (fromAccount is null)
                    return OperationResult<bool>.Fail(OperationMessages.TransferSourceNotFound);

                var toAccount = await _accountRepository.GetByIdAsync(dto.ToAccountId);
                if (toAccount is null)
                    return OperationResult<bool>.Fail(OperationMessages.TransferDestinationNotFound);

                var (fromCategoryId, toCategoryId) = SystemCategories.GetTransferCategoriesByAccountType(
                    fromAccount.Type,
                    toAccount.Type);

                var fromAccountDto = fromAccount.MapToDto();
                var toAccountDto = toAccount.MapToDto();

                var (fromTransaction, toTransaction) = dto.MapToTransferPair(
                    fromCategoryId,
                    toCategoryId,
                    fromAccountDto.Name,
                    toAccountDto.Name,
                    _timeZoneService);

                var fromId = await _repository.AddAndReturnIdAsync(fromTransaction);

                toTransaction.TransferPairId = fromId;
                var toId = await _repository.AddAndReturnIdAsync(toTransaction);

                fromTransaction.TransferPairId = toId;
                await _repository.UpdateAsync(fromTransaction);

                var fromBalanceResult = await UpdateAccountBalanceOnlyAsync(fromTransaction.AccountId, fromTransaction.Amount);
                if (!fromBalanceResult.Success)
                    return OperationResult<bool>.Fail(fromBalanceResult.Message ?? "Error updating source account balance");

                var toBalanceResult = await UpdateAccountBalanceOnlyAsync(toTransaction.AccountId, toTransaction.Amount);
                if (!toBalanceResult.Success)
                    return OperationResult<bool>.Fail(toBalanceResult.Message ?? "Error updating destination account balance");

                return OperationResult<bool>.Ok(true, OperationMessages.TransferCreated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> UpdateAsync(TransactionDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return OperationResult<bool>.Fail(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            try
            {
                var existing = await _repository.GetByIdAsync(dto.Id);
                if (existing is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                var revertResult = await UpdateAccountBalanceOnlyAsync(existing.AccountId, -existing.Amount);
                if (!revertResult.Success)
                    return OperationResult<bool>.Fail(revertResult.Message ?? "Error reverting account balance");

                TransactionMapper.UpdateEntity(existing, dto, _timeZoneService);
                await _repository.UpdateAsync(existing);

                var applyResult = await UpdateAccountBalanceOnlyAsync(existing.AccountId, existing.Amount);
                if (!applyResult.Success)
                    return OperationResult<bool>.Fail(applyResult.Message ?? "Error applying account balance");

                return OperationResult<bool>.Ok(true, OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> UpdateTransferAsync(UpdateTransferDto dto)
        {
            if (dto.Amount <= 0)
                return OperationResult<bool>.Fail(ValidationMessages.GreaterThanZero);

            try
            {
                var transaction = await _repository.GetByIdAsync(dto.TransactionId);
                if (transaction is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                if (!transaction.TransferPairId.HasValue)
                    return OperationResult<bool>.Fail(OperationMessages.TransferNotValid);

                var paired = await _repository.GetByIdAsync(transaction.TransferPairId.Value);
                if (paired is null)
                    return OperationResult<bool>.Fail(OperationMessages.TransferPairNotFound);

                var revertFirst = await UpdateAccountBalanceOnlyAsync(transaction.AccountId, -transaction.Amount);
                if (!revertFirst.Success)
                    return OperationResult<bool>.Fail(revertFirst.Message ?? "Error reverting source account balance");

                var revertSecond = await UpdateAccountBalanceOnlyAsync(paired.AccountId, -paired.Amount);
                if (!revertSecond.Success)
                    return OperationResult<bool>.Fail(revertSecond.Message ?? "Error reverting destination account balance");

                TransferMapper.UpdateTransferPair(transaction, paired, dto, _timeZoneService);

                await _repository.UpdateAsync(transaction);
                await _repository.UpdateAsync(paired);

                var applyFirst = await UpdateAccountBalanceOnlyAsync(transaction.AccountId, transaction.Amount);
                if (!applyFirst.Success)
                    return OperationResult<bool>.Fail(applyFirst.Message ?? "Error applying source account balance");

                var applySecond = await UpdateAccountBalanceOnlyAsync(paired.AccountId, paired.Amount);
                if (!applySecond.Success)
                    return OperationResult<bool>.Fail(applySecond.Message ?? "Error applying destination account balance");

                _logger.LogInformation(
                    "Transfer updated: Transaction {Id1} and paired {Id2}, Amount: {Amount}",
                    transaction.Id, paired.Id, dto.Amount);

                return OperationResult<bool>.Ok(true, OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> DeleteAsync(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(id);
                if (existing is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                await _repository.DeleteAsync(id);

                var revertResult = await UpdateAccountBalanceOnlyAsync(existing.AccountId, -existing.Amount);
                if (!revertResult.Success)
                    return OperationResult<bool>.Fail(revertResult.Message ?? "Error reverting account balance");

                return OperationResult<bool>.Ok(true, OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> DeleteTransferAsync(int transactionId)
        {
            try
            {
                var transaction = await _repository.GetByIdAsync(transactionId);
                if (transaction is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                if (!transaction.TransferPairId.HasValue)
                    return OperationResult<bool>.Fail(OperationMessages.TransferNotValid);

                var paired = await _repository.GetByIdAsync(transaction.TransferPairId.Value);
                if (paired is null)
                    return OperationResult<bool>.Fail(OperationMessages.TransferPairNotFound);

                await _repository.DeleteAsync(transaction.Id);
                await _repository.DeleteAsync(paired.Id);

                var revertFirst = await UpdateAccountBalanceOnlyAsync(transaction.AccountId, -transaction.Amount);
                if (!revertFirst.Success)
                    return OperationResult<bool>.Fail(revertFirst.Message ?? "Error reverting source account balance");

                var revertSecond = await UpdateAccountBalanceOnlyAsync(paired.AccountId, -paired.Amount);
                if (!revertSecond.Success)
                    return OperationResult<bool>.Fail(revertSecond.Message ?? "Error reverting destination account balance");

                _logger.LogInformation(
                    "Transfer deleted: Transaction {Id1} and paired {Id2}",
                    transaction.Id, paired.Id);

                return OperationResult<bool>.Ok(true, OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<int>> DuplicateTransactionAsync(int transactionId)
        {
            try
            {
                var original = await _repository.GetByIdAsync(transactionId);
                if (original == null)
                    return OperationResult<int>.Fail(OperationMessages.NotFound);

                // Validate: do NOT duplicate credit payments
                if (SystemCategories.IsCreditRelatedCategory(original.CategoryId))
                {
                    return OperationResult<int>.Fail(OperationMessages.CreditPaymentCannotDuplicate);
                }

                // If it's a transfer, duplicate using mapper
                if (original.TransferPairId.HasValue)
                {
                    var paired = await _repository.GetByIdAsync(original.TransferPairId.Value);
                    if (paired == null)
                        return OperationResult<int>.Fail(OperationMessages.TransferPairNotFound);

                    // Determine FROM and TO
                    var isOutgoing = original.Category?.Type == CategoryTypeEnum.Expense;
                    var fromTransaction = isOutgoing ? original : paired;
                    var toTransaction = isOutgoing ? paired : original;

                    // Use mapper to create DTO
                    var transferDto = TransferMapper.MapToDuplicateTransferDto(
                        fromTransaction,
                        toTransaction,
                        _timeZoneService);

                    var result = await CreateTransferAsync(transferDto);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Transfer duplicated: Original {OriginalId} (paired {PairedId})",
                            original.Id, paired.Id);
                    }

                    return result.Success
                        ? OperationResult<int>.Ok(0, OperationMessages.TransferDuplicated)
                        : OperationResult<int>.Fail(result.Message ?? OperationMessages.UnexpectedError);
                }

                // Duplicate regular transaction using mapper
                var duplicate = original.MapToDuplicate(_timeZoneService);
                int newId = await _repository.AddAndReturnIdAsync(duplicate);

                _logger.LogInformation(
                    "Transaction duplicated: Original {OriginalId} -> New {NewId}",
                    transactionId, newId);

                return OperationResult<int>.Ok(newId, OperationMessages.TransactionDuplicated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<int>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<CategoryStatsDto>> GetCategoryStatsAsync(int categoryId)
        {
            try
            {
                var result = await _repository.GetAllAsync();

                var categoryTransactions = result
                    .Where(t => t.CategoryId == categoryId)
                    .ToList();

                if (!categoryTransactions.Any())
                {
                    return OperationResult<CategoryStatsDto>.Ok(new CategoryStatsDto(), "No transactions found.");
                }

                var thisMonthAmount = categoryTransactions
                    .Where(t => t.Date.IsThisMonth())
                    .Sum(t => t.Amount);

                var dto = new CategoryStatsDto
                {
                    TotalCount = categoryTransactions.Count,
                    ThisMonthAmount = thisMonthAmount,
                    AverageAmount = categoryTransactions.Select(t => t.Amount).DefaultIfEmpty().Average()
                };

                return OperationResult<CategoryStatsDto>.Ok(dto, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<CategoryStatsDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<TransactionSummaryDto>> GetFilteredAsync(TransactionFilterDto filter)
        {
            try
            {
                var (fromDateUtc, toDateUtc) = _timeRangeService.GetDateRangeUtc(
                    filter.TimePeriod,
                    filter.FromDate,
                    filter.ToDate
                );

                // Call repository with UTC dates
                var transactions = await _repository.GetFilteredAsync(
                    fromDateUtc,
                    toDateUtc,
                    filter.AccountIds,
                    filter.TransactionTypeIds
                );

                // Convert entities to DTOs (dates converted from UTC -> local here)
                var transactionDtos = transactions.Select(x => x.MapToDto(_timeZoneService)).ToList();

                // Apply additional filters at Application Layer
                transactionDtos = ApplyApplicationFilters(transactionDtos, filter);

                // Calculate stats based on filtered data (exclude transfers)
                var totalIncome = transactionDtos
                    .Where(t => t.IsIncome())
                    .Sum(t => t.Amount);

                var totalExpense = transactionDtos
                    .Where(t => t.IsExpense())
                    .Sum(t => t.Amount);

                var summary = new TransactionSummaryDto
                {
                    Transactions = transactionDtos,
                    TotalIncome = totalIncome,
                    TotalExpense = totalExpense,
                    Balance = totalIncome - Math.Abs(totalExpense),
                    TotalCount = transactionDtos.Count
                };

                return OperationResult<TransactionSummaryDto>.Ok(summary, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<TransactionSummaryDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        private List<TransactionDto> ApplyApplicationFilters(List<TransactionDto> transactions, TransactionFilterDto filter)
        {
            var filtered = transactions.AsEnumerable();

            // Text search filter
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var searchLower = filter.SearchText.ToLower();
                filtered = filtered.Where(t =>
                    t.Name.ToLower().Contains(searchLower) ||
                    (!string.IsNullOrWhiteSpace(t.Description) && t.Description.ToLower().Contains(searchLower)));
            }

            // Category filter
            if (filter.CategoryId.HasValue)
            {
                filtered = filtered.Where(t => t.CategoryId == filter.CategoryId.Value);
            }

            // Category type filter
            if (filter.Type.HasValue)
            {
                filtered = filtered.Where(t => t.Category?.Type == filter.Type.Value);
            }

            return filtered
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToList();
        }

        public async Task<OperationResult<List<TransactionDto>>> GetByCategoryForMonthAsync(int categoryId, int year, int month)
        {
            try
            {
                var localStart = new DateTime(year, month, 1);
                var localEnd = localStart.AddMonths(1).AddTicks(-1);

                var fromUtc = _timeZoneService.ConvertToUtc(localStart);
                var toUtc = _timeZoneService.ConvertToUtc(localEnd);

                var transactions = await _repository
                    .GetByCategoryTreeAsync(categoryId, fromUtc, toUtc);

                var dto = transactions
                    .Select(t => t.MapToDto(_timeZoneService))
                    .ToList();

                return OperationResult<List<TransactionDto>>
                    .Ok(dto, OperationMessages.DataRetrieved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<TransactionDto>>
                    .Fail(OperationMessages.UnexpectedError);
            }
        }

        private async Task<OperationResult> UpdateAccountBalanceOnlyAsync(int accountId, decimal delta)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account is null)
                return OperationResult.Fail(OperationMessages.NotFound);

            account.Balance += delta;

            var updated = await _accountRepository.UpdateAsync(account);
            if (!updated)
                return OperationResult.Fail("Error updating account balance");

            return OperationResult.Ok(OperationMessages.Updated);
        }

        public async Task<OperationResult<decimal>> GetAccountBalanceAsync(int accountId)
        {
            try
            {
                var balance = await _repository.GetAccountBalanceAsync(accountId);
                return OperationResult<decimal>.Ok(balance, "Balance calculated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<decimal>.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}