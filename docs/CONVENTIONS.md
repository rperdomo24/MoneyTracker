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
