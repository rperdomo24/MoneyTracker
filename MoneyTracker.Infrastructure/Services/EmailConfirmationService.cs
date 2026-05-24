using Microsoft.AspNetCore.Identity;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Infrastructure.Persistence;
using System.Text;

namespace MoneyTracker.Infrastructure.Services
{
    public class EmailConfirmationService : IEmailConfirmationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSenderService _emailSenderService;

        public EmailConfirmationService(UserManager<ApplicationUser> userManager, IEmailSenderService emailSenderService)
        {
            _userManager = userManager;
            _emailSenderService = emailSenderService;
        }

        public async Task<OperationResult> ConfirmEmailAsync(string? userId, string? code, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out var parsedUserId) || string.IsNullOrWhiteSpace(code))
                return OperationResult.Fail("Confirmation parameters are missing.");

            var user = await _userManager.FindByIdAsync(parsedUserId.ToString());
            if (user is null)
                return OperationResult.Fail("User not found.");

            var decodedToken = Encoding.UTF8.GetString(Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
                return OperationResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));

            return OperationResult.Ok();
        }

        public async Task<OperationResult<bool>> ResendConfirmationEmailAsync(string normalizedEmail, string publicBaseUrl, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user is null)
                return OperationResult<bool>.Ok(false);

            if (await _userManager.IsEmailConfirmedAsync(user))
                return OperationResult<bool>.Ok(true);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var confirmationUrl = $"{publicBaseUrl.TrimEnd('/')}/confirm-email?userId={Uri.EscapeDataString(user.Id.ToString())}&code={Uri.EscapeDataString(encodedToken)}";

            var sendResult = await _emailSenderService.SendAsync(
                user.Email!,
                "Confirm your MoneyTracker email",
                $"<p>Confirm your account:</p><p><a href=\"{confirmationUrl}\">{confirmationUrl}</a></p>");

            if (!sendResult.Success)
                return OperationResult<bool>.Fail(string.IsNullOrEmpty(sendResult.Message) ? "Failed to send confirmation email." : sendResult.Message);

            return OperationResult<bool>.Ok(false);
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var base64 = input.Replace('-', '+').Replace('_', '/');
            var padding = (4 - base64.Length % 4) % 4;
            base64 = base64 + new string('=', padding);
            return Convert.FromBase64String(base64);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}
