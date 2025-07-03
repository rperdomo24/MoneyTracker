namespace MoneyTracker.Domain.Enums.Transaction
{
    public enum TransferTypeEnum
    {
        AccountTransfer = 1,    // Checking → Savings (neutral patrimonio)
        CreditPayment = 2,      // Checking → Credit Card (mejora patrimonio)
        CreditAdvance = 3       // Credit Card → Checking (empeora patrimonio)
    }
}
