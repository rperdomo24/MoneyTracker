namespace MoneyTracker.Domain.Entities
{
    public class Account
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal InitialBalance { get; set; }
        public string Type { get; set; } = default!; // Ej: "Bank", "Cash", "Credit Card"

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
