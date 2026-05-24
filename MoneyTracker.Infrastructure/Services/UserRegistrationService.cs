using Microsoft.AspNetCore.Identity;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Services
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MoneyTrackerDbContext _dbContext;
        private readonly ITenantBootstrapService _tenantBootstrapService;
        private readonly IUserInvitationService _userInvitationService;
        private readonly IErrorLogService _errorLogService;

        public UserRegistrationService(
            UserManager<ApplicationUser> userManager,
            MoneyTrackerDbContext dbContext,
            ITenantBootstrapService tenantBootstrapService,
            IUserInvitationService userInvitationService,
            IErrorLogService errorLogService)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _tenantBootstrapService = tenantBootstrapService;
            _userInvitationService = userInvitationService;
            _errorLogService = errorLogService;
        }

        public async Task<OperationResult> RegisterPublicAsync(string displayName, string email, string password, CancellationToken cancellationToken = default)
        {
            var maskedEmail = MaskEmail(email);
            var tenantId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                TenantId = tenantId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                TwoFactorEnabled = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var error = string.Join(" ", createResult.Errors.Select(x => x.Description));
                await _errorLogService.LogMessageAsync($"Public registration user creation failed for {maskedEmail}. {error}", "Warning", "Registration", cancellationToken);
                return OperationResult.Fail(error);
            }

            try
            {
                _dbContext.Tenants.Add(new Tenant
                {
                    TenantId = tenantId,
                    Name = BuildTenantName(displayName, email),
                    OwnerUserId = user.Id
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
                await _tenantBootstrapService.SeedDefaultsAsync(tenantId, cancellationToken);
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                await _errorLogService.LogExceptionAsync(ex, $"Public registration tenant setup failed for {maskedEmail}.", cancellationToken: cancellationToken);
                await _userManager.DeleteAsync(user);
                return OperationResult.Fail("Registration failed. Please try again.");
            }
        }

        public async Task<OperationResult> RegisterFromInvitationAsync(string displayName, string password, UserInvitationDetailsDto invitation, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = invitation.Email.Trim().ToLowerInvariant();
            var maskedEmail = MaskEmail(normalizedEmail);
            var tenantId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = true,
                TenantId = tenantId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                TwoFactorEnabled = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var error = string.Join(" ", createResult.Errors.Select(x => x.Description));
                await _errorLogService.LogMessageAsync($"Invite registration user creation failed for {maskedEmail}. {error}", "Warning", "Registration", cancellationToken);
                return OperationResult.Fail(error);
            }

            try
            {
                _dbContext.Tenants.Add(new Tenant
                {
                    TenantId = tenantId,
                    Name = BuildTenantName(displayName, normalizedEmail),
                    OwnerUserId = user.Id
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
                await _tenantBootstrapService.SeedDefaultsAsync(tenantId, cancellationToken);

                var acceptResult = await _userInvitationService.MarkAcceptedAsync(invitation.InvitationId, cancellationToken);
                if (!acceptResult.Success)
                {
                    await _errorLogService.LogMessageAsync($"Invite registration accept marker failed for {maskedEmail}. {acceptResult.Message}", "Warning", "Registration", cancellationToken);
                    await _userManager.DeleteAsync(user);
                    return OperationResult.Fail(acceptResult.Message);
                }

                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                await _errorLogService.LogExceptionAsync(ex, $"Invite registration tenant setup failed for {maskedEmail}.", cancellationToken: cancellationToken);
                await _userManager.DeleteAsync(user);
                return OperationResult.Fail("Registration failed. Please try again.");
            }
        }

        private static string BuildTenantName(string? displayName, string normalizedEmail)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName.Trim();

            var atIndex = normalizedEmail.IndexOf('@');
            if (atIndex > 0)
            {
                var localPart = normalizedEmail[..atIndex].Trim();
                if (!string.IsNullOrWhiteSpace(localPart))
                    return localPart;
            }

            return "Personal";
        }

        public async Task<bool> UserExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            return user is not null;
        }

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "***";
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1) return "***";
            return $"{email[0]}***{email[(atIndex - 1)..]}";
        }
    }
}
