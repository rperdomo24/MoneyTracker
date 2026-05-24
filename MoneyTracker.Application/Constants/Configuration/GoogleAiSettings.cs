namespace MoneyTracker.Application.Constants.Configuration
{
    public class GoogleAiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-1.5-flash";
        public int MaxTokens { get; set; } = 1024;
    }
}
