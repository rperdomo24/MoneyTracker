# ADR 0001: Clean Architecture Structure

- Status: Accepted (observed in current repository).
- Date: 2026-03-05.

## Context
- Solution contains separate projects for UI, Application, Domain, Infrastructure, and Tests.
- Project references enforce directional dependencies from outer layers to inner abstractions.

## Decision
- Keep the current layered structure:
  - `MoneyTracker.UI` as composition root and presentation layer.
  - `MoneyTracker.Application` for services/use-cases/DTOs/validators/mappers.
  - `MoneyTracker.Domain` for entities/enums/constants/repository interfaces.
  - `MoneyTracker.Infrastructure` for EF Core/PostgreSQL persistence and infra services.

## Evidence
- `MoneyTracker.sln`
- `MoneyTracker.UI/MoneyTracker.UI.csproj`
- `MoneyTracker.Application/MoneyTracker.Application.csproj`
- `MoneyTracker.Infrastructure/MoneyTracker.Infrastructure.csproj`
- `MoneyTracker.Domain/MoneyTracker.Domain.csproj`

## Consequences
- Business logic remains testable at service level (see `MoneyTracker.Tests`).
- Infrastructure can evolve behind repository interfaces.
- Composition is centralized in `MoneyTracker.UI/Program.cs`.
