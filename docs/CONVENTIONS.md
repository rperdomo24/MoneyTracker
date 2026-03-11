# Conventions

## Naming conventions
- DTOs use `Dto` suffix (example: `AccountDto`, `TransactionDto`, `BudgetDto`).
- Services use `*Service` naming and interfaces use `I*Service`.
- Repositories use `*Repository` naming and interfaces use `I*Repository`.
- Validators use `*Validator` and inherit `AbstractValidator<T>`.
- Mapper extension classes live under `MoneyTracker.Application/Mappers`.

## OperationResult rules
- Application service methods return `OperationResult` or `OperationResult<T>`.
- Use static factories:
  - `OperationResult.Ok(...)` / `OperationResult.Fail(...)`
  - `OperationResult<T>.Ok(data, message)` / `OperationResult<T>.Fail(message)`
- Shared operation messages are centralized in:
  - `MoneyTracker.Application.Common.OperationMessages`
- Keep service-level flow result-based (avoid exception-driven normal control flow).

## Mapping rules
- Mapping is explicit and centralized via extension methods:
  - `MapToDto`, `MapToEntity`, `UpdateEntity`.
- Main mapper locations:
  - `MoneyTracker.Application/Mappers/*`
  - `MoneyTracker.Application/Mappers/Transactions/*`
- `AutoMapper` is not used (no `AutoMapper` references found).

## Architecture boundary rules
- UI (`MoneyTracker.UI`) must depend on `MoneyTracker.Application` contracts only.
- Do not inject or reference `MoneyTracker.Infrastructure` types directly in Razor components/pages.
- Persistence and Identity implementation details stay in `MoneyTracker.Infrastructure`.

## Testing rules
- Every new service behavior must include unit tests for success and failure paths.
- Add or update tests under `MoneyTracker.Tests/Services` when adding/altering application use-cases.
- No feature is complete without validating impacted tests.

## Logging and security rules
- Every new auth, registration, invitation, OTP, or email flow must persist controlled failures and unexpected exceptions through `IErrorLogService`.
- Prefer `LogExceptionAsync(exception, customMessage)` for exceptions and `LogMessageAsync(...)` for handled failures.
- Do not persist raw passwords, OTP values, invitation tokens, or full email addresses in application error logs.
- For persisted auth/email logs, prefer masked email text or existing user and tenant ids.
- Email links sent from the server must use `ApplicationSettings.PublicBaseUrl` in non-development environments.
- Do not rely on request-host fallback for production email links.

## Commenting rules
- Keep comments in English only.
- Add comments only when they provide non-obvious, high-value context.
- Avoid redundant comments that restate obvious code.

## Validator rules
- Validation library: FluentValidation.
- Validators are registered explicitly in `MoneyTracker.UI/Program.cs`.
- Validators are grouped by feature (`Validators/Budgets`, `Validators/Transaction`, etc.).

## UI conventions
- UI stack: Blazor Server + MudBlazor (`MudBlazor` package in `MoneyTracker.UI.csproj`).
- Dialog structure in this repo uses:
  - `<MudDialog><TitleContent>...<DialogContent>...<DialogActions>...</MudDialog>`
- Snackbar pattern:
  - `ISnackbar` injected into pages/components.
  - Global provider in `MainLayout.razor` (`<MudSnackbarProvider />`).

## Razor and MudBlazor guardrails
- Use only documented MudBlazor component parameters for the installed version in this repo.
- Do not use legacy/invalid params (for example, deprecated aliases) when analyzer reports `MUD0002`.
- In Razor attributes, avoid mixed markup/escaped-string expressions that can break parsing.
- For dynamic classes/text with string arguments, prefer small helper properties/methods over complex inline expressions.
- Keep component nullability explicit (`string.Empty` over `null` for non-nullable strings, null-guard optional references).
- For transfer/pair components, guard nullable paired objects before dereferencing.
- Reuse shared helpers (`CurrencyHelper`, transaction type helpers, filter helpers) to keep UI behavior consistent.

## Do and don't
- Do return `OperationResult` from services.
- Do reuse `OperationMessages` when possible.
- Do keep mapping in mapper extension files.
- Do use FluentValidation validators for DTO validation.
- Do register dependencies in `Program.cs` consistently.
- Do convert dates through `ITimeZoneService` for transaction-related flows.
- Do follow existing MudBlazor dialog structure.
- Do keep feature UI under `MoneyTracker.UI/Components/Pages/<Feature>`.
- Don't introduce AutoMapper.
- Don't bypass mappers with ad-hoc property mapping in UI.
- Don't add secrets to docs/config commits.
- Don't change architecture boundaries without explicit agreement.
- Don't replace dialog pattern with `MudDialogTitle/MudDialogContent/MudDialogActions` unless repo standard changes.
- Don't commit UI changes without a successful `dotnet build MoneyTracker.UI/MoneyTracker.UI.csproj --no-restore`.
