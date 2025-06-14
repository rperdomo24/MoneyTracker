namespace MoneyTracker.Domain.Const
{
    public static class SystemCategories
    {
        public const int INITIAL_BALANCE_INCOME_ID = 4;
        public const int INITIAL_BALANCE_EXPENSE_ID = 5;

        public const int BALANCE_ADJUSTMENT_INCOME_ID = 6;
        public const int BALANCE_ADJUSTMENT_EXPENSE_ID = 7;

        public const string INITIAL_BALANCE_NAME = "Initial Balance";
        public const string BALANCE_ADJUSTMENT_NAME = "Balance Adjustment";

        public const string INITIAL_BALANCE_DESC = "Automatic system transaction when creating account";
        public const string BALANCE_ADJUSTMENT_DESC = "Manual balance adjustment";

        public static int GetInitialBalanceCategoryId(bool isIncome)
            => isIncome ? INITIAL_BALANCE_INCOME_ID : INITIAL_BALANCE_EXPENSE_ID;

        public static int GetBalanceAdjustmentCategoryId(bool isIncome)
            => isIncome ? BALANCE_ADJUSTMENT_INCOME_ID : BALANCE_ADJUSTMENT_EXPENSE_ID;
    }
}
