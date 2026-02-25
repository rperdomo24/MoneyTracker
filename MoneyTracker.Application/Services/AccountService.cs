using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Application.Mappers.Transactions;
using MoneyTracker.Domain.Const;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;
        private readonly ITransactionService _transactionService;
        private readonly ITimeZoneService _timeZoneService;

        public AccountService(
            IAccountRepository repository,
            ITransactionService transactionService,
            ITimeZoneService timeZoneService)
        {
            _repository = repository;
            _transactionService = transactionService;
            _timeZoneService = timeZoneService;
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
                return OperationResult.Fail("Error updating account");

            return OperationResult.Ok(OperationMessages.Updated);
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
                return OperationResult.Fail(OperationMessages.NotFound);

            var success = await _repository.DeleteAsync(id);
            if (!success)
                return OperationResult.Fail("Error deleting account");

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
                    return OperationResult.Fail("Error creating account");

                // ✅ Create initial balance transaction if needed
                if (initialBalance != 0)
                {
                    await CreateInitialBalanceTransactionAsync(entity.Id, initialBalance, dto.Name);
                }

                return OperationResult.Ok(OperationMessages.Created);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Error creating account: {ex.Message}");
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
                    return OperationResult.Ok("No changes in balance");

                await CreateBalanceAdjustmentTransactionAsync(accountId, adjustment, account.Name, reason);

                var sign = adjustment > 0 ? "+" : "";
                return OperationResult.Ok($"Balance adjusted by {sign}{adjustment:C2}");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Error adjusting balance: {ex.Message}");
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
                return OperationResult<decimal>.Fail($"Error calculating balance: {ex.Message}");
            }
        }

        private async Task CreateInitialBalanceTransactionAsync(int accountId, decimal amount, string accountName)
        {
            var isIncome = amount >= 0;
            var categoryId = SystemCategories.GetInitialBalanceCategoryId(isIncome);
            var transactionType = SystemCategories.GetSystemCategoryTypeName(categoryId);

            var transactionDto = new TransactionDto
            {
                Name = transactionType.ToString(),
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
            var categoryId = SystemCategories.GetBalanceAdjustmentCategoryId(isIncome);
            var transactionType = SystemCategories.GetSystemCategoryTypeName(categoryId);

            var transactionDto = new TransactionDto
            {
                Name = transactionType.ToString(),
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
                return OperationResult.Fail(balanceResult.Message ?? "Error calculating balance");

            account.Balance = balanceResult.Data;

            var updated = await _repository.UpdateAsync(account);
            if (!updated)
                return OperationResult.Fail("Error syncing account balance");

            return OperationResult.Ok("Account balance synced successfully");
        }

    }
}