using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Budgets;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Application.Validators;
using MoneyTracker.Application.Validators.Budgets;
using MoneyTracker.Application.Validators.Auth;
using MoneyTracker.Application.Validators.Transaction;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;
using MoneyTracker.Infrastructure.Persistence.Repositories;
using MoneyTracker.Infrastructure.Services;
using MoneyTracker.UI.Components;
using MoneyTracker.UI.Services.Components.Drawer;
using MoneyTracker.UI.Services.Filters;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Settings;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using System.Globalization;

namespace MoneyTracker.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
            });

            builder.Services.AddDbContext<MoneyTrackerDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            }, ServiceLifetime.Scoped);

            builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 3;

                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<MoneyTrackerDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddSingleton<ILookupNormalizer, LowerInvariantLookupNormalizer>();
            builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
            builder.Services.AddScoped<ITenantContext>(sp => (ITenantContext)sp.GetRequiredService<ICurrentUserService>());
            builder.Services.AddScoped<ITenantBootstrapService, TenantBootstrapService>();
            builder.Services.AddScoped<ISystemCategoryResolver, SystemCategoryResolver>();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/login";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
            });
            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();

            var cultureInfo = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            builder.Host.UseSerilog();

            builder.Services.Configure<UISettings>(builder.Configuration.GetSection("UISettings"));

            builder.Services.AddScoped<ICurrentUserKeyProvider, CurrentUserKeyProvider>();
            builder.Services.AddScoped<ProtectedSessionStorage>();
            builder.Services.AddScoped<IFilterStorageService, FilterStorageService>();
            builder.Services.AddScoped<IAccountFilterStateService, AccountFilterStateService>();
            builder.Services.AddScoped<ITransactionFilterStateService, TransactionFilterStateService>();
            builder.Services.AddScoped<IBudgetFilterStateService, BudgetFilterStateService>();
            builder.Services.AddScoped<IAccountsPanelStateService, AccountsPanelStateService>();

            builder.Services.Configure<ApplicationSettings>(
            builder.Configuration.GetSection("ApplicationSettings"));
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();
            builder.Services.AddScoped<IEmailSenderService, SmtpEmailSenderService>();
            builder.Services.AddScoped<ITimeRangeService, TimeRangeService>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<IValidator<CreateTransferDto>, CreateTransferValidator>();
            builder.Services.AddScoped<IValidator<LoginRequestDto>, LoginRequestValidator>();
            builder.Services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestValidator>();
            builder.Services.AddScoped<IValidator<LoginOtpRequestDto>, LoginOtpRequestValidator>();
            builder.Services.AddScoped<IValidator<CategoryDto>, CategoryValidator>();
            builder.Services.AddScoped<IValidator<AccountDto>, AccountValidator>();
            builder.Services.AddScoped<IValidator<TransactionDto>, TransactionValidator>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();

            builder.Services.AddScoped<IBudgetRepository, BudgetRepository>();
            builder.Services.AddScoped<IBudgetService, BudgetService>();
            builder.Services.AddScoped<IValidator<CreateBudgetDto>, CreateBudgetValidator>();
            builder.Services.AddScoped<IValidator<UpdateBudgetDto>, UpdateBudgetValidator>();

            builder.Services.AddScoped<ITextImportService, TextImportService>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
            builder.Services.AddScoped<AccountsDrawerState>();
            builder.Services.AddScoped<AccountsRefreshBus>();


            builder.Services.AddScoped<ProtectedSessionStorage>();

            Log.Logger = new LoggerConfiguration()
                            .WriteTo.Console()
                            .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
                            .CreateLogger();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseAntiforgery();

            app.MapPost("/auth/login", async (
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                SignInManager<ApplicationUser> signInManager,
                UserManager<ApplicationUser> userManager,
                IEmailSenderService emailSenderService,
                IVerificationCodeService verificationCodeService,
                IValidator<LoginRequestDto> validator) =>
            {
                var dto = new LoginRequestDto
                {
                    Email = email,
                    Password = password,
                    RememberMe = rememberMe
                };

                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, error, null));
                }

                var normalizedEmail = email.Trim().ToLowerInvariant();
                var result = await signInManager.PasswordSignInAsync(
                    normalizedEmail,
                    password,
                    rememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    return Results.LocalRedirect(SafeReturnUrl(returnUrl));
                }

                if (result.RequiresTwoFactor)
                {
                    var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                    if (user is not null)
                    {
                        var codeResult = await verificationCodeService.IssueCodeAsync(
                            user.Id,
                            user.TenantId,
                            Application.Constants.AuthVerificationPurposes.LoginOtp,
                            expiryMinutes: 10,
                            resendCooldownSeconds: 60,
                            maxAttempts: 3);

                        if (!codeResult.Success || codeResult.Data is null)
                        {
                            return Results.LocalRedirect(BuildLoginUrl(returnUrl, codeResult.Message, null));
                        }

                        await emailSenderService.SendAsync(
                            user.Email!,
                            "Your MoneyTracker login code",
                            $"<p>Your OTP code is:</p><h2>{codeResult.Data.Code}</h2><p>The code expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");
                    }

                    var otpUrl = $"/login-otp?returnUrl={Uri.EscapeDataString(SafeReturnUrl(returnUrl))}&rememberMe={rememberMe.ToString().ToLowerInvariant()}";
                    return Results.LocalRedirect(otpUrl);
                }

                if (result.IsLockedOut)
                {
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Your account is temporarily locked. Try again later.", null));
                }

                if (result.IsNotAllowed)
                {
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&error={Uri.EscapeDataString("Email confirmation is required before sign in.")}");
                }

                return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Invalid email or password.", null));
            }).AllowAnonymous();

            app.MapPost("/auth/login-otp", async (
                [FromForm] string code,
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                SignInManager<ApplicationUser> signInManager,
                UserManager<ApplicationUser> userManager,
                IVerificationCodeService verificationCodeService,
                IValidator<LoginOtpRequestDto> validator) =>
            {
                var dto = new LoginOtpRequestDto { Code = code };
                var validation = await validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, error, null));
                }

                var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user is null)
                {
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Session expired. Please sign in again.", null));
                }

                if (await userManager.IsLockedOutAsync(user))
                {
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, "Your account is temporarily locked. Try again later.", null));
                }

                var cleanedCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);
                var verifyResult = await verificationCodeService.VerifyCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.LoginOtp,
                    cleanedCode);

                if (!verifyResult.Success)
                {
                    await userManager.AccessFailedAsync(user);
                    if (await userManager.IsLockedOutAsync(user))
                    {
                        return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, "Your account is temporarily locked. Try again later.", null));
                    }

                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, verifyResult.Message, null));
                }

                await userManager.ResetAccessFailedCountAsync(user);
                await signInManager.SignInAsync(user, rememberMe);
                return Results.LocalRedirect(SafeReturnUrl(returnUrl));
            }).AllowAnonymous();

            app.MapPost("/auth/login-otp/resend", async (
                [FromForm] bool rememberMe,
                [FromForm] string? returnUrl,
                SignInManager<ApplicationUser> signInManager,
                IEmailSenderService emailSenderService,
                IVerificationCodeService verificationCodeService) =>
            {
                var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user is null)
                {
                    return Results.LocalRedirect(BuildLoginUrl(returnUrl, "Session expired. Please sign in again.", null));
                }

                var codeResult = await verificationCodeService.IssueCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.LoginOtp,
                    expiryMinutes: 10,
                    resendCooldownSeconds: 60,
                    maxAttempts: 3);

                if (!codeResult.Success || codeResult.Data is null)
                {
                    return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, codeResult.Message, null));
                }

                await emailSenderService.SendAsync(
                    user.Email!,
                    "Your MoneyTracker login code",
                    $"<p>Your OTP code is:</p><h2>{codeResult.Data.Code}</h2><p>The code expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

                return Results.LocalRedirect(BuildOtpUrl(returnUrl, rememberMe, null, "A verification code was sent to your email."));
            }).AllowAnonymous();

            app.MapPost("/auth/resend-confirmation", async (
                [FromForm] string email,
                UserManager<ApplicationUser> userManager,
                IEmailSenderService emailSenderService) =>
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null)
                {
                    return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&info={Uri.EscapeDataString("If the email exists, a confirmation link was sent.")}");
                }

                if (await userManager.IsEmailConfirmedAsync(user))
                {
                    return Results.LocalRedirect(BuildLoginUrl(null, null, "Email already confirmed. You can sign in."));
                }

                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
                var confirmationUrl = $"/confirm-email?userId={Uri.EscapeDataString(user.Id.ToString())}&code={Uri.EscapeDataString(encodedToken)}";
                await emailSenderService.SendAsync(user.Email!, "Confirm your MoneyTracker email", $"<p>Confirm your account:</p><p><a href=\"{confirmationUrl}\">{confirmationUrl}</a></p>");

                return Results.LocalRedirect($"/verify-email-pending?email={Uri.EscapeDataString(normalizedEmail)}&info={Uri.EscapeDataString("Confirmation link sent. Check your email.")}");
            }).AllowAnonymous();

            app.MapPost("/auth/forgot-password", async (
                [FromForm] string email,
                UserManager<ApplicationUser> userManager,
                IEmailSenderService emailSenderService,
                IVerificationCodeService verificationCodeService) =>
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null || !await userManager.IsEmailConfirmedAsync(user))
                {
                    return Results.LocalRedirect("/forgot-password?info=If%20the%20account%20exists%2C%20a%20reset%20code%20was%20sent.");
                }

                var codeResult = await verificationCodeService.IssueCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.PasswordReset,
                    expiryMinutes: 10,
                    resendCooldownSeconds: 60,
                    maxAttempts: 3);

                if (!codeResult.Success || codeResult.Data is null)
                {
                    return Results.LocalRedirect($"/forgot-password?error={Uri.EscapeDataString(codeResult.Message)}");
                }

                await emailSenderService.SendAsync(
                    user.Email!,
                    "MoneyTracker password reset code",
                    $"<p>Your reset code is:</p><h2>{codeResult.Data.Code}</h2><p>Expires at {codeResult.Data.ExpiresAtUtc:u} UTC.</p>");

                return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(user.Email!)}&info={Uri.EscapeDataString("A reset code was sent to your email.")}");
            }).AllowAnonymous();

            app.MapPost("/auth/reset-password", async (
                [FromForm] string email,
                [FromForm] string code,
                [FromForm] string newPassword,
                [FromForm] string confirmPassword,
                UserManager<ApplicationUser> userManager,
                IVerificationCodeService verificationCodeService) =>
            {
                if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                {
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString("Password confirmation does not match.")}");
                }

                var normalizedEmail = email.Trim().ToLowerInvariant();
                var user = await userManager.FindByEmailAsync(normalizedEmail);
                if (user is null)
                {
                    return Results.LocalRedirect("/login?info=Password%20updated%20if%20the%20account%20exists.");
                }

                var verifyResult = await verificationCodeService.VerifyCodeAsync(
                    user.Id,
                    user.TenantId,
                    Application.Constants.AuthVerificationPurposes.PasswordReset,
                    code.Replace(" ", string.Empty).Replace("-", string.Empty));

                if (!verifyResult.Success)
                {
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString(verifyResult.Message)}");
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, token, newPassword);
                if (!resetResult.Succeeded)
                {
                    var error = string.Join(" ", resetResult.Errors.Select(x => x.Description));
                    return Results.LocalRedirect($"/reset-password?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString(error)}");
                }

                return Results.LocalRedirect("/login?info=Password%20updated%20successfully.");
            }).AllowAnonymous();

            app.MapGet("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.LocalRedirect("/login");
            }).AllowAnonymous();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();

            static string SafeReturnUrl(string? returnUrl)
                => !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') ? returnUrl : "/";

            static string BuildLoginUrl(string? returnUrl, string? error, string? info)
            {
                var safeReturnUrl = Uri.EscapeDataString(SafeReturnUrl(returnUrl));
                var url = $"/login?returnUrl={safeReturnUrl}";

                if (!string.IsNullOrWhiteSpace(error))
                {
                    url += $"&error={Uri.EscapeDataString(error)}";
                }

                if (!string.IsNullOrWhiteSpace(info))
                {
                    url += $"&info={Uri.EscapeDataString(info)}";
                }

                return url;
            }

            static string BuildOtpUrl(string? returnUrl, bool rememberMe, string? error, string? info)
            {
                var safeReturnUrl = Uri.EscapeDataString(SafeReturnUrl(returnUrl));
                var url = $"/login-otp?returnUrl={safeReturnUrl}&rememberMe={rememberMe.ToString().ToLowerInvariant()}";

                if (!string.IsNullOrWhiteSpace(error))
                {
                    url += $"&error={Uri.EscapeDataString(error)}";
                }

                if (!string.IsNullOrWhiteSpace(info))
                {
                    url += $"&info={Uri.EscapeDataString(info)}";
                }

                return url;
            }
        }
    }
}
