using Microsoft.AspNetCore.Components;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.UI.Helpers;

namespace MoneyTracker.UI.Components.Pages.Transactions
{
    public partial class TransactionListBase
    {
        [Parameter] public List<TransactionGroupDto> GroupedTransactions { get; set; } = new();
        [Parameter] public bool Loading { get; set; }
        [Parameter] public bool ShowAccounts { get; set; } = true;
        [Parameter] public bool HasActiveFilters { get; set; }
        [Parameter] public string EmptyStateMessage { get; set; } = "Start by adding your first transaction";
        [Parameter] public int TotalTransactions { get; set; }
        [Parameter] public int CurrentPage { get; set; } = 1;
        [Parameter] public int TotalPages { get; set; } = 1;
        [Parameter] public int RowsPerPage { get; set; } = 10;
        [Parameter] public List<int> PageSizeOptions { get; set; } = new() { 5, 10, 25, 50, 100 };

        // Selection
        [Parameter] public HashSet<int> SelectedTransactionIds { get; set; } = new();
        [Parameter] public EventCallback<HashSet<int>> SelectedTransactionIdsChanged { get; set; }

        // Actions
        [Parameter] public EventCallback<TransactionDto> OnEdit { get; set; }
        [Parameter] public EventCallback<TransactionDto> OnDuplicate { get; set; }
        [Parameter] public EventCallback<TransactionDto> OnDelete { get; set; }
        [Parameter] public EventCallback OnClearFilters { get; set; }
        [Parameter] public EventCallback OnDeleteSelected { get; set; }
        [Parameter] public EventCallback OnPrintSelected { get; set; }
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }
        [Parameter] public EventCallback<int> OnRowsPerPageChanged { get; set; }

        // Data for pairing transactions
        [Parameter] public List<TransactionDto>? AllTransactions { get; set; }

        [Parameter] public decimal SelectedSum { get; set; }

        // ========== COMPUTED PROPERTIES ==========
        public int SelectedCount => SelectedTransactionIds?.Count ?? 0;

        // ========== METHODS ==========
        private TransactionDto? GetPairedTransaction(TransactionDto transaction)
        {
            return TransactionGroupingHelper.GetPairedTransaction(transaction, AllTransactions ?? new());
        }

        private bool IsTransactionSelected(int transactionId)
        {
            return SelectedTransactionIds?.Contains(transactionId) == true;
        }

        private bool IsDuplicating(int transactionId) => false;

        private bool IsGroupSelected(TransactionGroupDto group)
        {
            return group.Transactions.All(t => SelectedTransactionIds?.Contains(t.Id) == true);
        }

        private async Task ToggleGroupSelection(TransactionGroupDto group, bool selected)
        {
            if (SelectedTransactionIds == null) return;

            if (selected)
            {
                foreach (var transaction in group.Transactions)
                {
                    SelectedTransactionIds.Add(transaction.Id);
                }
            }
            else
            {
                foreach (var transaction in group.Transactions)
                {
                    SelectedTransactionIds.Remove(transaction.Id);
                }
            }
            await SelectedTransactionIdsChanged.InvokeAsync(SelectedTransactionIds);
        }

        private async Task ToggleTransactionSelection(int transactionId, bool selected)
        {
            if (SelectedTransactionIds == null) return;

            if (selected)
            {
                SelectedTransactionIds.Add(transactionId);
            }
            else
            {
                SelectedTransactionIds.Remove(transactionId);
            }
            await SelectedTransactionIdsChanged.InvokeAsync(SelectedTransactionIds);
        }

        private void ClearSelection()
        {
            SelectedTransactionIds?.Clear();
            SelectedTransactionIdsChanged.InvokeAsync(SelectedTransactionIds);
        }

        private async Task HandlePageChanged(int page)
        {
            await OnPageChanged.InvokeAsync(page);
        }

        private async Task HandlePageSizeChanged(int newSize)
        {
            RowsPerPage = newSize;
            await OnRowsPerPageChanged.InvokeAsync(newSize);
        }
    }
}

