using MoneyTracker.Application.DTOs;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Account;

namespace MoneyTracker.Application.Mappers
{
    public static class AccountMapper
    {
        public static AccountDto MapToDto(this Account entity)
        {
            return new AccountDto
            {
                Id = entity.Id,
                Name = entity.Name,
                CurrentBalance = entity.Balance,
                CreditLimit = entity.CreditLimit,
                Notes = entity.Notes,
                Icon = Enum.TryParse<AccountIcon>(entity.Icon, out var parsedIcon) ? parsedIcon : AccountIcon.Wallet,
                Color = entity.Color,
                Type = entity.Type,
                BankName = entity.BankName,
                CardDisplayName = entity.CardDisplayName,
                IncludeInNetWorth = entity.IncludeInNetWorth,
                CutDay = entity.CutDay,
                PaymentDay = entity.PaymentDay,
                AnnualInterestRate = entity.AnnualInterestRate
            };
        }

        public static Account MapToEntity(this AccountDto dto)
        {
            return new Account
            {
                Id = dto.Id,
                Name = dto.Name,
                Balance = dto.CurrentBalance,
                CreditLimit = dto.CreditLimit,
                Notes = dto.Notes,
                Icon = dto.Icon.ToString(),
                Color = dto.Color,
                Type = dto.Type,
                BankName = dto.BankName,
                CardDisplayName = dto.CardDisplayName,
                IncludeInNetWorth = dto.IncludeInNetWorth,
                CutDay = dto.CutDay,
                PaymentDay = dto.PaymentDay,
                AnnualInterestRate = dto.AnnualInterestRate
            };
        }

        public static void UpdateEntity(this Account entity, AccountDto dto)
        {
            entity.Name = dto.Name;
            //entity.Balance = dto.CurrentBalance;
            entity.CreditLimit = dto.CreditLimit;
            entity.Notes = dto.Notes;
            entity.Icon = dto.Icon.ToString();
            entity.Color = dto.Color;
            entity.Type = dto.Type;
            entity.BankName = dto.BankName;
            entity.CardDisplayName = dto.CardDisplayName;
            entity.IncludeInNetWorth = dto.IncludeInNetWorth;
            entity.CutDay = dto.CutDay;
            entity.PaymentDay = dto.PaymentDay;
            entity.AnnualInterestRate = dto.AnnualInterestRate;
        }
    }
}
