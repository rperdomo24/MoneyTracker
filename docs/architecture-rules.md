# Architecture Rules

Binding rules for all contributors and AI agents. Do not deviate without explicit owner approval.

---

## Layer dependency rules

```
MoneyTracker.UI
  └─ depends on → MoneyTracker.Application (interfaces only)
                   └─ depends on → MoneyTracker.Domain
MoneyTracker.Infrastructure
  └─ depends on → MoneyTracker.Domain
```

**Rule 1 — UI never touches Infrastructure.**
Razor pages and components must inject `Application` service interfaces (`IFooService`). Never inject repositories, `DbContext`, or any `Infrastructure` type into a Blazor component.

**Rule 2 — Business logic lives in Application services.**
Services in `MoneyTracker.Application/Services/` own all business rules. Razor components call services; they do not contain validation logic, entity construction, or data-shaping.

**Rule 3 — Domain has no external dependencies.**
`MoneyTracker.Domain` references no other project and no third-party packages. Entities and interfaces here are pure C#.

**Rule 4 — Infrastructure implements Domain contracts.**
Every `IFooRepository` is defined in Domain; its implementation lives in Infrastructure. The UI layer sees only the interface.

---

## OperationResult rules

All service methods return `OperationResult` or `OperationResult<T>`. No exceptions for expected business failures.

```csharp
// Correct
OperationResult.Ok()
OperationResult.Fail("Message from OperationMessages or explicit string")
OperationResult<T>.Ok(data)
OperationResult<T>.Ok(data, "Optional message")
OperationResult<T>.Fail("Message")

// Wrong — do not throw for expected failures
throw new InvalidOperationException("...");
```

- Reuse `MoneyTracker.Application.Common.OperationMessages` before hardcoding strings.
- UI reads `result.IsSuccess`, `result.Message`, `result.Data`.
- Services return generic safe messages to callers; full exception context goes to `IErrorLogService`.

---

## Mapping rules

Mapping between layers is explicit and centralized.

- Method names: `MapToDto`, `MapToEntity`, `UpdateEntity`.
- Files: `MoneyTracker.Application/Mappers/` (sub-folders for complex features).
- **No AutoMapper.** No other mapping library. Ever.
- Never map inline inside a Razor component or service constructor.
- When adding a new DTO or entity, add the mapper extension method in the correct mapper file first.

---

## Validation rules

- FluentValidation only. Validators: `AbstractValidator<T>` subclass, suffix `Validator`.
- Location: `MoneyTracker.Application/Validators/<Feature>/`.
- Registration: explicit in `MoneyTracker.UI/Program.cs` — no assembly scanning.
- Services call validators explicitly; UI does not duplicate service-level validation.

---

## Data access rules

**No direct repository calls from UI.**
Repositories are injected into services. Blazor components never call `IFooRepository` directly.

**Tenancy is non-negotiable.**
EF Core global query filters on `TenantId` enforce tenant isolation. Do not bypass with `.IgnoreQueryFilters()` unless explicitly approved for admin-level operations.

**UTC-only storage.**
All `DateTime` values persisted to the database are UTC. Conversion to local time happens only at the service/mapper layer via `ITimeZoneService`. Never store or filter by local time.

**DbContext concurrency.**
Blazor Server can trigger simultaneous component refreshes. Each repository operation must use an isolated `DbContext` instance. Do not share an active EF Core operation across concurrent calls — it will throw.

---

## UI pattern rules

**Dialog structure is fixed.** Do not change, extend, or replace:
```razor
<MudDialog>
  <TitleContent>...</TitleContent>
  <DialogContent>...</DialogContent>
  <DialogActions>...</DialogActions>
</MudDialog>
```

**Auth pages** must use `@layout Layout.AuthLayout`. Do not duplicate branding or shell elements per page.

**Auth form postbacks** use plain HTML:
```html
<form method="post">
  <AntiforgeryToken />
  <input name="fieldName" ... />
</form>
```
Every value the endpoint binds must have an explicit `name` attribute. Do not rely on Blazor component state for postback values.

**Branding assets** are resolved through `IBrandingService` + `BrandingAsset`. No hardcoded `/images/...` paths in any component.

**Snackbar** via `ISnackbar`. Global provider already in `MainLayout.razor` — do not add a second one.

**Shared helpers** (`CurrencyHelper`, filter helpers, dialog helpers) must be reused. Do not duplicate formatting or color logic in components.

**MudBlazor analyzer warnings** (`MUD*`) are treated as errors. Fix invalid parameters — never suppress.

---

## Logging and security rules

**Auth and email flows** must persist failures through `IErrorLogService`:
- Exceptions: `LogExceptionAsync(exception, customMessage)`
- Handled failures: `LogMessageAsync(...)`

**Auth lifecycle events** (login success/fail, OTP, password reset) must be persisted through `IAuthAuditService` with: action, outcome, reason, masked identity, IP, user-agent, path, traceId.

**Never persist:**
- Raw passwords
- OTP values
- Invite tokens
- Full email addresses

**Email links** generated on the server must use `ApplicationSettings.PublicBaseUrl`. Never use request-host as fallback in production.

---

## Scope rules — keep changes minimal

- Fix the stated problem. Do not refactor surrounding code.
- Do not introduce abstractions for single use-cases.
- Do not add error handling for impossible scenarios.
- Do not add feature flags.
- Three similar lines is acceptable. A premature abstraction is not.
- Formatting-only diffs must be reverted before commit.

---

## Things that require explicit approval before proceeding

> Mark assumptions in this category as **"Needs confirmation"** and stop until answered.

1. Architecture boundary or dependency direction changes.
2. Introducing a new third-party package not already in the solution.
3. New infrastructure concerns: caching, background jobs, messaging, full-text search.
4. Auth flow changes, identity schema changes, or tenancy enforcement changes.
5. EF Core migrations that **drop or rename** columns.
6. Changes to `Program.cs` middleware pipeline order.
7. Cross-feature changes touching more than one domain module simultaneously.
8. Any change to global EF query filters (tenancy, soft-delete).
9. Changes to existing mapper method signatures (`MapToDto`, `MapToEntity`, `UpdateEntity`) that affect multiple callers.

---

## Never do

| Action | Reason |
|---|---|
| Inject `IFooRepository` into a Razor component | Bypasses service layer; business logic leaks into UI |
| Use AutoMapper or any mapping library | Explicit mappers are required for traceability |
| `throw` for expected business failures | Use `OperationResult.Fail` |
| Map DTOs inline in UI or service constructors | Centralizes mapping, prevents drift |
| Store local `DateTime` in database | Use UTC everywhere except display |
| Share `DbContext` across concurrent Blazor calls | EF Core throws on concurrent operations |
| Hardcode `/images/...` paths in components | Use `IBrandingService` |
| Bypass EF global query filters without approval | Breaks tenant isolation |
| Suppress `MUD*` analyzer warnings | They indicate invalid component usage |
| Commit formatting-only or comment-only diffs | Noise in history, revert first |
| Add secrets or credentials to any tracked file | Security violation |
| Broad refactors without explicit scope agreement | Risk of regressions across features |
