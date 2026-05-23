# MoneyTracker Agent Guide

## First read
- Always read these files before making changes:
  - `/docs/PROJECT_MAP.md`
  - `/docs/CONVENTIONS.md`

## Build, run, test
- Restore: `dotnet restore MoneyTracker.sln`
- Run app: `dotnet run --project MoneyTracker.UI`
- Run tests: `dotnet test MoneyTracker.Tests`
- Docker: `docker compose up --build -d`

## Where to add new features
- UI pages/components/dialogs:
  - `MoneyTracker.UI/Components/Pages/<Feature>`
  - Shared UI: `MoneyTracker.UI/Components/Shared`
- Application use-cases/services:
  - `MoneyTracker.Application/Services`
  - Service interfaces: `MoneyTracker.Application/Interfaces`
- DTOs/validators/mappers:
  - `MoneyTracker.Application/DTOs`
  - `MoneyTracker.Application/Validators`
  - `MoneyTracker.Application/Mappers`
- Domain model/contracts:
  - `MoneyTracker.Domain/Entities`
  - `MoneyTracker.Domain/Interfaces`
  - `MoneyTracker.Domain/Enums`
- Persistence/infrastructure:
  - `MoneyTracker.Infrastructure/Persistence`
  - `MoneyTracker.Infrastructure/Persistence/Repositories`
  - `MoneyTracker.Infrastructure/Migrations`

## New CRUD checklist (follow existing patterns)
1. Add/extend Domain entity + repository interface if needed.
2. Implement/extend repository in Infrastructure.
3. Add DTOs in Application.
4. Add explicit mapper methods (`MapToDto`, `MapToEntity`, `UpdateEntity`).
5. Add FluentValidation validator(s).
6. Add service methods returning `OperationResult`/`OperationResult<T>`.
7. Register dependencies/validators in `MoneyTracker.UI/Program.cs`.
8. Add UI page/dialog components using MudBlazor dialog pattern.
9. Add/adjust tests in `MoneyTracker.Tests/Services`.

## Hard constraints
- Never add secrets, tokens, API keys, or personal data.
- Never introduce AutoMapper.
- Never change dialog pattern from:
  - `<MudDialog><TitleContent>...<DialogContent>...<DialogActions>...</MudDialog>`
- Never change architecture boundaries without explicit user approval.
- Never reference `MoneyTracker.Infrastructure` directly from UI pages/components; use `MoneyTracker.Application` interfaces.
- Always add/update unit tests for new or changed service behavior.
- Keep comments minimal and high-signal; avoid obvious comments.
- Keep docs and comments in English.

## UI safety checks (required before UI commit)
- Run `dotnet build MoneyTracker.UI/MoneyTracker.UI.csproj --no-restore` before committing UI changes.
- Treat MudBlazor analyzer warnings (`MUD*`) as actionable; do not ignore invalid attributes/parameters.
- Avoid introducing new nullable warnings in UI code (`CS860*`, `CS862*`, `CS8669`) unless explicitly approved.
- Prefer existing shared helpers for formatting/colors (`CurrencyHelper`, filter helpers, dialog helpers) instead of duplicating view logic.
- Keep Razor component attributes simple: avoid escaped nested quotes inside inline expressions; move complex logic to helper methods/properties.
- If a change is formatting-only or accidental (encoding/comment-only diffs), revert it before commit.
