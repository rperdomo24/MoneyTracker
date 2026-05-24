namespace MoneyTracker.Application.Constants
{
    public static class ServiceMessages
    {
        public const string AccountUpdateError = "Error updating account";
        public const string AccountDeleteError = "Error deleting account";
        public const string AccountCreateError = "Error creating account";
        public const string AccountAdjustNoChanges = "No changes in balance";
        public const string AccountBalanceCalculatedError = "Error calculating balance";
        public const string AccountBalanceSyncError = "Error syncing account balance";
        public const string RelatedAccountBalanceSyncError = "Error syncing related account balances";
        public const string AccountBalanceAdjusted = "Balance adjusted by {0}{1:C2}";
        public const string AccountSynced = "Account balance synced successfully";
        public const string AllAccountsSynced = "All {0} account balance(s) synced successfully";
        public const string AllAccountsSyncedPartial = "{0} account(s) synced. Failed: {1}";

        public const string DashboardOverviewError = "Error retrieving dashboard overview.";
        public const string DashboardWidgetsError = "Error retrieving dashboard widgets.";
        public const string DashboardSummaryError = "Error retrieving dashboard summary.";
        public const string DashboardAlertsError = "Error retrieving alerts.";
        public const string DashboardAccountActivityError = "Error retrieving account activity.";
        public const string DashboardMonthlyComparisonError = "Error retrieving monthly comparison.";
    }
}
