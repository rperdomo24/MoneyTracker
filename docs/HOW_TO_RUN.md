# How To Run

## Prerequisites
- .NET target framework: `net9.0` (from all `*.csproj` files).
- Local SDK used in this environment: `10.0.200-preview.0.26103.119` (`dotnet --version`).
- PostgreSQL is required (`UseNpgsql` in `MoneyTracker.UI/Program.cs` and Npgsql package in `MoneyTracker.Infrastructure.csproj`).
- Docker required only for container workflow (`Dockerfile`, `docker-compose.yml`).

## Local run (CLI)
1. Restore:
   - `dotnet restore MoneyTracker.sln`
2. Run UI project:
   - `dotnet run --project MoneyTracker.UI`
3. Default dev URLs (launch profile):
   - `http://localhost:5229`
   - `https://localhost:7238`

## Local configuration notes
- Connection string is read from:
  - `MoneyTracker.UI/appsettings.json` -> `ConnectionStrings:DefaultConnection`
- Timezone/date formats are read from:
  - `MoneyTracker.UI/appsettings.json` -> `ApplicationSettings`
- UI user key comes from:
  - `MoneyTracker.UI/appsettings.json` -> `UISettings`

## Docker run
1. Build and start:
   - `docker compose up --build -d`
2. Stop:
   - `docker compose down`
3. Notes from compose file:
   - App publishes port `8091`.
   - Expects host-mounted files/directories under `C:/MoneyTracker/...`.
   - Uses an external docker network: `jobordermanager_default`.

## Migrations
- EF Core migrations exist in `MoneyTracker.Infrastructure/Migrations`.
- Exact repo-defined migration command: `Unknown`.
- Files to check/standardize:
  - `MoneyTracker.Infrastructure/Migrations/*`
  - `MoneyTracker.UI/MoneyTracker.UI.csproj` (`Microsoft.EntityFrameworkCore.Design`)

## Tests
- Test project exists: `MoneyTracker.Tests`.
- Run tests:
  - `dotnet test MoneyTracker.Tests`
- Verified result in this environment:
  - `52 passed, 0 failed`.
