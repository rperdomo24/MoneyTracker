using MudBlazor;

public static class DialogHelpers
{
    public static async Task<bool> ShowConfirmDialogAsync(
        this IDialogService dialogService,
        string title,
        string message,
        string buttonText = "Confirm",
        Color color = Color.Primary)
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = message,
            ["ButtonText"] = buttonText,
            ["Color"] = color
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await dialogService.ShowAsync<MudMessageBox>(title, parameters, options);
        var result = await dialog.Result;
        return !result.Canceled;
    }
}