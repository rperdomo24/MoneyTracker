namespace MoneyTracker.UI.Utility.Const
{
    public class StorageKeys
    {
        public static class Filters
        {
            private const string Prefix = "mt:filters";

            public static string AccountTransactions(string userKey, int accountId)
                => $"{Prefix}:{userKey}:account:{accountId}:transactions";
        }
    }
}
