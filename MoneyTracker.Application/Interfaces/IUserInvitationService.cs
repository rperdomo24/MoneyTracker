using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Auth;

namespace MoneyTracker.Application.Interfaces
{
    public interface IUserInvitationService
    {
        Task<OperationResult<UserInvitationResultDto>> CreateOrResendAsync(CreateUserInvitationDto dto, CancellationToken cancellationToken = default);
        Task<OperationResult<UserInvitationDetailsDto>> GetValidInvitationAsync(string token, CancellationToken cancellationToken = default);
        Task<OperationResult> MarkAcceptedAsync(int invitationId, CancellationToken cancellationToken = default);
    }
}
