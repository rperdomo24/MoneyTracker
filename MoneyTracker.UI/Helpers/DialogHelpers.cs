using MoneyTracker.UI.Components.Shared;
using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class DialogServiceExtensions
    {
        public static async Task<bool> ShowConfirmDialogAsync(
            this IDialogService dialogService,
            string title,
            string message,
            string confirmText = "Confirm",
            Color color = Color.Primary,
            string cancelText = "Cancel")
        {
            var parameters = new DialogParameters
            {
                ["Title"] = title,
                ["Message"] = message,
                ["ConfirmText"] = confirmText,
                ["CancelText"] = cancelText,
                ["Color"] = color
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await dialogService.ShowAsync<ConfirmDialog>(title, parameters, options);
            var result = await dialog.Result;

            if (result is null || result.Canceled)
            {
                return false;
            }

            return result.Data is bool confirmed && confirmed;
        }

        public static async Task<bool> ShowDeleteConfirmDialogAsync(
            this IDialogService dialogService,
            string itemName,
            string itemType = "item")
        {
            return await ShowConfirmDialogAsync(
                dialogService,
                $"Delete {itemType}",
                $"Are you sure you want to delete '{itemName}'? This action cannot be undone.",
                "Delete",
                Color.Error,
                "Cancel"
            );
        }

        public static async Task<string?> ShowInputDialogAsync(
            this IDialogService dialogService,
            string title,
            string message,
            string placeholder = "",
            string initialValue = "")
        {
            var parameters = new DialogParameters
            {
                ["Title"] = title,
                ["Message"] = message,
                ["Placeholder"] = placeholder,
                ["InitialValue"] = initialValue
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await dialogService.ShowAsync<InputDialog>(title, parameters, options);
            var result = await dialog.Result;

            if (result is null || result.Canceled)
            {
                return null;
            }

            return result.Data?.ToString();
        }
    }
}
