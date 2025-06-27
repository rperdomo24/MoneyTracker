namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class FinancialAlertDto
    {
        public string Type { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsUrgent { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Limit { get; set; }
    }
}
