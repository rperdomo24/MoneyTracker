using MoneyTracker.Application.DTOs.Rules;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers
{
    public static class TransactionRuleMapper
    {
        public static TransactionRuleDto MapToDto(this TransactionRule entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            IsEnabled = entity.IsEnabled,
            ApplyToHistorical = entity.ApplyToHistorical,
            ApplyFromDate = entity.ApplyFromDate,
            Order = entity.Order,
            Conditions = entity.Conditions.Select(c => c.MapToDto()).ToList(),
            Actions = entity.Actions.Select(a => a.MapToDto()).ToList()
        };

        public static TransactionRuleConditionDto MapToDto(this TransactionRuleCondition c) => new()
        {
            Id = c.Id,
            RuleId = c.RuleId,
            Field = c.Field,
            Operator = c.Operator,
            Value = c.Value
        };

        public static TransactionRuleActionDto MapToDto(this TransactionRuleAction a) => new()
        {
            Id = a.Id,
            RuleId = a.RuleId,
            ActionType = a.ActionType,
            MerchantId = a.MerchantId,
            CategoryId = a.CategoryId,
            StringValue = a.StringValue,
            MerchantName = a.Merchant?.Name,
            CategoryName = a.Category?.Name
        };

        public static TransactionRule MapToEntity(this TransactionRuleDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            IsEnabled = dto.IsEnabled,
            ApplyToHistorical = dto.ApplyToHistorical,
            ApplyFromDate = dto.ApplyFromDate,
            Order = dto.Order,
            Conditions = dto.Conditions.Select(c => new TransactionRuleCondition
            {
                Field = c.Field,
                Operator = c.Operator,
                Value = c.Value
            }).ToList(),
            Actions = dto.Actions.Select(a => new TransactionRuleAction
            {
                ActionType = a.ActionType,
                MerchantId = a.MerchantId,
                CategoryId = a.CategoryId,
                StringValue = a.StringValue
            }).ToList()
        };
    }
}
