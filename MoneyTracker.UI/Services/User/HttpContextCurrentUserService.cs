using System.Security.Claims;
using MoneyTracker.Application.Constants;
using MoneyTracker.Application.Interfaces;

namespace MoneyTracker.UI.Services.User
{
    public class HttpContextCurrentUserService : ICurrentUserService, ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public Guid? TenantId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(CustomClaimTypes.TenantId);
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public bool IsAuthenticated
            => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    }
}
