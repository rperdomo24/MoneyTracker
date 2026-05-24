# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## First read

Before making changes, read:
- `/docs/PROJECT_MAP.md` — entity relationships, data paths, full architecture
- `/docs/CONVENTIONS.md` — patterns, constraints, naming rules

---

## Project overview

**MoneyTracker** — personal finance tracker. Multi-tenant, invite-only beta. Features: accounts, hierarchical categories, transactions (with timezone-aware filters), paired transfers, monthly budgets with forecasting, dashboard with net-worth trend + spending charts, full auth lifecycle (OTP, email verification, invite tokens).

**Stack**: ASP.NET Core 9 + Blazor Server + MudBlazor + ApexCharts + PostgreSQL 16 + EF Core 9 + ASP.NET Identity + FluentValidation + Serilog

---

## Build / run / test commands

```bash
# Restore
dotnet restore MoneyTracker.sln

# Run (http://localhost:5229 / https://localhost:7238)
dotnet run --project MoneyTracker.UI

# Test
dotnet test MoneyTracker.Tests

# Single test class
dotnet test MoneyTracker.Tests --filter "FullyQualifiedName~ServiceClassName"

# UI build safety check (required before UI commit)
dotnet build MoneyTracker.UI/MoneyTracker.UI.csproj --no-restore

# Docker (port 8091)
docker compose up --build -d
docker compose down

# Docker with bundled PostgreSQL
docker compose --profile internal-db up --build -d

# EF Core migrations (run from repo root)
dotnet ef migrations add <Name> --project MoneyTracker.Infrastructure --startup-project MoneyTracker.UI
dotnet ef database update --project MoneyTracker.Infrastructure --startup-project MoneyTracker.UI
```

---

## Architecture layers

```
MoneyTracker.UI             Blazor pages/components, DI root, auth endpoints, appsettings
MoneyTracker.Application    Services, DTOs, mappers, validators, OperationResult
MoneyTracker.Domain         Entities, enums, repository interfaces (zero dependencies)
MoneyTracker.Infrastructure EF Core DbContext, repositories, migrations, TimeZoneService
MoneyTracker.Tests          xUnit + Moq, service-layer unit tests
```

**Strict dependency rule**: UI → Application → Domain ← Infrastructure. UI must never reference `MoneyTracker.Infrastructure` types directly.

**Key entry points**:
- `MoneyTracker.UI/Program.cs` — DI composition, Identity config, Serilog, MudBlazor
- `MoneyTracker.UI/Endpoints/AuthEndpoints.cs` — login / OTP / password-reset / invite
- `MoneyTracker.UI/Components/Pages/` — one subfolder per feature

---

## Architecture rules

- UI depends on `Application` interfaces only — never inject Infrastructure types into Razor components.
- Domain interfaces define contracts; Infrastructure implements them.
- No architecture boundary changes without explicit user approval.
- New cross-cutting infrastructure (caching, messaging, etc.) requires confirmation before introduction.

---

## Naming conventions

| Artifact | Pattern | Example |
|---|---|---|
| Service interface | `I<Feature>Service` | `ITransactionService` |
| Service implementation | `<Feature>Service` | `TransactionService` |
| Repository interface | `I<Feature>Repository` | `ITransactionRepository` |
| Repository implementation | `<Feature>Repository` | `TransactionRepository` |
| DTO | `<Feature>Dto` | `TransactionDto` |
| Validator | `<Feature>Validator` | `TransactionValidator` |
| Mapper file | `<Feature>Mapper.cs` | `TransactionMapper.cs` |
| UI page dir | `Components/Pages/<Feature>/` | `Components/Pages/Transactions/` |
| Shared UI | `Components/Shared/` | — |

---

## Service layer conventions

- Every service method returns `OperationResult` or `OperationResult<T>`.
- Use static factories only:
  ```csharp
  OperationResult.Ok() / OperationResult.Fail("message")
  OperationResult<T>.Ok(data, message) / OperationResult<T>.Fail("message")
  ```
- Shared messages live in `MoneyTracker.Application.Common.OperationMessages` — reuse before hardcoding strings.
- No `throw` for expected business failures; use `OperationResult.Fail`.
- Interfaces in `Application/Interfaces/`, implementations in `Application/Services/`.
- Register every new service in `MoneyTracker.UI/Program.cs`.

---

## Data access conventions

- Repository interfaces defined in `MoneyTracker.Domain/Interfaces/`.
- Implementations in `MoneyTracker.Infrastructure/Persistence/Repositories/`.
- DbContext: `MoneyTrackerDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.
- Tenancy enforced via EF Core global query filters on `TenantId` — do not bypass them.
- **Concurrency rule**: Blazor Server can trigger parallel component refreshes. Each repository operation must use an isolated `DbContext` instance. Never share an active EF operation across concurrent calls.
- UTC storage only — conversions happen at the service/mapper layer via `ITimeZoneService`.
- Migrations live in `MoneyTracker.Infrastructure/Migrations/`; always generate through the CLI commands above.

---

## DTO / mapper / validator conventions

**DTOs**
- Defined in `Application/DTOs/`.
- Suffix: `Dto` (e.g., `AccountDto`, `BudgetDto`).

**Mappers**
- Explicit extension methods — no AutoMapper, ever.
- Method names: `MapToDto`, `MapToEntity`, `UpdateEntity`.
- Files in `Application/Mappers/` (sub-folders for complex features, e.g., `Mappers/Transactions/`).
- Never map ad-hoc in UI components or service constructors.

**Validators**
- `AbstractValidator<T>` subclasses, grouped by feature: `Application/Validators/<Feature>/`.
- Registered explicitly in `Program.cs` (not via assembly scanning).

---

## OperationResult rules

```csharp
// Void success / failure
OperationResult.Ok()
OperationResult.Fail("Human-readable message")

