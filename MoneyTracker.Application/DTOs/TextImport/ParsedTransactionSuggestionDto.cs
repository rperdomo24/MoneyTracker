using MoneyTracker.Domain.Enums.Transaction;

namespace MoneyTracker.Application.DTOs.TextImport
{
    public sealed class ParsedTransactionSuggestionDto
    {
        public Guid TempId { get; set; } = Guid.NewGuid();

        public TransactionTypeEnum Type { get; set; } // Expense / Income / TransferCandidate / Unknown
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        public DateTime DateLocal { get; set; } // Local time (your UI time zone)
        public string Merchant { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty; // "CUSCATLAN" | "AMEX"
        public string AccountHint { get; set; } = string.Empty; // last4 / "0175" / "0731" etc.

        public int? SuggestedAccountId { get; set; }
        public int? SuggestedCategoryId { get; set; }

        public decimal Confidence { get; set; } // 0..1
        public List<string> Warnings { get; set; } = new();

        public string RawText { get; set; } = string.Empty;
    }
}
