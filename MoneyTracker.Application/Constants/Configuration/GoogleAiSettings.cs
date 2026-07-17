namespace MoneyTracker.Application.Constants.Configuration
{
    public class GoogleAiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.0-flash";
        public int MaxTokens { get; set; } = 1024;
        public string ProjectId { get; set; } = string.Empty;
        public string Location { get; set; } = "us-central1";
        // Path to service account JSON file (or set GOOGLE_APPLICATION_CREDENTIALS env var)
        public string ServiceAccountJsonPath { get; set; } = string.Empty;
    }
}
