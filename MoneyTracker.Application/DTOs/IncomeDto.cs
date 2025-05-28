namespace MoneyTracker.Application.DTOs
{
    public class IncomeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public int? AccountId { get; set; }
        public string? PaymentMethod { get; set; }
    }
}
