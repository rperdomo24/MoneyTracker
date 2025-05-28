namespace MoneyTracker.Application.DTOs
{
    public class AccountDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public decimal Balance { get; set; }
        public decimal CreditLimit { get; set; }
        public string? Color { get; set; }
        public string? Notes { get; set; }
    }
}
