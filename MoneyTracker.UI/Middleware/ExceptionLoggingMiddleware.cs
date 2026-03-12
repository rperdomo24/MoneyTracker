using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.Interfaces;

namespace MoneyTracker.UI.Middleware
{
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IErrorLogService errorLogService,
            ITenantContext tenantContext,
            ICurrentUserService currentUserService,
            IWebHostEnvironment environment)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var entry = new ExceptionLogEntryDto
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    Level = "Error",
                    ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException?.ToString(),
                    HttpMethod = context.Request.Method,
                    Path = context.Request.Path.Value,
                    QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                    TraceId = context.TraceIdentifier,
                    UserId = currentUserService.UserId,
                    TenantId = tenantContext.TenantId,
                    Environment = environment.EnvironmentName
                };

                await errorLogService.LogAsync(entry, context.RequestAborted);
                throw;
            }
        }
    }
}
