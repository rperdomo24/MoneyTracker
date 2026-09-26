using MoneyTracker.Application.DTOs.Gmail;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers.Gmail
{
    public static class EmailImportRuleMapper
    {
        public static EmailImportRuleDto MapToDto(this EmailImportRule entity) =>
            new()
            {
                Id = entity.Id,
                SenderPattern = entity.SenderPattern,
                BankLabel = entity.BankLabel,
                SubjectIncludeKeywords = entity.SubjectIncludeKeywords,
                SubjectExcludeKeywords = entity.SubjectExcludeKeywords,
                DefaultAccountId = entity.DefaultAccountId,
                IsActive = entity.IsActive
            };

        public static EmailImportRule MapToEntity(this EmailImportRuleDto dto) =>
            new()
            {
                Id = dto.Id,
                SenderPattern = dto.SenderPattern,
                BankLabel = dto.BankLabel,
                SubjectIncludeKeywords = dto.SubjectIncludeKeywords,
                SubjectExcludeKeywords = dto.SubjectExcludeKeywords,
                DefaultAccountId = dto.DefaultAccountId,
                IsActive = dto.IsActive
            };

        public static void UpdateEntity(this EmailImportRule entity, EmailImportRuleDto dto)
        {
            entity.SenderPattern = dto.SenderPattern;
            entity.BankLabel = dto.BankLabel;
            entity.SubjectIncludeKeywords = dto.SubjectIncludeKeywords;
            entity.SubjectExcludeKeywords = dto.SubjectExcludeKeywords;
            entity.DefaultAccountId = dto.DefaultAccountId;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
