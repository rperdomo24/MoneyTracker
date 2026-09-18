using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Rules;

namespace MoneyTracker.Application.Interfaces
{
    public interface ITransactionRuleService
    {
        Task<OperationResult<List<TransactionRuleDto>>> GetAllAsync();
        Task<OperationResult<TransactionRuleDto>> GetByIdAsync(int id);
        Task<OperationResult<bool>> CreateAsync(TransactionRuleDto dto);
        Task<OperationResult<bool>> UpdateAsync(TransactionRuleDto dto);
        Task<OperationResult<bool>> DeleteAsync(int id);
        Task<OperationResult<bool>> ToggleEnabledAsync(int id, bool enabled);
        Task<OperationResult<int>> ApplyRulesAsync();
        Task<OperationResult<int>> ApplyRuleAsync(int ruleId);
        Task ApplyRulesToNewTransactionAsync(int transactionId);
        Task<OperationResult> ReorderAsync(List<int> orderedIds);
    }
}
