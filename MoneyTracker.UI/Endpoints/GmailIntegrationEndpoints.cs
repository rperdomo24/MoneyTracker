using Microsoft.Extensions.Options;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.Interfaces;

namespace MoneyTracker.UI.Endpoints
{
    public static class GmailIntegrationEndpoints
    {
        private const string StateCookieName = "gmail_oauth_state";

        public static WebApplication MapGmailIntegrationEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/integrations/gmail").RequireAuthorization();

            group.MapGet("/connect", (HttpContext context, IGmailApiClient client) =>
            {
                var state = Guid.NewGuid().ToString("N");
                context.Response.Cookies.Append(StateCookieName, state, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromMinutes(10)
                });

                return Results.Redirect(client.BuildAuthorizationUrl(state));
            });

            group.MapGet("/callback", async (
                HttpContext context,
                string? code,
                string? state,
                string? error,
                IEmailImportService emailImportService,
                IErrorLogService errorLogService) =>
            {
                var expectedState = context.Request.Cookies[StateCookieName];
                context.Response.Cookies.Delete(StateCookieName);

                if (!string.IsNullOrWhiteSpace(error))
                    return Results.Redirect(BuildRedirect(false, "Google sign-in was cancelled or denied."));

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state) ||
                    string.IsNullOrWhiteSpace(expectedState) || state != expectedState)
                {
                    await errorLogService.LogMessageAsync("Gmail OAuth callback failed state validation.");
                    return Results.Redirect(BuildRedirect(false, "Invalid or expired sign-in request. Please try again."));
                }

                var result = await emailImportService.CompleteConnectionAsync(code);
                return Results.Redirect(BuildRedirect(result.Success, result.Message));
            });

            return app;
        }

        private static string BuildRedirect(bool success, string message) =>
            $"/settings?emailImport={(success ? "connected" : "error")}&message={Uri.EscapeDataString(message)}#email-import";
    }
}
