namespace MoneyTracker.Domain.Enums
{
    public enum EmailImportStatus
    {
        Pending = 1,
        Imported = 2,
        Dismissed = 3,
        Ignored = 4,
        NotTransaction = 5,
        Duplicate = 6
    }
}
