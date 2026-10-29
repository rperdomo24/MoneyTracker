namespace MoneyTracker.Application.DTOs.Merchants
{
    public class MerchantDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        public int TransactionCount { get; set; }
    }
}
