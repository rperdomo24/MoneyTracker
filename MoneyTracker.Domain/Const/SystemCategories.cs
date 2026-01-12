using MoneyTracker.Domain.Enums.Account;

namespace MoneyTracker.Domain.Const
{
    public static class SystemCategories
    {
        // System category IDs (1-10 for system, 11+ for user)

        // Balance management (1-4)
        public const int INITIAL_BALANCE_INCOME_ID = 1;
        public const int INITIAL_BALANCE_EXPENSE_ID = 2;
        public const int BALANCE_ADJUSTMENT_INCOME_ID = 3;
        public const int BALANCE_ADJUSTMENT_EXPENSE_ID = 4;

        // Transfer (5-6)
        public const int TRANSFER_OUT_ID = 5;
        public const int TRANSFER_IN_ID = 6;

        // Credit Payment (7-8)
        public const int CREDIT_PAYMENT_ID = 7;
        public const int PAYMENT_RECEIVED_ID = 8;

        // Credit Advance (9-10)
        public const int CREDIT_ADVANCE_ID = 9;
        public const int ADVANCE_RECEIVED_ID = 10;

        // ✅ NEW: Helper method based on AccountType instead of TransferTypeEnum
        public static (int fromCategoryId, int toCategoryId) GetTransferCategoriesByAccountType(
            AccountType fromType,
            AccountType toType)
        {
            return (fromType, toType) switch
            {
                // Payment TO credit card
                (_, AccountType.Credit) => (CREDIT_PAYMENT_ID, PAYMENT_RECEIVED_ID),

                // Advance FROM credit card
                (AccountType.Credit, _) => (CREDIT_ADVANCE_ID, ADVANCE_RECEIVED_ID),

                // Regular transfer between accounts
                _ => (TRANSFER_OUT_ID, TRANSFER_IN_ID)
            };
        }

        // ✅ NEW: Determine transfer type by category IDs
        public static string GetTransferTypeName(int fromCategoryId, int toCategoryId)
        {
            return (fromCategoryId, toCategoryId) switch
            {
                (TRANSFER_OUT_ID, TRANSFER_IN_ID) => "Account Transfer",
                (CREDIT_PAYMENT_ID, PAYMENT_RECEIVED_ID) => "Credit Payment",
                (CREDIT_ADVANCE_ID, ADVANCE_RECEIVED_ID) => "Credit Advance",
                _ => "Unknown Transfer"
            };
        }

        public static string GetSystemCategoryTypeName(int categoryId)
        {
            return categoryId switch
            {
                INITIAL_BALANCE_INCOME_ID => SystemCategoryNames.INITIAL_BALANCE_NAME + "- Income",
                INITIAL_BALANCE_EXPENSE_ID => SystemCategoryNames.INITIAL_BALANCE_NAME + "- Expense",
                BALANCE_ADJUSTMENT_INCOME_ID => $"{SystemCategoryNames.BALANCE_ADJUSTMENT_NAME} - Income",
                BALANCE_ADJUSTMENT_EXPENSE_ID => $"{SystemCategoryNames.BALANCE_ADJUSTMENT_NAME} - Expense",
                TRANSFER_OUT_ID => $"{SystemCategoryNames.TRANSFER_OUT_NAME}",
                TRANSFER_IN_ID => $"{SystemCategoryNames.TRANSFER_IN_NAME}",
                CREDIT_PAYMENT_ID => $"{SystemCategoryNames.CREDIT_PAYMENT_NAME}",
                PAYMENT_RECEIVED_ID => $"{SystemCategoryNames.PAYMENT_RECEIVED_NAME}",
                CREDIT_ADVANCE_ID => $"{SystemCategoryNames.CREDIT_ADVANCE_NAME}",
                ADVANCE_RECEIVED_ID => $"{SystemCategoryNames.ADVANCE_RECEIVED_NAME}",
                _ => ""
            };
        }


        // ✅ NEW: Check if category pair is valid for transfers
        public static bool IsValidTransferPair(int fromCategoryId, int toCategoryId)
        {
            return (fromCategoryId, toCategoryId) switch
            {
                (TRANSFER_OUT_ID, TRANSFER_IN_ID) => true,
                (CREDIT_PAYMENT_ID, PAYMENT_RECEIVED_ID) => true,
                (CREDIT_ADVANCE_ID, ADVANCE_RECEIVED_ID) => true,
                _ => false
            };
        }

        // Existing helper methods
        public static int GetInitialBalanceCategoryId(bool isIncome)
            => isIncome ? INITIAL_BALANCE_INCOME_ID : INITIAL_BALANCE_EXPENSE_ID;

        public static int GetBalanceAdjustmentCategoryId(bool isIncome)
            => isIncome ? BALANCE_ADJUSTMENT_INCOME_ID : BALANCE_ADJUSTMENT_EXPENSE_ID;

        public static bool IsSystemCategory(int categoryId)
            => categoryId >= 1 && categoryId <= 10;

        public static bool IsTransferCategory(int categoryId)
            => categoryId >= 5 && categoryId <= 10;

        public static bool IsCreditRelatedCategory(int categoryId)
            => categoryId >= 7 && categoryId <= 10;

        // ✅ NEW: More specific transfer category checks
        public static bool IsTransferOutCategory(int categoryId)
            => categoryId == TRANSFER_OUT_ID ||
               categoryId == CREDIT_PAYMENT_ID ||
               categoryId == CREDIT_ADVANCE_ID;

        public static bool IsTransferInCategory(int categoryId)
            => categoryId == TRANSFER_IN_ID ||
               categoryId == PAYMENT_RECEIVED_ID ||
               categoryId == ADVANCE_RECEIVED_ID;

        // ✅ NEW: Get paired category for transfers
        public static int? GetPairedTransferCategory(int categoryId)
        {
            return categoryId switch
            {
                TRANSFER_OUT_ID => TRANSFER_IN_ID,
                TRANSFER_IN_ID => TRANSFER_OUT_ID,
                CREDIT_PAYMENT_ID => PAYMENT_RECEIVED_ID,
                PAYMENT_RECEIVED_ID => CREDIT_PAYMENT_ID,
                CREDIT_ADVANCE_ID => ADVANCE_RECEIVED_ID,
                ADVANCE_RECEIVED_ID => CREDIT_ADVANCE_ID,
                _ => null
            };
        }
    }
}