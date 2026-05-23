# ADR 0003: Explicit Mapping Pattern

- Status: Accepted (observed in current repository).
- Date: 2026-03-05.

## Context
- Domain entities and DTOs have non-trivial differences (signed amounts, enum/string icon conversions, timezone conversion).
- Mapping must stay visible and deterministic.

## Decision
- Keep explicit mapper extensions (`MapToDto`, `MapToEntity`, `UpdateEntity`) in Application layer.
- Do not introduce AutoMapper under current conventions.

## Evidence
- `MoneyTracker.Application/Mappers/AccountMapper.cs`
- `MoneyTracker.Application/Mappers/CategoryMapper.cs`
- `MoneyTracker.Application/Mappers/BudgetMapper.cs`
- `MoneyTracker.Application/Mappers/Transactions/TransactionMapper.cs`
- `MoneyTracker.Application/Mappers/Transactions/TransferMapper.cs`
- `rg` search: no `AutoMapper` references in solution code.

## Consequences
- Mapping logic stays reviewable and easy to debug.
- Timezone conversion behavior remains explicit in transaction mapping.
- New DTO/entity changes require explicit mapper updates.
