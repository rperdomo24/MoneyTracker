using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Budgets;

namespace MoneyTracker.Application.Interfaces
{
    public interface IBudgetService
    {
        //Task<OperationResult<BudgetMonthlySummaryDto>> GetMonthlyAsync(int year, int month, int? categoryType = null);
        Task<OperationResult<BudgetDto>> GetByIdAsync(int id);
        Task<OperationResult<bool>> CreateOrUpdateAsync(BudgetDto dto);
        Task<OperationResult<bool>> DeleteAsync(int id);
        Task<OperationResult<List<BudgetWithUsageDto>>> GetMonthlyWithUsageAsync(int year, int month);
    }
}
