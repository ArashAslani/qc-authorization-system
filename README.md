# Access Management Core

A CleanArchitecture-based .NET 10 implementation of a product-agnostic
Access Management Core. It models organization hierarchy, position-based
grants, role-based grants, individual overrides, and delegations. A single
`IAccessEvaluator` is the only component that produces Allow/Deny
decisions. Product-specific permissions and scopes are added via
`IAccessPluginSeeder` on a product branch — never in Core.

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
  Core/
    AccessManagement.Domain/         entities, value objects, domain services
    AccessManagement.Application/    use cases, IAccessEvaluator, plugin contract
    AccessManagement.Infrastructure/ EF Core + SQLite + ASP.NET Identity
  Host/WebApi/         ASP.NET Core minimal API (Core only on main)
  Shared/              cross-cutting primitives
  ServiceDefaults/     default observability
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
dotnet run --project src/Host/WebApi
```

Apply database migrations (development auto-runs on startup; manual):

```bash
dotnet ef database update --project src/Core/AccessManagement.Infrastructure --startup-project src/Host/WebApi
```

The default URL is printed by ASP.NET Core; OpenAPI is available at
`/openapi/v1.json`. The SQLite database file is created on first run.

### First-time setup

A fresh database has no UserAdmin, and every organization write path is
gated by `IRequireUserAdmin`. Bootstrap is step zero so the system is
portable to a new deployment:

1. Register an identity user: `POST /api/users/register`.
2. Call `POST /api/organization/bootstrap/admin` **without a JWT**, using
   that user's `identityUserId` plus personnel fields (`nationalId`,
   `firstName`, `lastName`, `personnelCode`).
3. Log in as that user. Subsequent `POST /api/organization/bootstrap/admin`
   calls are rejected: the route disables itself as soon as any
   `Personnel.IsSystemUser` row exists.

### API endpoint groups

| Group | Use cases |
|-------|-----------|
| `OrganizationEndpoints` | Personnel, positions, assignments, re-parent |
| `AuthorizationEndpoints` | Create grant, evaluate access |
| `AccessDefinitionEndpoints` | Permissions, roles, role groups |
| `DelegationEndpoints` | Create/revoke delegation |

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
- **Decision Trace** — explains why a decision was made (per evaluation).
- **Authorization Audit** — records what changed (grant/delegation lifecycle).

## Implementation phases

See [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md) for the
full per-phase plan. Phases 00–12 are **complete**; each phase was gated
with `dotnet build && dotnet test` and a local commit.

## Database

SQLite via EF Core 10. The provider is selected by connection string
(`appsettings.json`); switching to SQL Server or PostgreSQL is a
one-line change in `src/Core/AccessManagement.Infrastructure/DependencyInjection.cs`. Migrations
live under `src/Core/AccessManagement.Infrastructure/Data/Migrations/`:

```bash
dotnet ef migrations add <Name> --project src/Core/AccessManagement.Infrastructure --startup-project src/Host/WebApi
dotnet ef database update --project src/Core/AccessManagement.Infrastructure --startup-project src/Host/WebApi
```

## License

This is a private project. No license granted.
