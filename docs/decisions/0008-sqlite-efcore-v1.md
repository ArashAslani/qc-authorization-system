# 0008 — SQLite + EF Core 10 in V1

- Status: Accepted
- Phase: 00

## Context

The architecture spec does not pin a database provider, but it does
require EF Core and discourages speculative infrastructure. The execution
protocol says "use the persistence approach from the current
CleanArchitecture template unless the project specification requires
otherwise". The current template defaults to SQLite for `--database
sqlite`.

## Decision

V1 uses SQLite via EF Core 10, with the database file living next to the
Web project. The provider is selected by connection string; switching to
SQL Server or PostgreSQL is a one-line change in
`Infrastructure/DependencyInjection.cs` and a connection-string update.

Integration tests use the same provider with a per-test database file.

## Consequences

- Zero-setup dev experience.
- No external services required to run the test suite.
- Migrations work the same way they would for any relational provider.
- The persistence model is intentionally provider-neutral.

## Alternatives considered

- SQL Server / LocalDB — rejected for V1 because it forces a Windows
  dependency on every contributor.
- PostgreSQL in Docker — rejected for V1 because it requires Docker to
  be installed.
- In-memory only — rejected because migrations and provider-specific
  SQL need to be exercised in tests.
