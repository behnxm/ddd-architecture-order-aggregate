# 0011. SQLite Default with Committed Migrations

## Context
A production-quality reference implementation should run immediately with zero external dependencies (no Docker container spun up, no cloud database provisioned) upon cloning the repository and running `dotnet run`. However, relying on in-memory database providers (like `Microsoft.EntityFrameworkCore.InMemory`) fails to exercise actual relational schema constraints, foreign keys, concurrency tokens, and SQL expression translations.

## Decision
1. **SQLite Default**: I configured SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) as the default database provider (`Data Source=orders.db`).
2. **Committed Migrations**: Database schemas are managed strictly through EF Core migrations generated and committed using the local `dotnet-ef` tool manifest.
3. **Automatic Startup Migration**: When running in `Development` or when `Database:ApplyMigrationsOnStartup` is enabled, the API automatically executes `Database.Migrate()` on application start.
4. **Isolated Integration Tests**: Integration tests execute against isolated in-memory SQLite connections (`Data Source=:memory:;Foreign Keys=True;`) with enforced foreign key constraints per test class.
5. **Provider Independence**: Switching to enterprise relational engines (PostgreSQL or SQL Server) requires only altering the connection string and changing `UseSqlite` to `UseNpgsql` or `UseSqlServer` in the Infrastructure composition root.

## Consequences
- **Positive**: Immediate developer onboarding: clone repo, run `dotnet run`, and interact immediately via `/scalar` or `.http` files.
- **Positive**: Exact relational constraints, transactions, and foreign keys verified in tests without external container dependencies.
- **Positive**: Schema version control is fully reproducible via committed migrations.
