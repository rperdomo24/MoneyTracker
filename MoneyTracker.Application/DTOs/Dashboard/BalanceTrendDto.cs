namespace MoneyTracker.Application.DTOs.Dashboard
{
    public class BalanceTrendDto
    {
        public DateTime Date { get; set; }
        public decimal CheckingBalance { get; set; }
        public decimal SavingsBalance { get; set; }
        public decimal CreditBalance { get; set; }
        public decimal InvestmentBalance { get; set; }
        public decimal CashBalance { get; set; }
        public decimal TotalBalance { get; set; }
    }
}
