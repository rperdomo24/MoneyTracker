# MoneyTracker Agent Guide

## First read
Always read these files before making changes:
- `/docs/PROJECT_MAP.md`
- `/docs/CONVENTIONS.md`

---

## Project overview

Personal finance tracker. Multi-tenant, invite-only beta. Domains: accounts, hierarchical categories, timezone-aware transactions, paired transfers, monthly budgets with forecasting, dashboard (net-worth trend + spending charts), full auth lifecycle (OTP, email verification, invite tokens).

**Stack**: ASP.NET Core 9 + Blazor Server + MudBlazor + ApexCharts + PostgreSQL 16 + EF Core 9 + ASP.NET Identity + FluentValidation + Serilog

---

## Build, run, test

```bash
dotnet restore MoneyTracker.sln
dotnet run --project MoneyTracker.UI
dotnet test MoneyTracker.Tests
dotnet test MoneyTracker.Tests --filter "FullyQualifiedName~ServiceClassName"
dotnet build MoneyTracker.UI/MoneyTracker.UI.csproj --no-restore   # UI safety check
docker compose up --build -d
docker compose down
docker compose --profile internal-db up --build -d

# Migrations (from repo root)
dotnet ef migrations add <Name> --project MoneyTracker.Infrastructure --startup-project MoneyTracker.UI
dotnet ef database update --project MoneyTracker.Infrastructure --startup-project MoneyTracker.UI
```

---

## Architecture layers

```
MoneyTracker.UI             Blazor pages/components, DI root, auth endpoints, appsettings
MoneyTracker.Application    Services, DTOs, mappers, validators, OperationResult
MoneyTracker.Domain         Entities, enums, repository interfaces (zero dependencies)
MoneyTracker.Infrastructure EF Core, repositories, migrations, TimeZoneService
MoneyTracker.Tests          xUnit + Moq, service-layer tests
```

Dependency direction: UI → Application → Domain ← Infrastructure. UI never touches Infrastructure.

---

## Architecture rules

- UI depends on `Application` interfaces only.
- Domain defines repository interfaces; Infrastructure implements them.
- No architecture boundary changes without explicit user approval.
- New cross-cutting infrastructure requires confirmation before introduction.

---

## Naming conventions

| Artifact | Pattern |
|---|---|
| Service interface / impl | `I<Feature>Service` / `<Feature>Service` |
| Repository interface / impl | `I<Feature>Repository` / `<Feature>Repository` |
| DTO | `<Feature>Dto` |
| Validator | `<Feature>Validator` |
| Mapper file | `<Feature>Mapper.cs` |
| UI page dir | `Components/Pages/<Feature>/` |
| Shared UI | `Components/Shared/` |

---

## Service layer conventions

- All service methods return `OperationResult` or `OperationResult<T>`.
- Static factories only:
  ```csharp
  OperationResult.Ok() / OperationResult.Fail("message")
  OperationResult<T>.Ok(data) / OperationResult<T>.Ok(data, "message") / OperationResult<T>.Fail("message")
  ```
- Reuse messages from `MoneyTracker.Application.Common.OperationMessages` before hardcoding strings.
- No `throw` for expected business failures — use `OperationResult.Fail`.
- Interfaces in `Application/Interfaces/`; implementations in `Application/Services/`.
- Register every new service in `MoneyTracker.UI/Program.cs`.

---

## Data access conventions

- Repository interfaces: `MoneyTracker.Domain/Interfaces/`
- Implementations: `MoneyTracker.Infrastructure/Persistence/Repositories/`
- DbContext: `MoneyTrackerDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`
- Tenancy via EF Core global query filters on `TenantId` — do not bypass.
- **Concurrency**: each repository operation must use an isolated `DbContext` instance. Never share an active EF operation across concurrent Blazor calls.
- UTC storage only. Conversions at service/mapper layer via `ITimeZoneService`.

---

## DTO / mapper / validator conventions

**DTOs**: `Application/DTOs/`, suffix `Dto`.

**Mappers**: explicit extension methods — never AutoMapper.
- Method names: `MapToDto`, `MapToEntity`, `UpdateEntity`.
- Files: `Application/Mappers/` (sub-folders for complex features).
- Never map ad-hoc in UI components or service constructors.

**Validators**: `AbstractValidator<T>` subclasses in `Application/Validators/<Feature>/`. Registered explicitly in `Program.cs`.

---

## OperationResult rules

```csharp
OperationResult.Ok()
OperationResult.Fail("Human-readable message")
OperationResult<T>.Ok(data)
OperationResult<T>.Ok(data, "message")
OperationResult<T>.Fail("Human-readable message")
```

UI reads `result.IsSuccess`, `result.Message`, `result.Data`. Never surface stack traces to callers — log internally, return safe generic message.

---

## UI conventions

- Blazor Server + MudBlazor. Feature pages: `Components/Pages/<Feature>/`.
- **Dialog pattern is fixed**:
  ```razor
  <MudDialog>
    <TitleContent>...</TitleContent>
    <DialogContent>...</DialogContent>
    <DialogActions>...</DialogActions>
  </MudDialog>
  ```
