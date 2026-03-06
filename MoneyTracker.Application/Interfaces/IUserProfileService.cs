using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.Application.Interfaces
{
    public interface IUserProfileService
    {
        Task<OperationResult<UserProfileSettingsDto>> GetCurrentProfileAsync();
        Task<OperationResult> UpdateDisplayNameAsync(string? displayName);
        Task<OperationResult> ChangePasswordAsync(string currentPassword, string newPassword);
        Task<OperationResult<string?>> UpdateAvatarAsync(string contentType, byte[] content);
        Task<OperationResult> RemoveAvatarAsync();
    }
}
