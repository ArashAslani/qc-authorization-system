# Qc Authorization System

A CleanArchitecture-based .NET 10 implementation of the Qc Authorization
and Access Management system. It models organization hierarchy, position-based
grants, role-based grants, individual overrides, and delegations. A single
Access Evaluation Engine is the only component that produces Allow/Deny
decisions.

## Architecture

The two governing principles are:

> **Grant = dumb data.**
>
> **Access Evaluation Engine = sole owner of Allow/Deny.**

The full architectural specification lives in
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) and the per-decision
rationale is in [`docs/decisions/`](docs/decisions/).

## Repository structure

```text
docs/                  architecture, plan, testing, ADRs
src/
  Domain/              pure C# entities, value objects, domain services
  Application/         use cases, IAccessEvaluator, contracts
  Infrastructure/      EF Core + SQLite persistence
  Shared/              cross-cutting primitives
  ServiceDefaults/     default observability
  Web/                 ASP.NET Core minimal API
tests/
  Domain.UnitTests/                 NUnit unit tests for Domain
  Application.UnitTests/            NUnit unit tests for Application
  Infrastructure.IntegrationTests/  EF Core + SQLite integration tests
  ArchitectureTests/                NetArchTest layering rules
```

## Prerequisites

- .NET 10 SDK (`10.0.101` or any 10.0.x with `latestFeature` roll-forward).
- No external database. SQLite ships with the runtime; the file is created
  on first run.

## How to build and test

```bash
dotnet restore
dotnet build
dotnet test
```

`dotnet test` runs the unit, integration, and architecture test projects.
The architecture test project is the dependency-direction gate described
in `docs/ARCHITECTURE.md`.

## How to run the API

```bash
dotnet run --project src/Web
```

The default URL is printed by ASP.NET Core; OpenAPI is available at
`/openapi/v1.json`. The database file is created next to the Web project
on first run.

## Authorization concepts

- **Personnel / Position / PositionAssignment** — the organization
  foundation, with hierarchy traversal and cycle detection.
- **Permission** — a `(Resource, Action)` tuple, e.g. `PERSONNEL.UPDATE`.
- **Role** — a named bag of permissions, e.g. `HR_MANAGER`.
- **Grant** — a fact ("this subject has this permission, in this scope,
  with this effect, from this source"). It does not decide anything.
- **Delegation** — produces a Grant with `SourceType = Delegation`,
  enforcing `Delegated ⊆ Effective Access of Delegator`.
- **Access Evaluation Engine** — the only place that returns
  Allow / Deny.
- **Decision Trace** — explains why a decision was made.

## Implementation phases

See [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md) for the
full per-phase plan, including the mandatory test matrix per phase and
the per-phase commit message. Every phase ends with a build / test
gate, a commit, and a push to `main`.

## Database

SQLite via EF Core 10. The provider is selected by connection string
(`appsettings.json`); switching to SQL Server or PostgreSQL is a
one-line change in `src/Infrastructure/DependencyInjection.cs`. Migrations
live under `src/Infrastructure/Data/Migrations/` and are created with
`dotnet ef migrations add ...`.

## License

This is a private project. No license granted.
