# Project Overview

## What it is

**MoneyTracker** — personal finance tracker. Multi-tenant, invite-only beta. Single web application; no public API, no mobile client.

## Core domains

| Domain | Responsibility |
|---|---|
| **Accounts** | Balance tracking, manual adjustments, reconciliation |
| **Categories** | Hierarchical (parent/child), system categories seeded per tenant |
| **Transactions** | CRUD, timezone-aware date filters, bulk delete, advanced filtering |
| **Transfers** | Paired transaction management (debit + credit as one transfer) |
| **Budgets** | Monthly targets, actual spend tracking, usage forecasting |
| **Dashboard** | Net-worth trend, balance distribution, spending summary (ApexCharts) |
| **Auth** | OTP login, email verification, invite-only onboarding, session audit |
| **Tenancy** | Column-based isolation (`TenantId`) enforced via EF Core global query filters |

## Technology stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9 / ASP.NET Core 9 |
| UI rendering | Blazor Server |
| UI components | MudBlazor |
| Data visualizations | ApexCharts |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 9 |
| Identity | ASP.NET Core Identity |
| Validation | FluentValidation 12 |
| Logging | Serilog (console + file) |
| Container | Docker + Docker Compose |

## Projects in the solution

| Project | Role |
|---|---|
| `MoneyTracker.UI` | Blazor Server host — pages, components, auth endpoints, DI root, config |
| `MoneyTracker.Application` | Use-cases — services, DTOs, mappers, validators, OperationResult |
| `MoneyTracker.Domain` | Core model — entities, enums, repository interfaces (no external deps) |
| `MoneyTracker.Infrastructure` | Persistence — EF Core DbContext, repositories, migrations, TimeZoneService |
| `MoneyTracker.Tests` | Unit tests — xUnit + Moq, covers service layer |

## Key entry points

- **`MoneyTracker.UI/Program.cs`** — DI composition, middleware, Identity options, Serilog, MudBlazor registration
- **`MoneyTracker.UI/Endpoints/AuthEndpoints.cs`** — login / OTP / password-reset / invite postback handlers
- **`MoneyTracker.UI/Components/Pages/`** — one subfolder per feature domain
- **`MoneyTracker.Infrastructure/Persistence/MoneyTrackerDbContext.cs`** — EF Core context + tenant query filters

## Configuration files

| File | Purpose |
|---|---|
| `MoneyTracker.UI/appsettings.json` | DB connection, SMTP, logging levels, `PublicBaseUrl`, date formats |
| `MoneyTracker.UI/appsettings.Development.json` | Dev overrides |
| `.env` / `.env.example` | Docker: DB credentials, timezone, SMTP, `ASPNETCORE_ENVIRONMENT` |
| `docker-compose.yml` | Web service (port 8091) + optional internal PostgreSQL + external network |

## Related docs

- [`/docs/PROJECT_MAP.md`](PROJECT_MAP.md) — entity relationships and data-flow paths
- [`/docs/CONVENTIONS.md`](CONVENTIONS.md) — all coding and naming conventions
- [`/docs/HOW_TO_RUN.md`](HOW_TO_RUN.md) — local and Docker setup walkthrough
- [`/docs/architecture-rules.md`](architecture-rules.md) — enforceable boundary and pattern rules
