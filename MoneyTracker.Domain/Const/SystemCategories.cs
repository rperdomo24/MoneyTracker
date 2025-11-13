using MoneyTracker.Domain.Enums.Account;
using MoneyTracker.Domain.Enums.Transaction;

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

        // User Income Categories (11-13)
        public const int SALARY_ID = 11;
        public const int FREELANCE_ID = 12;
        public const int INVESTMENTS_ID = 13;

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

        public static TransactionTypeEnum GetInitialBalanceTransactionType(bool isIncome)
            => isIncome ? TransactionTypeEnum.Income : TransactionTypeEnum.Expense;

        public static TransactionTypeEnum GetBalanceAdjustmentTransactionType(bool isIncome)
            => isIncome ? TransactionTypeEnum.Income : TransactionTypeEnum.Expense;

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

        public static bool IsTransferOutCategory(int categoryId)
            => categoryId == TRANSFER_OUT_ID;

        public static bool IsTransferInCategory(int categoryId)
            => categoryId == TRANSFER_IN_ID;
    }
}