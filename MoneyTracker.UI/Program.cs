using FluentValidation;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Application.Constants.Configuration;
using MoneyTracker.Application.DTOs;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Application.Services;
using MoneyTracker.Application.Validators;
using MoneyTracker.Application.Validators.Transaction;
using MoneyTracker.Domain.Interfaces;
using MoneyTracker.Infrastructure.Persistence;
using MoneyTracker.Infrastructure.Persistence.Repositories;
using MoneyTracker.Infrastructure.Services;
using MoneyTracker.UI.Components;
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

            var cultureInfo = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            builder.Host.UseSerilog();

            builder.Services.Configure<ApplicationSettings>(
            builder.Configuration.GetSection("ApplicationSettings"));
            builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();

            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<IValidator<CreateTransferDto>, CreateTransferValidator>();
            builder.Services.AddScoped<IValidator<CategoryDto>, CategoryValidator>();
            builder.Services.AddScoped<IValidator<AccountDto>, AccountValidator>();
            builder.Services.AddScoped<IValidator<TransactionDto>, TransactionValidator>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();


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

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
