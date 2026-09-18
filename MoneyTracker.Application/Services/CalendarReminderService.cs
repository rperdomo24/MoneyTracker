using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Calendar;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Mappers.Calendar;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class CalendarReminderService : ICalendarReminderService
    {
        private readonly ICalendarReminderRepository _repository;
        private readonly ILogger<CalendarReminderService> _logger;

        public CalendarReminderService(ICalendarReminderRepository repository, ILogger<CalendarReminderService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OperationResult<List<CalendarReminderDto>>> GetByMonthAsync(int year, int month)
        {
            try
            {
                var entities = await _repository.GetByMonthAsync(year, month);
                return OperationResult<List<CalendarReminderDto>>.Ok(entities.Select(e => e.MapToDto()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reminders for {Year}/{Month}", year, month);
                return OperationResult<List<CalendarReminderDto>>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> CreateAsync(CalendarReminderDto dto)
        {
            try
            {
                await _repository.AddAsync(dto.MapToEntity());
                return OperationResult.Ok(OperationMessages.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar reminder");
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> UpdateAsync(CalendarReminderDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(dto.Id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                entity.UpdateEntity(dto);
                await _repository.UpdateAsync(entity);
                return OperationResult.Ok(OperationMessages.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating calendar reminder {Id}", dto.Id);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity is null)
                    return OperationResult.Fail(OperationMessages.NotFound);

                await _repository.DeleteAsync(id);
                return OperationResult.Ok(OperationMessages.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calendar reminder {Id}", id);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }
    }
}
