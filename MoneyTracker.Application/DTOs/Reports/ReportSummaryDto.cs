namespace MoneyTracker.Application.DTOs.Reports
{
    public class ReportSummaryDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal FinalBalance => TotalIncome - TotalExpenses;
        public decimal TotalCard { get; set; }
        public decimal TotalCash { get; set; }
        public int TransactionCount { get; set; }
    }
}
