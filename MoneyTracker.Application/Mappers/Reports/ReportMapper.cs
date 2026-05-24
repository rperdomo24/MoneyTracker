using MoneyTracker.Application.DTOs.Reports;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Category;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.Mappers.Reports
{
    public static class ReportMapper
    {
        public static ReportTransactionDto MapToReportDto(this Transaction entity, ITimeZoneService timeZoneService)
        {
            var paymentMethod = entity.PaymentMethod ?? InferPaymentMethod(entity.Account);
            var bankName = entity.Account?.BankName ?? InferBankName(entity.Account?.Name ?? string.Empty);
            var accountName = (entity.Account?.Name ?? string.Empty).Trim();
            var cardName = entity.Account?.CardDisplayName?.Trim() ?? accountName;

            return new ReportTransactionDto
            {
                Id = entity.Id,
                Date = timeZoneService.ConvertFromUtc(entity.Date),
                Name = entity.Name.Trim(),
                Merchant = entity.Merchant?.Name ?? entity.Name.Trim(),
                Description = entity.Description,
                CategoryName = entity.Category?.Name ?? string.Empty,
                Amount = entity.Amount,
                IsExpense = entity.Category?.Type == CategoryTypeEnum.Expense,
                PaymentMethod = paymentMethod,
                AccountName = accountName,
                CardName = cardName,
                BankName = bankName,
                CardDisplayName = entity.Account?.CardDisplayName?.Trim()
            };
        }

        private static PaymentMethodEnum? InferPaymentMethod(Account? account)
        {
            if (account is null) return null;

            return account.Type switch
            {
                AccountType.Credit => PaymentMethodEnum.CreditCard,
                AccountType.Cash => PaymentMethodEnum.Cash,
                AccountType.Bank or AccountType.Checking or AccountType.Savings => PaymentMethodEnum.DebitCard,
                _ => InferPaymentMethodFromName(account.Name)
            };
        }

        private static PaymentMethodEnum? InferPaymentMethodFromName(string name)
        {
            var upper = name.ToUpperInvariant();
            if (upper.Contains("AMEX") || upper.Contains("VISA") || upper.Contains("MASTERCARD") || upper.Contains("CREDIT"))
                return PaymentMethodEnum.CreditCard;
            if (upper.Contains("CASH") || upper.Contains("EFECTIVO"))
                return PaymentMethodEnum.Cash;
            return null;
        }

        internal static string? InferBankName(string accountName)
        {
            var upper = accountName.ToUpperInvariant();
            if (upper.Contains("BAC")) return "BAC Credomatic";
            if (upper.Contains("CUSCA")) return "Banco Cuscatlán";
            if (upper.Contains("AGRICOLA") || upper.Contains("AGRÍCOLA")) return "Banco Agrícola";
            if (upper.Contains("DAVIVIENDA")) return "Davivienda";
            if (upper.Contains("BANPRO")) return "Banpro";
            if (upper.Contains("BCR") || upper.Contains("BANCO NACIONAL")) return "Banco Nacional";
            return null;
        }
    }
}
