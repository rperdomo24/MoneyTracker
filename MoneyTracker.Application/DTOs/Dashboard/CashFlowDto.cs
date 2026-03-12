namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class CashFlowDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetCashFlow { get; set; }
        public decimal DailyBurnRate { get; set; }
        public int DaysLeftInMonth { get; set; }
        public decimal ProjectedMonthEnd { get; set; }
        public int DaysInMonth { get; set; }
        public int DaysElapsed { get; set; }
        public List<CashFlowCategoryDto> IncomeByCategory { get; set; } = new();
        public List<CashFlowCategoryDto> ExpenseByCategory { get; set; } = new();
    }

    public class CashFlowCategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
