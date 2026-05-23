namespace MoneyTracker.UI.Utility.Const
{
    public static class StorageKeys
    {
        public static class Filters
        {
            private const string Prefix = "mt:filters";
            private const string DefaultUserKey = "default"; // temporal

            private static string NormalizeUserKey(string? userKey)
                => string.IsNullOrWhiteSpace(userKey) ? DefaultUserKey : userKey.Trim();

            public static string Transactions(string? userKey)
                => $"{Prefix}:{NormalizeUserKey(userKey)}:transactions";

            public static string AccountTransactions(string? userKey, int accountId)
                => $"{Prefix}:{NormalizeUserKey(userKey)}:account:{accountId}:transactions";

            public static string Budgets(string userKey)
                => $"{Prefix}:{userKey}:budgets";

            public static string Dashboard(string userKey)
                => $"{Prefix}:{userKey}:dashboard";

            public static string AccountsPanel(string userKey)
                => $"mt:{userKey}:panels:accounts:is_open";
        }
    }
}
