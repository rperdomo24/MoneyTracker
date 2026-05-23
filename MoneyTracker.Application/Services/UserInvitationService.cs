using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services
{
    public class UserInvitationService : IUserInvitationService
    {
        private const int InvitationExpiryDays = 7;

        private readonly IUserInvitationRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IErrorLogService _errorLogService;
        private readonly IValidator<CreateUserInvitationDto> _validator;
        private readonly ILogger<UserInvitationService> _logger;

        public UserInvitationService(
            IUserInvitationRepository repository,
            ICurrentUserService currentUserService,
            IEmailSenderService emailSenderService,
            IErrorLogService errorLogService,
            IValidator<CreateUserInvitationDto> validator,
            ILogger<UserInvitationService> logger)
        {
            _repository = repository;
            _currentUserService = currentUserService;
            _emailSenderService = emailSenderService;
            _errorLogService = errorLogService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<OperationResult<UserInvitationResultDto>> CreateOrResendAsync(CreateUserInvitationDto dto, CancellationToken cancellationToken = default)
        {
            var validation = await _validator.ValidateAsync(dto, cancellationToken);
            if (!validation.IsValid)
            {
                await _errorLogService.LogMessageAsync(validation.Errors.First().ErrorMessage, "Warning", "InvitationValidation");
                return OperationResult<UserInvitationResultDto>.Fail(validation.Errors.First().ErrorMessage);
            }

            if (!_currentUserService.IsAuthenticated)
            {
                await _errorLogService.LogMessageAsync("Invitation send attempt rejected because the user is not authenticated.", "Warning", "InvitationUnauthorized");
                return OperationResult<UserInvitationResultDto>.Fail("You must be signed in to send invitations.");
            }

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var maskedEmail = MaskEmail(normalizedEmail);
            var token = Guid.NewGuid().ToString("N");
            var tokenHash = ComputeTokenHash(token);
            var inviteUrl = dto.InviteUrlTemplate.Replace("{token}", Uri.EscapeDataString(token), StringComparison.Ordinal);
            var nowUtc = DateTime.UtcNow;

            try
            {
                var invitation = await _repository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
                if (invitation is not null && invitation.AcceptedAtUtc.HasValue)
                {
                    await _errorLogService.LogMessageAsync($"Invitation resend rejected because {maskedEmail} already accepted an invitation.", "Warning", "InvitationAlreadyAccepted", cancellationToken);
                    return OperationResult<UserInvitationResultDto>.Fail("This email already accepted an invitation.");
                }

                if (invitation is null)
                {
                    invitation = new UserInvitation
                    {
                        Email = normalizedEmail,
                        NormalizedEmail = normalizedEmail,
                        InvitedByUserId = _currentUserService.UserId,
                        CreatedAtUtc = nowUtc
                    };

                    ApplyRefresh(invitation, tokenHash, nowUtc);
                    await _repository.AddAsync(invitation, cancellationToken);
                }
                else
                {
                    invitation.Email = normalizedEmail;
                    invitation.InvitedByUserId = _currentUserService.UserId;
                    ApplyRefresh(invitation, tokenHash, nowUtc);
                    await _repository.UpdateAsync(invitation, cancellationToken);
                }

                var emailSent = false;
                var sendResult = await _emailSenderService.SendAsync(
                    normalizedEmail,
                    "You're invited to try MoneyTracker",
                    BuildInviteEmailBody(inviteUrl, invitation.ExpiresAtUtc),
                    cancellationToken);

                if (sendResult.Success)
                {
                    emailSent = true;
                }
                else
                {
                    _logger.LogWarning("Invitation email could not be delivered to {Email}. Reason: {Reason}", normalizedEmail, sendResult.Message);
                    await _errorLogService.LogMessageAsync($"Invitation email could not be delivered to {maskedEmail}. {sendResult.Message}", "Warning", "InvitationEmailFailure", cancellationToken);
                }

                return OperationResult<UserInvitationResultDto>.Ok(
                    new UserInvitationResultDto
                    {
                        Email = normalizedEmail,
                        InviteUrl = inviteUrl,
                        EmailSent = emailSent
                    },
                    emailSent
                        ? "Invitation sent successfully."
                        : "Invitation created, but the email could not be sent. Share the invite link manually.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invitation for {Email}", normalizedEmail);
                await _errorLogService.LogExceptionAsync(ex, $"Error creating invitation for {maskedEmail}.", cancellationToken: cancellationToken);
                return OperationResult<UserInvitationResultDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult<UserInvitationDetailsDto>> GetValidInvitationAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                await _errorLogService.LogMessageAsync("Invitation validation failed because the token was empty.", "Warning", "InvitationTokenMissing", cancellationToken);
                return OperationResult<UserInvitationDetailsDto>.Fail("Invitation token is required.");
            }

            try
            {
                var invitation = await _repository.GetPendingByTokenHashAsync(ComputeTokenHash(token.Trim()), cancellationToken);
                if (invitation is null || invitation.ExpiresAtUtc <= DateTime.UtcNow)
                {
                    await _errorLogService.LogMessageAsync("Invitation validation failed because the token was invalid or expired.", "Warning", "InvitationInvalidOrExpired", cancellationToken);
                    return OperationResult<UserInvitationDetailsDto>.Fail("This invitation is invalid or expired.");
                }

                return OperationResult<UserInvitationDetailsDto>.Ok(new UserInvitationDetailsDto
                {
                    InvitationId = invitation.Id,
                    Email = invitation.Email,
                    ExpiresAtUtc = invitation.ExpiresAtUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating invitation token.");
                await _errorLogService.LogExceptionAsync(ex, "Error validating invitation token.", cancellationToken: cancellationToken);
                return OperationResult<UserInvitationDetailsDto>.Fail(OperationMessages.UnexpectedError);
            }
        }

        public async Task<OperationResult> MarkAcceptedAsync(int invitationId, CancellationToken cancellationToken = default)
        {
            if (invitationId <= 0)
            {
                await _errorLogService.LogMessageAsync("Invitation acceptance failed because the invitation id was invalid.", "Warning", "InvitationInvalidId", cancellationToken);
                return OperationResult.Fail("Invitation was not found.");
            }

            try
            {
                var invitation = await _repository.GetByIdAsync(invitationId, cancellationToken);
                if (invitation is null)
                {
                    await _errorLogService.LogMessageAsync($"Invitation acceptance failed because invitation {invitationId} was not found.", "Warning", "InvitationNotFound", cancellationToken);
                    return OperationResult.Fail("Invitation was not found.");
                }

                invitation.AcceptedAtUtc = DateTime.UtcNow;
                invitation.UpdatedAtUtc = invitation.AcceptedAtUtc.Value;
                await _repository.UpdateAsync(invitation, cancellationToken);
                return OperationResult.Ok("Invitation accepted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invitation {InvitationId} as accepted.", invitationId);
                await _errorLogService.LogExceptionAsync(ex, $"Error marking invitation {invitationId} as accepted.", cancellationToken: cancellationToken);
                return OperationResult.Fail(OperationMessages.UnexpectedError);
            }
        }

        private static void ApplyRefresh(UserInvitation invitation, string tokenHash, DateTime nowUtc)
        {
            invitation.TokenHash = tokenHash;
            invitation.LastSentAtUtc = nowUtc;
            invitation.UpdatedAtUtc = nowUtc;
            invitation.ExpiresAtUtc = nowUtc.AddDays(InvitationExpiryDays);
            invitation.AcceptedAtUtc = null;
        }

        private static string ComputeTokenHash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private static string BuildInviteEmailBody(string inviteUrl, DateTime expiresAtUtc)
        {
            return $"""
                <p>You were invited to join the MoneyTracker beta.</p>
                <p><a href="{inviteUrl}">Create your account</a></p>
                <p>This invitation expires on {expiresAtUtc:u} UTC.</p>
                """;
        }

        private static string MaskEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1)
            {
                return "***";
            }

            return $"{email[0]}***{email[(atIndex - 1)..]}";
        }
    }
}
