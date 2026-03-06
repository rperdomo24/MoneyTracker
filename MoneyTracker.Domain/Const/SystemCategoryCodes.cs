using MoneyTracker.Domain.Enums.Account;

namespace MoneyTracker.Domain.Const
{
    public static class SystemCategoryCodes
    {
        public const string InitialBalanceIncome = "INITIAL_BALANCE_INCOME";
        public const string InitialBalanceExpense = "INITIAL_BALANCE_EXPENSE";
        public const string BalanceAdjustmentIncome = "BALANCE_ADJUSTMENT_INCOME";
        public const string BalanceAdjustmentExpense = "BALANCE_ADJUSTMENT_EXPENSE";
        public const string TransferOut = "TRANSFER_OUT";
        public const string TransferIn = "TRANSFER_IN";
        public const string CreditPayment = "CREDIT_PAYMENT";
        public const string PaymentReceived = "PAYMENT_RECEIVED";
        public const string CreditAdvance = "CREDIT_ADVANCE";
        public const string AdvanceReceived = "ADVANCE_RECEIVED";

        public static string GetInitialBalanceCode(bool isIncome)
            => isIncome ? InitialBalanceIncome : InitialBalanceExpense;

        public static string GetBalanceAdjustmentCode(bool isIncome)
            => isIncome ? BalanceAdjustmentIncome : BalanceAdjustmentExpense;

        public static (string fromCode, string toCode) GetTransferCodesByAccountType(AccountType fromType, AccountType toType)
        {
            return (fromType, toType) switch
            {
                (_, AccountType.Credit) => (CreditPayment, PaymentReceived),
                (AccountType.Credit, _) => (CreditAdvance, AdvanceReceived),
                _ => (TransferOut, TransferIn)
            };
        }

        public static string GetTransferTypeName(string fromCode, string toCode)
        {
            return (fromCode, toCode) switch
            {
                (TransferOut, TransferIn) => "Account Transfer",
                (CreditPayment, PaymentReceived) => "Credit Payment",
                (CreditAdvance, AdvanceReceived) => "Credit Advance",
                _ => "Transfer"
            };
        }

        public static bool IsTransfer(string? code)
            => code is TransferOut or TransferIn or CreditPayment or PaymentReceived or CreditAdvance or AdvanceReceived;

        public static bool IsTransferOut(string? code)
            => code is TransferOut or CreditPayment or CreditAdvance;

        public static bool IsTransferIn(string? code)
            => code is TransferIn or PaymentReceived or AdvanceReceived;

        public static bool IsCreditRelated(string? code)
            => code is CreditPayment or PaymentReceived or CreditAdvance or AdvanceReceived;

        public static string GetDisplayName(string code)
        {
            return code switch
            {
                InitialBalanceIncome => $"{SystemCategoryNames.INITIAL_BALANCE_NAME} - Income",
                InitialBalanceExpense => $"{SystemCategoryNames.INITIAL_BALANCE_NAME} - Expense",
                BalanceAdjustmentIncome => $"{SystemCategoryNames.BALANCE_ADJUSTMENT_NAME} - Income",
                BalanceAdjustmentExpense => $"{SystemCategoryNames.BALANCE_ADJUSTMENT_NAME} - Expense",
                TransferOut => SystemCategoryNames.TRANSFER_OUT_NAME,
                TransferIn => SystemCategoryNames.TRANSFER_IN_NAME,
                CreditPayment => SystemCategoryNames.CREDIT_PAYMENT_NAME,
                PaymentReceived => SystemCategoryNames.PAYMENT_RECEIVED_NAME,
                CreditAdvance => SystemCategoryNames.CREDIT_ADVANCE_NAME,
                AdvanceReceived => SystemCategoryNames.ADVANCE_RECEIVED_NAME,
                _ => string.Empty
            };
        }
    }
}
