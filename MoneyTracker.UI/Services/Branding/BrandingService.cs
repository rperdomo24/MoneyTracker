namespace MoneyTracker.UI.Services.Branding;

public sealed class BrandingService : IBrandingService
{
    private const string BrandingRoot = "/images/branding";

    public string GetAssetPath(BrandingAsset asset, bool isDarkMode = false)
    {
        return asset switch
        {
            BrandingAsset.HorizontalLogo => isDarkMode
                ? $"{BrandingRoot}/logo-dark.png"
                : $"{BrandingRoot}/logo-light.png",
            BrandingAsset.IconLogo => $"{BrandingRoot}/logo-icon.png",
            BrandingAsset.MonoLogo => $"{BrandingRoot}/logo-mono.png",
            BrandingAsset.Favicon32 => $"{BrandingRoot}/favicon-32x32.png",
            BrandingAsset.AppleTouch180 => $"{BrandingRoot}/apple-touch-icon-180x180.png",
            BrandingAsset.AppIcon192 => $"{BrandingRoot}/icon-192x192.png",
            BrandingAsset.AppIcon512 => $"{BrandingRoot}/icon-512x512.png",
            _ => $"{BrandingRoot}/logo-light.png"
        };
    }
}
