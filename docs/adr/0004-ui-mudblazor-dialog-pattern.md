# ADR 0004: UI MudBlazor Dialog Pattern

- Status: Accepted (observed in current repository).
- Date: 2026-03-05.

## Context
- Multiple feature modals/dialogs exist across Accounts, Categories, Transactions, Budgets, and shared components.
- A consistent dialog structure reduces UI drift and rework.

## Decision
- Keep dialog structure as:
  - `<MudDialog>`
  - `<TitleContent>`
  - `<DialogContent>`
  - `<DialogActions>`
- Continue using `IDialogService` to open feature dialogs.

## Evidence
- Shared dialogs:
  - `MoneyTracker.UI/Components/Shared/ConfirmDialog.razor`
  - `MoneyTracker.UI/Components/Shared/InputDialog.razor`
- Feature dialogs/modals:
  - `MoneyTracker.UI/Components/Pages/Accounts/AddAccountModal.razor`
  - `MoneyTracker.UI/Components/Pages/Categories/AddCategoryModal.razor`
  - `MoneyTracker.UI/Components/Pages/Transactions/AddTransactionModal.razor`
  - `MoneyTracker.UI/Components/Pages/Budgets/UpdateBudgetModal.razor`

## Assumption
- This ADR documents current repository convention, not an externally mandated framework rule.

## Consequences
- Future dialogs should follow this structure unless a repo-wide standard migration is approved.
- Existing helper methods (`DialogHelper`, `DialogHelpers`) stay compatible.
