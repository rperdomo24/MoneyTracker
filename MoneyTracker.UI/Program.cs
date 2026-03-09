using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
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
using MoneyTracker.UI.Endpoints;
using MoneyTracker.UI.Services.Components.Drawer;
using MoneyTracker.UI.Services.Filters;
using MoneyTracker.UI.Services.Filters.Interface;
using MoneyTracker.UI.Services.User;
using MoneyTracker.UI.Utility.Settings;
using ApexCharts;
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
            builder.Services.AddApexCharts();

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
            builder.Services.AddScoped<IDashboardFilterStateService, DashboardFilterStateService>();
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

            app.MapAuthEndpoints();
            app.MapDiagnosticsEndpoints();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
