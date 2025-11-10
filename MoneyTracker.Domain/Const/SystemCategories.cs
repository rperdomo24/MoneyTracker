using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Domain.Const
{
    public static class SystemCategories
    {
        // System category IDs (1-3 for income, 4-7 for balance management, 8-13 for transfers)
        public const int SALARY_ID = 1;
        public const int FREELANCE_ID = 2;
        public const int INVESTMENTS_ID = 3;

        // Balance management (existing - mantener)
        public const int INITIAL_BALANCE_INCOME_ID = 4;
        public const int INITIAL_BALANCE_EXPENSE_ID = 5;
        public const int BALANCE_ADJUSTMENT_INCOME_ID = 6;
        public const int BALANCE_ADJUSTMENT_EXPENSE_ID = 7;

        // Transfer 
        public const int TRANSFER_OUT_ID = 8;          // Account transfers
        public const int TRANSFER_IN_ID = 9;
        public const int CREDIT_PAYMENT_ID = 10;       // Credit card payments
        public const int PAYMENT_RECEIVED_ID = 11;
        public const int CREDIT_ADVANCE_ID = 12;       // Credit advances
        public const int ADVANCE_RECEIVED_ID = 13;

        public const string SALARY_NAME = "Salary";
        public const string FREELANCE_NAME = "Freelance";
        public const string INVESTMENTS_NAME = "Investments";

        // System category names and descriptions
        public const string INITIAL_BALANCE_NAME = "Initial Balance";
        public const string BALANCE_ADJUSTMENT_NAME = "Balance Adjustment";
        public const string INITIAL_BALANCE_DESC = "Automatic system transaction when creating account";
        public const string BALANCE_ADJUSTMENT_DESC = "Manual balance adjustment";

        public const string TRANSFER_OUT_NAME = "Transfer Out";
        public const string TRANSFER_IN_NAME = "Transfer In";
        public const string CREDIT_PAYMENT_NAME = "Credit Card Payment";
        public const string PAYMENT_RECEIVED_NAME = "Payment Received";
        public const string CREDIT_ADVANCE_NAME = "Credit Advance";
        public const string ADVANCE_RECEIVED_NAME = "Advance Received";

        // ✅ Helper methods for transfer intelligence
        public static (int fromCategoryId, int toCategoryId) GetTransferCategories(TransferTypeEnum transferType)
        {
            return transferType switch
            {
                TransferTypeEnum.AccountTransfer => (TRANSFER_OUT_ID, TRANSFER_IN_ID),
                TransferTypeEnum.CreditPayment => (CREDIT_PAYMENT_ID, PAYMENT_RECEIVED_ID),
                TransferTypeEnum.CreditAdvance => (CREDIT_ADVANCE_ID, ADVANCE_RECEIVED_ID),
                _ => throw new ArgumentException($"Invalid transfer type: {transferType}")
            };
        }

        public static TransferTypeEnum DetermineTransferType(AccountType fromType, AccountType toType)
        {
            return (fromType, toType) switch
            {
                (_, AccountType.Credit) => TransferTypeEnum.CreditPayment,
                (AccountType.Credit, _) => TransferTypeEnum.CreditAdvance,
                (_, _) => TransferTypeEnum.AccountTransfer
            };
        }

        // ✅ Existing helper methods (mantener)
        public static int GetInitialBalanceCategoryId(bool isIncome)
            => isIncome ? INITIAL_BALANCE_INCOME_ID : INITIAL_BALANCE_EXPENSE_ID;

        public static int GetBalanceAdjustmentCategoryId(bool isIncome)
            => isIncome ? BALANCE_ADJUSTMENT_INCOME_ID : BALANCE_ADJUSTMENT_EXPENSE_ID;

        // ✅ Verification methods
        public static bool IsSystemCategory(int categoryId)
            => categoryId >= 4 && categoryId <= 13;

        public static bool IsTransferCategory(int categoryId)
            => categoryId >= 8 && categoryId <= 13;

        public static bool IsCreditRelatedCategory(int categoryId)
            => categoryId >= 10 && categoryId <= 13;

        public static bool IsTransferOutCategory(int categoryId)
                 => categoryId == TRANSFER_OUT_ID;

        public static bool IsTransferInCategory(int categoryId)
            => categoryId == TRANSFER_IN_ID;
    }
}
