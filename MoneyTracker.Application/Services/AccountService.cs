using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;

        public AccountService(IAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<List<AccountDto>>> GetAllAsync()
        {
            var accounts = await _repository.GetAllAsync();
            var result = accounts.Select(x => x.MapToDto()).ToList();
            return OperationResult<List<AccountDto>>.Ok(result, OperationMessages.DataRetrieved);
        }

        public async Task<OperationResult<AccountDto>> GetByIdAsync(int id)
        {
            var account = await _repository.GetByIdAsync(id);
            if (account is null)
                return OperationResult<AccountDto>.Fail(OperationMessages.NotFound);

            return OperationResult<AccountDto>.Ok(account.MapToDto(), OperationMessages.DataRetrieved);
        }

        public async Task<OperationResult> CreateAsync(AccountDto dto)
        {
            var exists = (await _repository.GetAllAsync()).Any(x => x.Name == dto.Name);
            if (exists)
                return OperationResult.Fail(OperationMessages.DuplicateName);

            var entity = dto.MapToEntity();
            await _repository.AddAsync(entity);

            return OperationResult.Ok(OperationMessages.Created);
        }

        public async Task<OperationResult> UpdateAsync(AccountDto dto)
        {
            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing is null)
                return OperationResult.Fail(OperationMessages.NotFound);

            existing.Name = dto.Name;
            existing.Type = dto.Type;
            existing.Balance = dto.Balance;
            existing.CreditLimit = dto.CreditLimit;
            existing.Color = dto.Color;
            existing.Notes = dto.Notes;

            await _repository.UpdateAsync(existing);
            return OperationResult.Ok(OperationMessages.Updated);
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
                return OperationResult.Fail(OperationMessages.NotFound);

            await _repository.DeleteAsync(id);
            return OperationResult.Ok(OperationMessages.Deleted);
        }

    }
}
