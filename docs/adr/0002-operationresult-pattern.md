# ADR 0002: OperationResult Pattern

- Status: Accepted (observed in current repository).
- Date: 2026-03-05.

## Context
- Service methods need a consistent way to return success/failure and user-facing messages.
- UI consumes service calls directly and displays snackbar messages based on results.

## Decision
- Use `OperationResult` and `OperationResult<T>` as standard service return types.
- Use `OperationMessages` constants for reusable operation messages.

## Evidence
- `MoneyTracker.Application/Common/OperationResult.cs`
- `MoneyTracker.Application/Common/OperationMessages.cs`
- Service examples:
  - `MoneyTracker.Application/Services/AccountService.cs`
  - `MoneyTracker.Application/Services/CategoryService.cs`
  - `MoneyTracker.Application/Services/TransactionService.cs`
  - `MoneyTracker.Application/Services/BudgetService.cs`
  - `MoneyTracker.Application/Services/DashboardService.cs`

## Consequences
- Callers can branch on `Success` without exception-based normal flow.
- Messages are standardized across services and UI notifications.
- Exception handling still exists for unexpected errors, but result contract remains stable.