// Typed success / failure
OperationResult<T>.Ok(data)
OperationResult<T>.Ok(data, "Optional message")
OperationResult<T>.Fail("Human-readable message")
```

UI reads `result.IsSuccess`, `result.Message`, `result.Data`. Services never surface exception stack traces to callers — log internally, return safe generic message.

---

## UI conventions

- Framework: Blazor Server + MudBlazor.
- Feature pages: `Components/Pages/<Feature>/`. Shared components: `Components/Shared/`.
- **Dialog pattern is fixed** — do not change structure:
  ```razor
  <MudDialog>
    <TitleContent>...</TitleContent>
    <DialogContent>...</DialogContent>
    <DialogActions>...</DialogActions>
  </MudDialog>
  ```
- Auth pages: `@layout Layout.AuthLayout`.
- Branding assets: always via `IBrandingService` + `BrandingAsset`. No hardcoded `/images/...` paths.
- Snackbar: inject `ISnackbar`; global provider already in `MainLayout.razor`.
- Reuse shared helpers: `CurrencyHelper`, filter helpers, dialog helpers — do not duplicate view logic.
- Keep component nullability explicit (`string.Empty` over `null` for non-nullable strings).
- Mobile containers: `pa-1 pa-sm-6` baseline for full-width pages unless intentionally different.
- Complex inline Razor expressions → small helper method/property instead.

**Auth forms**:
- Plain HTML `<form method="post">` + `<AntiforgeryToken />`.
- Every endpoint-bound value needs explicit `name` attribute on input.
- Do not rely on Blazor component state for values expected by auth endpoints.

**Email links** (server-generated): use `ApplicationSettings.PublicBaseUrl` — never request-host fallback in production.

---

## Timezone and DateTime rules

- Store UTC; present local time only in UI.
- Single conversion source: `ITimeZoneService` (`ConvertToUtc`, `ConvertFromUtc`, `GetNowInUtc`).
- Date filters / period aggregations computed in local time boundaries, not raw UTC.
- Service and mapper layers own conversions — UI must not duplicate timezone math.

---

## Logging and security

- Auth / email / OTP failures → `IErrorLogService`:
  - Exceptions: `LogExceptionAsync(exception, customMessage)`
  - Handled failures: `LogMessageAsync(...)`
- Auth lifecycle events → `IAuthAuditService`: action, outcome, reason, masked identity, IP, user-agent, path, traceId.
- Never persist raw passwords, OTP values, invite tokens, or full email addresses in logs.
- User-facing error responses: safe generic message. Internal logs: full exception context.

---

## Testing conventions

- Tests live in `MoneyTracker.Tests/Services/`.
- Every new or changed service behavior needs: one success-path test + one failure-path test minimum.
- Use Moq for dependencies; no test should hit a real database or external service.
- No feature is complete without passing tests for all impacted service paths.
- Run `dotnet test MoneyTracker.Tests` before marking work done.

---

## New feature checklist

1. Domain entity + `I<Feature>Repository` interface
2. `<Feature>Repository` in `Infrastructure/Persistence/Repositories/`
3. `<Feature>Dto` in `Application/DTOs/`
4. Mapper: `MapToDto`, `MapToEntity`, `UpdateEntity` in `Application/Mappers/`
5. `<Feature>Validator` in `Application/Validators/<Feature>/`
6. `<Feature>Service` returning `OperationResult` in `Application/Services/`
7. Register service + validator in `Program.cs`
8. UI page/dialog in `Components/Pages/<Feature>/` using MudBlazor dialog pattern
9. Unit tests in `MoneyTracker.Tests/Services/` for success + failure paths

---

## Things Claude must never do

- Introduce AutoMapper or any mapping library.
- Reference `MoneyTracker.Infrastructure` from UI pages/components.
- Change the MudBlazor dialog structure (`MudDialogTitle/MudDialogContent/MudDialogActions` variants).
- Add secrets, tokens, API keys, or personal data to any file.
- Change architecture boundaries without explicit user approval.
- Use exception-driven control flow for expected business failures.
- Map DTOs inline in UI or service constructors — always use mapper extension methods.
- Hardcode `/images/...` paths in components — use `IBrandingService`.
- Duplicate timezone math in UI — use `ITimeZoneService`.
- Commit a formatting-only or comment-only diff — revert it first.
- Ignore `MUD*` analyzer warnings — treat as actionable errors.
- Introduce new nullable warnings (`CS860*`, `CS862*`, `CS8669`) without approval.
- Share a `DbContext` instance across concurrent Blazor operations.

---

## Areas that need confirmation before proceeding

- Any change to architecture layer boundaries or dependency direction.
- Introducing a new third-party package not already in the solution.
- Adding new infrastructure concerns (caching, background jobs, messaging, search).
- Changes to auth flows, identity schema, or tenancy enforcement.
- EF Core schema migrations that drop or rename columns.
- Changes to `Program.cs` middleware pipeline order.
- Cross-feature changes that touch more than one domain module.

---

## Configuration files

| File | Purpose |
|---|---|
| `MoneyTracker.UI/appsettings.json` | DB connection, SMTP, logging levels, `PublicBaseUrl`, date formats |
| `MoneyTracker.UI/appsettings.Development.json` | Dev overrides |
| `.env` / `.env.example` | Docker: DB creds, timezone, SMTP, `ASPNETCORE_ENVIRONMENT` |
| `docker-compose.yml` | Web on port 8091 + optional internal PostgreSQL + external network |
| `MoneyTracker.sln` | Solution root (5 projects) |
