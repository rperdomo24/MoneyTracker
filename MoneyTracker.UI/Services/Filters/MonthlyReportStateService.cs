using MoneyTracker.Application.DTOs.Reports;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.UI.Services.Filters.Interface;
using System.Text.Json;

namespace MoneyTracker.UI.Services.Filters
{
    public class MonthlyReportStateService : IMonthlyReportStateService
    {
        private readonly IUserReportPreferenceRepository _repo;

        public MonthlyReportStateService(IUserReportPreferenceRepository repo)
        {
            _repo = repo;
        }

        public async Task<MonthlyReportStateDto?> GetAsync()
        {
            var pref = await _repo.GetAsync();
            if (pref is null) return null;

            return new MonthlyReportStateDto
            {
                Year = pref.Year,
                Month = pref.Month,
                SelectedCategories = JsonSerializer.Deserialize<List<string>>(pref.CategoriesJson) ?? new(),
                ActiveCardFilter = pref.ActiveCardFilter
            };
        }

        public async Task SaveAsync(MonthlyReportStateDto state)
        {
            await _repo.SaveAsync(new UserReportPreference
            {
                Year = state.Year,
                Month = state.Month,
                CategoriesJson = JsonSerializer.Serialize(state.SelectedCategories),
                ActiveCardFilter = state.ActiveCardFilter
            });
        }

        public Task ClearAsync() => Task.CompletedTask;
    }
}
