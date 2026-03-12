using MoneyTracker.Application.Interfaces;

namespace MoneyTracker.UI.Endpoints
{
    public static class DiagnosticsEndpoints
    {
        public static WebApplication MapDiagnosticsEndpoints(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                return app;
            }

            var diagnosticsGroup = app.MapGroup("/diagnostics");

            diagnosticsGroup.MapGet("/smtp", (IEmailSenderService emailSenderService) =>
            {
                var validation = emailSenderService.ValidateConfiguration();
                return Results.Json(new
                {
                    success = validation.Success,
                    message = validation.Message,
                    checkedAtUtc = DateTime.UtcNow
                });
            }).AllowAnonymous();

            return app;
        }
    }
}
