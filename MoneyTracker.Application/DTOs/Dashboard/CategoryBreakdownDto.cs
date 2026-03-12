namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class CategoryBreakdownDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransaction { get; set; }
        public decimal PreviousMonthAmount { get; set; }
        public decimal Change { get; set; }
        public bool IsIncrease { get; set; }
    }
}
