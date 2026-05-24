using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Rules;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Enums.Rules;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class TransactionRuleService : ITransactionRuleService
    {
        private readonly ITransactionRuleRepository _ruleRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ITimeZoneService _timeZoneService;
        private readonly ILogger<TransactionRuleService> _logger;

        public TransactionRuleService(
            ITransactionRuleRepository ruleRepository,
            ITransactionRepository transactionRepository,
            ITimeZoneService timeZoneService,
            ILogger<TransactionRuleService> logger)
        {
            _ruleRepository = ruleRepository;
            _transactionRepository = transactionRepository;
            _timeZoneService = timeZoneService;
            _logger = logger;
        }

        public async Task<OperationResult<List<TransactionRuleDto>>> GetAllAsync()
        {
            try
            {
                var rules = await _ruleRepository.GetAllWithDetailsAsync();
                return OperationResult<List<TransactionRuleDto>>.Ok(rules.Select(r => r.MapToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<List<TransactionRuleDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<TransactionRuleDto>> GetByIdAsync(int id)
        {
            try
            {
                var rule = await _ruleRepository.GetByIdWithDetailsAsync(id);
                if (rule is null)
                    return OperationResult<TransactionRuleDto>.Fail(OperationMessages.NotFound);

                return OperationResult<TransactionRuleDto>.Ok(rule.MapToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<TransactionRuleDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> CreateAsync(TransactionRuleDto dto)
        {
            try
            {
                if (!dto.Conditions.Any())
                    return OperationResult<bool>.Fail("At least one condition is required.");

                if (!dto.Actions.Any())
                    return OperationResult<bool>.Fail("At least one action is required.");

                var entity = dto.MapToEntity();
                entity.CreatedAt = _timeZoneService.GetNowInUtc();
                entity.UpdatedAt = _timeZoneService.GetNowInUtc();

                await _ruleRepository.AddAsync(entity);
                return OperationResult<bool>.Ok(true, OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> UpdateAsync(TransactionRuleDto dto)
        {
            try
            {
                if (!dto.Conditions.Any())
                    return OperationResult<bool>.Fail("At least one condition is required.");

                if (!dto.Actions.Any())
                    return OperationResult<bool>.Fail("At least one action is required.");

                var entity = dto.MapToEntity();
                entity.UpdatedAt = _timeZoneService.GetNowInUtc();

                await _ruleRepository.UpdateAsync(entity);
                return OperationResult<bool>.Ok(true, OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> DeleteAsync(int id)
        {
            try
            {
                await _ruleRepository.SoftDeleteAsync(id);
                return OperationResult<bool>.Ok(true, OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<bool>> ToggleEnabledAsync(int id, bool enabled)
        {
            try
            {
                await _ruleRepository.ToggleEnabledAsync(id, enabled);
                return OperationResult<bool>.Ok(true, enabled ? "Rule enabled." : "Rule disabled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<bool>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<int>> ApplyRulesAsync()
        {
            try
            {
                var rules = await _ruleRepository.GetEnabledRulesAsync();
                if (!rules.Any())
                    return OperationResult<int>.Ok(0, "No enabled rules found.");

                var transactions = await _transactionRepository.GetAllAsync();
                return await ApplyRulesToTransactions(rules, transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<int>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<int>> ApplyRuleAsync(int ruleId)
        {
            try
            {
                var rule = await _ruleRepository.GetByIdWithDetailsAsync(ruleId);
                if (rule is null)
                    return OperationResult<int>.Fail(OperationMessages.NotFound);

                var transactions = await _transactionRepository.GetAllAsync();

                if (!rule.ApplyToHistorical && rule.ApplyFromDate.HasValue)
                {
                    var fromUtc = _timeZoneService.ConvertToUtc(rule.ApplyFromDate.Value);
                    transactions = transactions.Where(t => t.Date >= fromUtc).ToList();
                }

                return await ApplyRulesToTransactions(new List<TransactionRule> { rule }, transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationMessages.UnexpectedError);
                return OperationResult<int>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task ApplyRulesToNewTransactionAsync(int transactionId)
        {
            try
            {
                var rules = await _ruleRepository.GetEnabledRulesAsync();
                if (!rules.Any()) return;

                var transaction = await _transactionRepository.GetByIdAsync(transactionId);
                if (transaction is null || transaction.IsDeleted) return;

                bool modified = false;
                foreach (var rule in rules)
                {
                    if (!MatchesAllConditions(transaction, rule.Conditions)) continue;

                    ApplyActions(transaction, rule.Actions);
                    modified = true;
                    break;
                }

                if (modified)
                {
                    transaction.UpdatedAt = _timeZoneService.GetNowInUtc();
                    await _transactionRepository.UpdateAsync(transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying rules to new transaction {TransactionId}", transactionId);
            }
        }

        private async Task<OperationResult<int>> ApplyRulesToTransactions(
            List<TransactionRule> rules,
            List<Transaction> transactions)
        {
            var modified = new List<Transaction>();

            foreach (var transaction in transactions)
            {
                if (transaction.IsDeleted) continue;

                foreach (var rule in rules)
                {
                    if (!MatchesAllConditions(transaction, rule.Conditions)) continue;

                    ApplyActions(transaction, rule.Actions);
                    modified.Add(transaction);
                    break; // first matching rule wins
                }
            }

            foreach (var transaction in modified)
            {
                transaction.UpdatedAt = _timeZoneService.GetNowInUtc();
                await _transactionRepository.UpdateAsync(transaction);
            }

            return OperationResult<int>.Ok(modified.Count, $"{modified.Count} transaction(s) updated.");
        }

        private static bool MatchesAllConditions(Transaction t, IEnumerable<TransactionRuleCondition> conditions)
        {
            foreach (var cond in conditions)
            {
                if (!MatchesCondition(t, cond)) return false;
            }
            return true;
        }

        private static bool MatchesCondition(Transaction t, TransactionRuleCondition cond)
        {
            return cond.Field switch
            {
                RuleConditionField.Name => MatchesStringCondition(t.Name, cond.Operator, cond.Value),
                RuleConditionField.Amount => MatchesAmountCondition(t.Amount, cond.Operator, cond.Value),
                RuleConditionField.Category => cond.Operator == RuleConditionOperator.Equals
                    && int.TryParse(cond.Value, out var catId)
                    && t.CategoryId == catId,
                RuleConditionField.Merchant => cond.Operator == RuleConditionOperator.Equals
                    && int.TryParse(cond.Value, out var mId)
                    && t.MerchantId.HasValue
                    && t.MerchantId.Value == mId,
                _ => false
            };
        }

        private static bool MatchesStringCondition(string value, RuleConditionOperator op, string pattern)
        {
            return op switch
            {
                RuleConditionOperator.Contains => value.Contains(pattern, StringComparison.OrdinalIgnoreCase),
                RuleConditionOperator.Equals => value.Equals(pattern, StringComparison.OrdinalIgnoreCase),
                RuleConditionOperator.StartsWith => value.StartsWith(pattern, StringComparison.OrdinalIgnoreCase),
                RuleConditionOperator.EndsWith => value.EndsWith(pattern, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private static bool MatchesAmountCondition(decimal amount, RuleConditionOperator op, string rawValue)
        {
            if (!decimal.TryParse(rawValue, out var target)) return false;

            return op switch
            {
                RuleConditionOperator.Equals => amount == target,
                RuleConditionOperator.GreaterThan => amount > target,
                RuleConditionOperator.LessThan => amount < target,
                RuleConditionOperator.GreaterThanOrEqual => amount >= target,
                RuleConditionOperator.LessThanOrEqual => amount <= target,
                _ => false
            };
        }

        private static void ApplyActions(Transaction t, IEnumerable<TransactionRuleAction> actions)
        {
            foreach (var action in actions)
            {
                switch (action.ActionType)
                {
                    case RuleActionType.SetMerchant when action.MerchantId.HasValue:
                        t.MerchantId = action.MerchantId;
                        break;
                    case RuleActionType.SetCategory when action.CategoryId.HasValue:
                        t.CategoryId = action.CategoryId.Value;
                        break;
                    case RuleActionType.SetName when !string.IsNullOrWhiteSpace(action.StringValue):
                        t.Name = action.StringValue;
                        break;
                }
            }
        }
    }
}
