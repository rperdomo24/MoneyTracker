using System.ComponentModel;

namespace MoneyTracker.Domain.Enums.Transaction
{
    public enum TransactionTypeEnum
    {
        [Description("Income")]
        Income,

        [Description("Expense")]
        Expense,

        [Description("Transfer")]
        Transfer,

        [Description("Credit Payment")]
        CreditPayment,
    }
}