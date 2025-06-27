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
    }
}
