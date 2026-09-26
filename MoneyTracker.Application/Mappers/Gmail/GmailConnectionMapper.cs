using MoneyTracker.Application.DTOs.Gmail;
using MoneyTracker.Domain.Entities;

namespace MoneyTracker.Application.Mappers.Gmail
{
    public static class GmailConnectionMapper
    {
        public static GmailConnectionDto MapToDto(this GmailConnection entity) =>
            new()
            {
                Id = entity.Id,
                Email = entity.Email,
                LastSyncAtUtc = entity.LastSyncAtUtc,
                AutoSyncEnabled = entity.AutoSyncEnabled,
                SyncIntervalMinutes = entity.SyncIntervalMinutes
            };
    }
}
