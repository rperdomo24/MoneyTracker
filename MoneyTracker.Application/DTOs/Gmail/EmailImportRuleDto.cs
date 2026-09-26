namespace MoneyTracker.Application.DTOs.Gmail
{
    public class EmailImportRuleDto
    {
        public int Id { get; set; }
        public string SenderPattern { get; set; } = string.Empty;
        public string BankLabel { get; set; } = string.Empty;
        public string? SubjectIncludeKeywords { get; set; }
        public string? SubjectExcludeKeywords { get; set; }
        public int? DefaultAccountId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
