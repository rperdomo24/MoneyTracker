using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Application.Mappers.Transactions;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Application.Constants;
using Microsoft.Extensions.Logging;

namespace MoneyTracker.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ITransactionService _transactionService;
        private readonly ITimeZoneService _timeZoneService;
        private readonly ISystemCategoryResolver _systemCategoryResolver;
        private readonly ILogger<AccountService>? _logger;
        private readonly IErrorLogService? _errorLogService;

        public AccountService(
            IAccountRepository repository,
            ITransactionRepository transactionRepository,
            ITransactionService transactionService,
            ITimeZoneService timeZoneService,
            ISystemCategoryResolver systemCategoryResolver,
            ILogger<AccountService>? logger = null,
            IErrorLogService? errorLogService = null)
        {
            _repository = repository;
            _transactionRepository = transactionRepository;
            _transactionService = transactionService;
            _timeZoneService = timeZoneService;
            _systemCategoryResolver = systemCategoryResolver;
            _logger = logger;
            _errorLogService = errorLogService;
        }

        public async Task<OperationResult<List<AccountDto>>> GetAllAsync()
        {
            var accounts = await _repository.GetAllAsync();
            List<AccountDto> result = accounts.Select(x => x.MapToDto()).OrderBy(accounts => accounts.Type).ToList();
            return OperationResult<List<AccountDto>>.Ok(result, OperationMessages.DataRetrieved);
        }

        public async Task<OperationResult<List<AccountDto>>> GetAccountsWithBalancesAsync()
        {
            var accounts = await _repository.GetAllAsync();

            var result = accounts
                .Select(a => a.MapToDto())
                .ToList();

            return OperationResult<List<AccountDto>>.Ok(result, OperationMessages.DataRetrieved);
        }

        public async Task<OperationResult<AccountDto>> GetByIdAsync(int id)
        {
            var account = await _repository.GetByIdAsync(id);
            if (account is null)
                return OperationResult<AccountDto>.Fail(OperationMessages.NotFound);

            return OperationResult<AccountDto>.Ok(account.MapToDto(), OperationMessages.DataRetrieved);
        }

        public async Task<OperationResult> UpdateAsync(AccountDto dto)
        {
            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing is null)
                return OperationResult.Fail(OperationMessages.NotFound);

            // ✅ Use mapper for updates
            existing.UpdateEntity(dto);

            var success = await _repository.UpdateAsync(existing);
            if (!success)
                return OperationResult.Fail(ServiceMessages.AccountUpdateError);

            return OperationResult.Ok(OperationMessages.Updated);
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
                return OperationResult.Fail(OperationMessages.NotFound);

            var affectedAccountIds = await _transactionRepository.SoftDeleteByAccountAsync(id);
            foreach (var affectedAccountId in affectedAccountIds.Where(accountId => accountId != id))
            {
                var account = await _repository.GetByIdAsync(affectedAccountId);
                if (account is null)
                    continue;

                var recalculatedBalance = await _transactionService.GetAccountBalanceAsync(affectedAccountId);
                if (!recalculatedBalance.Success)
                    return OperationResult.Fail(recalculatedBalance.Message ?? ServiceMessages.RelatedAccountBalanceSyncError);

                account.Balance = recalculatedBalance.Data;

                var updated = await _repository.UpdateAsync(account);
                if (!updated)
                    return OperationResult.Fail(ServiceMessages.RelatedAccountBalanceSyncError);
            }

            var success = await _repository.DeleteAsync(id);
            if (!success)
                return OperationResult.Fail(ServiceMessages.AccountDeleteError);

            return OperationResult.Ok(OperationMessages.Deleted);
        }

        public async Task<OperationResult> CreateAsync(AccountDto dto)
        {
            return await CreateWithInitialBalanceAsync(dto, 0);
        }

        public async Task<OperationResult> CreateWithInitialBalanceAsync(AccountDto dto, decimal initialBalance)
        {
            var exists = (await _repository.GetAllAsync()).Any(x => x.Name == dto.Name);
            if (exists)
                return OperationResult.Fail(OperationMessages.DuplicateName);

            try
            {
                // ✅ Use mapper to create entity
                var entity = dto.MapToEntity();

                var success = await _repository.AddAsync(entity);
                if (!success)
                    return OperationResult.Fail(ServiceMessages.AccountCreateError);

                // ✅ Create initial balance transaction if needed
                if (initialBalance != 0)
                {
                    await CreateInitialBalanceTransactionAsync(entity.Id, initialBalance, dto.Name);
                }

                return OperationResult.Ok(OperationMessages.Created);
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync(ex, OperationMessages.UnexpectedError, ServiceMessages.AccountCreateError);
            }
        }

        public async Task<OperationResult> AdjustBalanceAsync(int accountId, decimal newBalance, string reason = "")
        {
            try
            {
                var account = await _repository.GetByIdAsync(accountId);
                if (account is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                var currentBalance = account.Balance;
                var adjustment = newBalance - currentBalance;

                if (adjustment == 0)
                    return OperationResult.Ok(ServiceMessages.AccountAdjustNoChanges);

                await CreateBalanceAdjustmentTransactionAsync(accountId, adjustment, account.Name, reason);

                var sign = adjustment > 0 ? "+" : "";
                return OperationResult.Ok(string.Format(ServiceMessages.AccountBalanceAdjusted, sign, adjustment));
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync(ex, OperationMessages.UnexpectedError, "Error adjusting account balance.");
            }
        }

        private async Task<OperationResult<decimal>> GetCurrentBalanceAsync(int accountId)
        {
            try
            {
                return await _transactionService.GetAccountBalanceAsync(accountId);
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<decimal>(ex, OperationMessages.UnexpectedError, ServiceMessages.AccountBalanceCalculatedError);
            }
        }

        private async Task CreateInitialBalanceTransactionAsync(int accountId, decimal amount, string accountName)
        {
            var isIncome = amount >= 0;
            var categoryId = await _systemCategoryResolver.GetInitialBalanceCategoryIdAsync(isIncome);
            var code = SystemCategoryCodes.GetInitialBalanceCode(isIncome);
            var transactionType = SystemCategoryCodes.GetDisplayName(code);

            var transactionDto = new TransactionDto
            {
                Name = transactionType,
                AccountId = accountId,
                Amount = Math.Abs(amount),
                CategoryId = categoryId,
                Date = _timeZoneService.GetLocalTimeInConfiguredTimeZone(),
                Description = $"{SystemCategoryNames.INITIAL_BALANCE_NAME} - {accountName}",
                CreatedAt = _timeZoneService.GetLocalTimeInConfiguredTimeZone(),
                UpdatedAt = _timeZoneService.GetLocalTimeInConfiguredTimeZone()
            };

            await _transactionService.CreateAsync(transactionDto);
        }

        private async Task CreateBalanceAdjustmentTransactionAsync(int accountId, decimal adjustment, string accountName, string reason)
        {
            var isIncome = adjustment >= 0;
            var categoryId = await _systemCategoryResolver.GetBalanceAdjustmentCategoryIdAsync(isIncome);
            var code = SystemCategoryCodes.GetBalanceAdjustmentCode(isIncome);
            var transactionType = SystemCategoryCodes.GetDisplayName(code);

            var transactionDto = new TransactionDto
            {
                Name = transactionType,
                AccountId = accountId,
                Amount = isIncome ? Math.Abs(adjustment) : -Math.Abs(adjustment),
                CategoryId = categoryId,
                Date = _timeZoneService.GetLocalTimeInConfiguredTimeZone(),
                Description = $"{SystemCategoryNames.BALANCE_ADJUSTMENT_NAME} - {accountName}",
                CreatedAt = _timeZoneService.GetLocalTimeInConfiguredTimeZone(),
                UpdatedAt = _timeZoneService.GetLocalTimeInConfiguredTimeZone()
            };

            if (!string.IsNullOrEmpty(reason))
            {
                // transactionDto.Notes = reason;
            }

            await _transactionService.CreateAsync(transactionDto);
        }

        public async Task<OperationResult<bool>> HasAccountByType(AccountType accountType)
        {
            bool hasAccounts = await _repository.HasAccountsByTypeAsync(accountType);

            return OperationResult<bool>.Ok(hasAccounts,
                hasAccounts ? "Accounts found" : "No accounts found of this type");
        }

        public async Task<OperationResult> SyncBalanceAsync(int accountId)
        {
            var account = await _repository.GetByIdAsync(accountId);
            if (account is null)
                return OperationResult.Fail(OperationMessages.NotFound);

            var balanceResult = await GetCurrentBalanceAsync(accountId);
            if (!balanceResult.Success)
                return OperationResult.Fail(balanceResult.Message ?? ServiceMessages.AccountBalanceCalculatedError);

            account.Balance = balanceResult.Data;

            var updated = await _repository.UpdateAsync(account);
            if (!updated)
                return OperationResult.Fail(ServiceMessages.AccountBalanceSyncError);

            return OperationResult.Ok(ServiceMessages.AccountSynced);
        }

        public async Task<OperationResult<int>> SyncAllBalancesAsync()
        {
            try
            {
                var accounts = await _repository.GetAllAsync();
                var failed = new List<string>();
                int synced = 0;
                foreach (var account in accounts)
                {
                    var balanceResult = await GetCurrentBalanceAsync(account.Id);
                    if (!balanceResult.Success) { failed.Add(account.Name); continue; }
                    account.Balance = balanceResult.Data;
                    var updated = await _repository.UpdateAsync(account);
                    if (updated) synced++; else failed.Add(account.Name);
                }
                var message = failed.Count == 0
                    ? string.Format(ServiceMessages.AllAccountsSynced, synced)
                    : string.Format(ServiceMessages.AllAccountsSyncedPartial, synced, string.Join(", ", failed));
                return OperationResult<int>.Ok(synced, message);
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<int>(ex, OperationMessages.UnexpectedError, "Error syncing all account balances.");
            }
        }

        public async Task<OperationResult<bool>> ToggleNetWorthAsync(int id)
        {
            try
            {
                var account = await _repository.GetByIdAsync(id);
                if (account is null)
                    return OperationResult<bool>.Fail(OperationMessages.NotFound);

                account.IncludeInNetWorth = !account.IncludeInNetWorth;
                await _repository.UpdateAsync(account);
                return OperationResult<bool>.Ok(account.IncludeInNetWorth,
                    account.IncludeInNetWorth ? "Included in net worth." : "Excluded from net worth.");
            }
            catch (Exception ex)
            {
                return await FailWithLoggedExceptionAsync<bool>(ex, OperationMessages.UnexpectedError, "Error toggling net worth for account.");
            }
        }

        private async Task<OperationResult> FailWithLoggedExceptionAsync(Exception ex, string failMessage, string logContext)
        {
            _logger?.LogError(ex, logContext);
            if (_errorLogService is not null)
            {
                await _errorLogService.LogExceptionAsync(ex, logContext);
            }

            return OperationResult.Fail(failMessage);
        }

        private async Task<OperationResult<T>> FailWithLoggedExceptionAsync<T>(Exception ex, string failMessage, string logContext)
        {
            _logger?.LogError(ex, logContext);
            if (_errorLogService is not null)
            {
                await _errorLogService.LogExceptionAsync(ex, logContext);
            }

            return OperationResult<T>.Fail(failMessage);
        }

    }
}
