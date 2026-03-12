# Requirements Snapshot

## Implemented features detected
- Dashboard page (`/`, `/overview`) with summary, cash flow, alerts, breakdown, recent activity.
- Accounts module (`/accounts`, `/accounts/{id:int}`):
  - Create, update, delete, adjust balance, sync balance.
- Categories module (`/categories`):
  - Create/update/delete, parent-child management, type grouping.
- Transactions module (`/transactions`):
  - Create/update/delete, filters, pagination, bulk delete, duplicate.
- Transfers:
  - Create/update/delete paired transfer transactions.
- Budgets module (`/budgets`):
  - Monthly listing, create/update, usage view, allocation/transactions dialogs.
- Text import dialog/service:
  - Analyze text and suggest transaction items.

## Key modules status requested
- Accounts: Implemented.
- Categories: Implemented.
- Transactions: Implemented.
- Budgets: Implemented, but delete action in UI is pending.
- Dashboard: Implemented with partial mocked calculations in trend logic.
- Settings: Present as UI page, mostly placeholder controls, no clear persistence flow.

## Missing or placeholder modules
- Budget deletion UI flow: placeholder message `"Delete not implemented yet"` in `Budgets.razor`.
- Dashboard balance trend uses simplified mock variation in `DashboardService`.
- Settings page actions (backup/restore/export/security) appear UI-only placeholders.
- Calendar page appears sample/mock data based.
- Counter/Weather pages are template/demo pages, not core finance features.

## Non-goals (not found in codebase)
- No public REST API project found.
- No authentication/authorization pipeline found in `Program.cs`.
- No background worker/queue service project found.
- No AutoMapper integration found.

## TODO/FIXME/TBD summary
- `TODO/FIXME/TBD` tokens: no first-party matches found in core projects.
- Relevant implementation markers:
  - `MoneyTracker.UI/Components/Pages/Budgets/Budgets.razor`: delete not implemented.
  - `MoneyTracker.Application/Services/DashboardService.cs`: comments indicate mock/simplified trend logic.
  - `MoneyTracker.UI/Components/Layout/MainLayout.razor`: quick balance noted as mock in comment.
