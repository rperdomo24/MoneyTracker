using MudBlazor;

namespace MoneyTracker.UI.Helpers
{
    public static class DialogHelper
    {
        public static DialogOptions DefaultOptions => new()
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large
        };

        public static DialogOptions MediumOptions => new()
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        public static DialogOptions SmallOptions => new()
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

        public static DialogOptions FullScreenOptions => new()
        {
            CloseButton = true,
            FullScreen = true
        };
    }
}