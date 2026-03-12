using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.UI.Helpers
{
    public static class TransactionGroupingHelper
    {
        /// <summary>
        /// Groups transactions by date and applies pagination
        /// </summary>
        public static (List<TransactionGroupDto> Groups, int TotalPages, int TotalCount)
            GroupAndPaginateTransactions(
                List<TransactionDto> transactions,
                int currentPage,
                int rowsPerPage)
        {
            if (transactions == null || !transactions.Any())
            {
                return (new List<TransactionGroupDto>(), 1, 0);
            }

            var totalCount = transactions.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)rowsPerPage);

            var pagedTransactions = transactions
                .OrderByDescending(t => t.Date)
                .Skip((currentPage - 1) * rowsPerPage)
                .Take(rowsPerPage)
                .ToList();

            var groups = pagedTransactions
                .GroupBy(t => t.Date.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new TransactionGroupDto
                {
                    Date = g.Key,
                    Transactions = g.OrderByDescending(t => t.Date).ToList()
                })
                .ToList();

            return (groups, totalPages, totalCount);
        }

        /// <summary>
        /// Finds paired transaction for a transfer
        /// </summary>
        public static TransactionDto? GetPairedTransaction(
            TransactionDto transaction,
            List<TransactionDto> allTransactions)
        {
            if (!transaction.TransferPairId.HasValue)
                return null;

            return allTransactions.FirstOrDefault(t => t.Id == transaction.TransferPairId.Value);
        }

        /// <summary>
        /// Filters transactions for display (handles transfer deduplication based on context)
        /// </summary>
        public static List<TransactionDto> FilterForDisplay(
            List<TransactionDto> transactions,
            TransactionFilterDto filter)
        {
            if (transactions == null || !transactions.Any())
                return new List<TransactionDto>();

            var result = new List<TransactionDto>();
            var processedTransferIds = new HashSet<int>();

            foreach (var transaction in transactions)
            {
                // Skip if already processed
                if (processedTransferIds.Contains(transaction.Id))
                    continue;

                // Handle transfers
                if (transaction.TransferPairId.HasValue)
                {
                    // Mark both as processed
                    processedTransferIds.Add(transaction.Id);
                    processedTransferIds.Add(transaction.TransferPairId.Value);

                    // If account filter exists, show only the transaction for filtered accounts
                    if (filter.AccountIds?.Any() == true)
                    {
                        if (filter.AccountIds.Contains(transaction.AccountId))
                        {
                            result.Add(transaction);
                        }
                    }
                    else
                    {
                        // No account filter: show the transaction (will render as pair)
                        result.Add(transaction);
                    }
                }
                else
                {
                    // Regular transaction: always add
                    result.Add(transaction);
                }
            }

            return result;
        }
    }
}
