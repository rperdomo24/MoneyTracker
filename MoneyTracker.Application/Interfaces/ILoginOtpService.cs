using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Interfaces
{
    public interface ILoginOtpService
    {
        Task<OperationResult<OtpVerifyResultDto>> VerifyOtpAsync(
            string code,
            bool rememberMe,
            RequestContextDto context,
            CancellationToken cancellationToken = default);

        Task<OperationResult<OtpResendResultDto>> ResendOtpAsync(
            RequestContextDto context,
            CancellationToken cancellationToken = default);
    }
}
