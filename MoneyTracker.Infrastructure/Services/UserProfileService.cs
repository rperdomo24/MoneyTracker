using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICurrentUserService _currentUserService;
        private readonly ITenantContext _tenantContext;

        public UserProfileService(
            IServiceScopeFactory scopeFactory,
            ICurrentUserService currentUserService,
            ITenantContext tenantContext)
        {
            _scopeFactory = scopeFactory;
            _currentUserService = currentUserService;
            _tenantContext = tenantContext;
        }

        public async Task<OperationResult<UserProfileSettingsDto>> GetCurrentProfileAsync()
        {
            await _lock.WaitAsync();
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                var user = await GetCurrentUserAsync(dbContext, asNoTracking: true);
                if (user is null)
                {
                    return OperationResult<UserProfileSettingsDto>.Fail("User not found.");
                }

                var avatar = await dbContext.UserAvatars
                    .AsNoTracking()
                    .SingleOrDefaultAsync(a => a.UserId == user.Id);

                var dto = new UserProfileSettingsDto
                {
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    TenantId = user.TenantId.ToString(),
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    AvatarDataUrl = avatar is { Content.Length: > 0 }
                        ? $"data:{avatar.ContentType};base64,{Convert.ToBase64String(avatar.Content)}"
                        : null
                };

                return OperationResult<UserProfileSettingsDto>.Ok(dto, OperationMessages.DataRetrieved);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<OperationResult> UpdateDisplayNameAsync(string? displayName)
        {
            await _lock.WaitAsync();
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                var user = await GetCurrentUserAsync(dbContext, asNoTracking: false);
                if (user is null)
                {
                    return OperationResult.Fail("User not found.");
                }

                user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
                await dbContext.SaveChangesAsync();
                return OperationResult.Ok("Profile updated.");
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<OperationResult> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            await _lock.WaitAsync();
            try
            {
                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return OperationResult.Fail("User not found.");
                }

                await using var scope = _scopeFactory.CreateAsyncScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var user = await userManager.FindByIdAsync(userId.Value.ToString());
                if (user is null)
                {
                    return OperationResult.Fail("User not found.");
                }

                var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!result.Succeeded)
                {
                    return OperationResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
                }

                return OperationResult.Ok("Password updated.");
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<OperationResult<string?>> UpdateAvatarAsync(string contentType, byte[] content)
        {
            await _lock.WaitAsync();
            try
            {
                var userId = _currentUserService.UserId;
                var tenantId = _tenantContext.TenantId;
                if (!userId.HasValue || !tenantId.HasValue)
                {
                    return OperationResult<string?>.Fail("Unauthorized.");
                }

                await using var scope = _scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                var avatar = await dbContext.UserAvatars.SingleOrDefaultAsync(a => a.UserId == userId.Value);
                if (avatar is null)
                {
                    avatar = new UserAvatar
                    {
                        UserId = userId.Value,
                        TenantId = tenantId.Value
                    };
                    dbContext.UserAvatars.Add(avatar);
                }

                avatar.ContentType = contentType.ToLowerInvariant();
                avatar.SizeBytes = content.Length;
                avatar.Content = content;
                avatar.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                var dataUrl = $"data:{avatar.ContentType};base64,{Convert.ToBase64String(avatar.Content)}";
                return OperationResult<string?>.Ok(dataUrl, "Avatar updated.");
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<OperationResult> RemoveAvatarAsync()
        {
            await _lock.WaitAsync();
            try
            {
                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return OperationResult.Fail("Unauthorized.");
                }

                await using var scope = _scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MoneyTrackerDbContext>();

                var avatar = await dbContext.UserAvatars.SingleOrDefaultAsync(a => a.UserId == userId.Value);
                if (avatar is not null)
                {
                    dbContext.UserAvatars.Remove(avatar);
                    await dbContext.SaveChangesAsync();
                }

                return OperationResult.Ok("Avatar removed.");
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync(MoneyTrackerDbContext dbContext, bool asNoTracking)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return null;
            }

            IQueryable<ApplicationUser> query = dbContext.Users;
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (_tenantContext.TenantId.HasValue)
            {
                var tenantId = _tenantContext.TenantId.Value;
                query = query.Where(u => u.TenantId == tenantId);
            }

            return await query.SingleOrDefaultAsync(u => u.Id == userId.Value);
        }
    }
}
