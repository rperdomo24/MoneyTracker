namespace MoneyTracker.UI.Services.Branding;

public interface IBrandingService
{
    string GetAssetPath(BrandingAsset asset, bool isDarkMode = false);
}
