# MoneyTracker Project Map

## What MoneyTracker is
- MoneyTracker is a personal finance tracker built as a .NET 9 Blazor Server app.
- It manages accounts, categories, transactions, transfers, budgets, and dashboard metrics.
- UI is implemented with MudBlazor components and dialog-based CRUD flows.
- Application services expose business operations and return `OperationResult` / `OperationResult<T>`.
- Domain contains entities, enums, constants, and repository interfaces.
- Infrastructure contains EF Core PostgreSQL persistence, repositories, migrations, and timezone service.
- Tests focus on application services (`Account`, `Category`, `Transaction`, `Budget`, `Dashboard`, `TimeRange`, `TextImport`).
- DI composition currently happens in the UI entry point (`Program.cs`), not in separate extension modules.
- Data is persisted through `MoneyTrackerDbContext` and repositories.
- App-level timezone handling is centralized via `ITimeZoneService`.

## Architecture
```text
MoneyTracker.UI (Blazor Server + MudBlazor, DI composition, appsettings)
    -> MoneyTracker.Application (DTOs, services, validators, mappers, OperationResult)
        -> MoneyTracker.Domain (entities, enums, constants, repository interfaces)
            <- MoneyTracker.Infrastructure (EF Core DbContext, PostgreSQL repos, migrations, timezone service)
MoneyTracker.Tests -> tests Application services with mocks
```

## Folder and project map
- `MoneyTracker.UI`: `Program.cs`, Razor pages/components, UI state services, filters, dialogs, app settings.
- `MoneyTracker.Application`: service layer, DTOs, FluentValidation validators, mapping extensions, shared result/message types.
- `MoneyTracker.Domain`: entities (`Account`, `Category`, `Transaction`, `Budget`), enums, constants, repository contracts.
- `MoneyTracker.Infrastructure`: `MoneyTrackerDbContext`, repositories, migrations, seeders, `TimeZoneService`.
- `MoneyTracker.Tests`: xUnit + Moq tests for service behavior.
- `docker-compose.yml` + `Dockerfile`: containerized app runtime/build definition.

## Where to start reading (top 7 files)
- `MoneyTracker.UI/Program.cs`: DI wiring, DB provider, MudBlazor/snackbar setup, hosted app pipeline.
- `MoneyTracker.Infrastructure/Persistence/MoneyTrackerDbContext.cs`: data model, relationships, constraints, seeding.
- `MoneyTracker.Application/Common/OperationResult.cs`: service return contract pattern.
- `MoneyTracker.Application/Services/TransactionService.cs`: largest business flow surface (CRUD, filters, transfers, duplication).
- `MoneyTracker.Application/Mappers/Transactions/TransactionMapper.cs`: explicit map conventions and timezone conversion behavior.
- `MoneyTracker.UI/Components/Pages/Transactions/Transactions.razor`: primary end-user CRUD/filter flow in UI.
- `MoneyTracker.UI/Components/Pages/Overview/Overview.razor`: dashboard orchestration and module composition.

## Main business flows found (top 5)
- Accounts CRUD + balance sync/adjustment (`Accounts.razor`, `AccountService.cs`).
- Categories CRUD + parent/child support (`Categories.razor`, `CategoryService.cs`).
- Transactions CRUD + filtering + bulk delete (`Transactions.razor`, `TransactionService.cs`).
- Transfers (paired transactions) create/update/delete/duplicate (`AddTransactionModal.razor`, `TransactionService.cs`, `TransferMapper.cs`).
- Budgets monthly management + usage overview (`Budgets.razor`, `BudgetService.cs`).

## Timezone handling notes
- App registers `ITimeZoneService` as singleton in `MoneyTracker.UI/Program.cs`.
- Default timezone comes from `ApplicationSettings.DefaultTimeZone` in configuration.
- Transaction mapping converts UTC <-> local in mappers (`TransactionMapper.cs`).
- Date range filtering for periods/custom ranges uses `TimeRangeService` + timezone conversion.
- `TimeZoneService` falls back to UTC if configured timezone is invalid (`Infrastructure/Services/TimeZoneService.cs`).
