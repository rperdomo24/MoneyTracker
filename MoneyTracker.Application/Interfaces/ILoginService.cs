using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Interfaces
{
    public interface ILoginService
    {
        Task<OperationResult<PasswordLoginResultDto>> LoginAsync(
            string email,
            string password,
            bool rememberMe,
            RequestContextDto context,
            CancellationToken cancellationToken = default);
    }
}