- Auth pages: `@layout Layout.AuthLayout`.
- Branding assets: always via `IBrandingService` + `BrandingAsset`. No hardcoded `/images/...` paths.
- Snackbar: inject `ISnackbar`; global provider in `MainLayout.razor`.
- Reuse `CurrencyHelper`, filter helpers, dialog helpers — do not duplicate view logic.
- Complex inline Razor expressions → small helper method/property.
- Mobile containers: `pa-1 pa-sm-6` baseline for full-width pages.
- Auth forms: plain HTML `<form method="post">` + `<AntiforgeryToken />`. Every endpoint-bound value needs explicit `name` attribute.
- Email links: `ApplicationSettings.PublicBaseUrl` — never request-host fallback in production.

---

## Timezone and DateTime rules

- Store UTC; present local only in UI.
- Single source: `ITimeZoneService` (`ConvertToUtc`, `ConvertFromUtc`, `GetNowInUtc`).
- Service + mapper layers own conversions. UI must not duplicate timezone math.
- Date filters and aggregations computed in local time boundaries.

---

## Logging and security rules

- Auth / email / OTP failures → `IErrorLogService`:
  - Exceptions: `LogExceptionAsync(exception, customMessage)`
  - Handled failures: `LogMessageAsync(...)`
- Auth lifecycle → `IAuthAuditService`: action, outcome, reason, masked identity, IP, user-agent, path, traceId.
- Never persist raw passwords, OTP values, invite tokens, or full email addresses.
- User-facing: safe generic message. Internal logs: full exception context.

---

## Testing conventions

- Tests in `MoneyTracker.Tests/Services/`.
- New or changed service behavior needs success-path test + failure-path test minimum.
- Moq for dependencies — no real DB or external service calls.
- Run `dotnet test MoneyTracker.Tests` before marking work done.

---

## New CRUD checklist

1. Domain entity + `I<Feature>Repository` interface
2. `<Feature>Repository` in `Infrastructure/Persistence/Repositories/`
3. `<Feature>Dto` in `Application/DTOs/`
4. Mapper: `MapToDto`, `MapToEntity`, `UpdateEntity` in `Application/Mappers/`
5. `<Feature>Validator` in `Application/Validators/<Feature>/`
6. `<Feature>Service` methods returning `OperationResult` in `Application/Services/`
7. Register service + validator in `Program.cs`
8. UI page/dialog in `Components/Pages/<Feature>/` using fixed dialog pattern
9. Unit tests in `MoneyTracker.Tests/Services/` for success + failure paths

---

## Hard constraints — never do these

- Introduce AutoMapper or any mapping library.
- Reference `MoneyTracker.Infrastructure` from UI pages/components.
- Change MudBlazor dialog structure.
- Add secrets, tokens, API keys, or personal data to any file.
- Change architecture boundaries without explicit user approval.
- Use exception-driven control flow for expected business failures.
- Map DTOs inline in UI or service constructors.
- Hardcode `/images/...` paths in components.
- Duplicate timezone math in UI.
- Commit formatting-only or comment-only diffs.
- Ignore `MUD*` analyzer warnings.
- Introduce new nullable warnings (`CS860*`, `CS862*`, `CS8669`) without approval.
- Share a `DbContext` instance across concurrent Blazor operations.

---

## Areas requiring confirmation before proceeding

- Architecture boundary or dependency direction changes.
- Introducing new third-party packages.
- New infrastructure concerns (caching, background jobs, messaging, search).
- Auth flow, identity schema, or tenancy enforcement changes.
- EF Core migrations that drop or rename columns.
- Changes to `Program.cs` middleware pipeline order.
- Cross-feature changes touching more than one domain module.

---

## UI safety checks (required before UI commit)

- `dotnet build MoneyTracker.UI/MoneyTracker.UI.csproj --no-restore` must pass.
- Treat `MUD*` analyzer warnings as actionable — fix, do not suppress.
- No new nullable warnings in UI code (`CS860*`, `CS862*`, `CS8669`) without approval.
- If a diff is formatting-only or comment-only, revert before commit.

---

## Where to add things

| What | Where |
|---|---|
| UI pages / dialogs | `MoneyTracker.UI/Components/Pages/<Feature>/` |
| Shared UI components | `MoneyTracker.UI/Components/Shared/` |
| Service interfaces | `MoneyTracker.Application/Interfaces/` |
| Service implementations | `MoneyTracker.Application/Services/` |
| DTOs | `MoneyTracker.Application/DTOs/` |
| Validators | `MoneyTracker.Application/Validators/<Feature>/` |
| Mappers | `MoneyTracker.Application/Mappers/` |
| Domain entities / enums | `MoneyTracker.Domain/Entities/` / `MoneyTracker.Domain/Enums/` |
| Repository interfaces | `MoneyTracker.Domain/Interfaces/` |
| Repository implementations | `MoneyTracker.Infrastructure/Persistence/Repositories/` |
| EF migrations | `MoneyTracker.Infrastructure/Migrations/` |
| Unit tests | `MoneyTracker.Tests/Services/` |
