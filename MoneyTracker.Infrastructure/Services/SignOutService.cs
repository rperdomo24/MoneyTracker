using Microsoft.AspNetCore.Identity;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Infrastructure.Persistence;

namespace MoneyTracker.Infrastructure.Services
{
    public class SignOutService : ISignOutService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuthAuditService _authAuditService;

        public SignOutService(SignInManager<ApplicationUser> signInManager, IAuthAuditService authAuditService)
        {
            _signInManager = signInManager;
            _authAuditService = authAuditService;
        }

        public async Task SignOutAsync(Guid? userId, Guid? tenantId, RequestContextDto context, CancellationToken cancellationToken = default)
        {
            await _authAuditService.LogAsync(new AuthAuditEntryDto
            {
                CreatedAtUtc = DateTime.UtcNow,
                Action = AuthAuditConstants.Logout,
                Outcome = AuthAuditConstants.OutcomeSuccess,
                UserId = userId,
                TenantId = tenantId,
                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,
                HttpMethod = context.HttpMethod,
                Path = context.Path,
                TraceId = context.TraceId
            }, cancellationToken);

            await _signInManager.SignOutAsync();
        }
    }
}
