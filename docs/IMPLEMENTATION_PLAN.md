# Qc Authorization System — Implementation Plan

## 1. Goal

Deliver a CleanArchitecture-based, .NET 10, SQLite-backed Authorization / Access
Management system whose behavior matches the architecture and execution
specifications stored in this repository. Every architectural decision is
captured in `docs/decisions/`.

## 2. Solution structure

```text
D:\Identity\qc-authorization-system
├── qc-authorization.slnx
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── docs/
│   ├── ARCHITECTURE.md
│   ├── IMPLEMENTATION_PLAN.md
│   ├── TESTING.md
│   └── decisions/
│       ├── 0001-grant-as-dumb-data.md
│       ├── 0002-engine-is-sole-allow-deny-owner.md
│       ├── 0003-asymmetric-position-propagation.md
│       ├── 0004-computed-not-materialized-propagation.md
│       ├── 0005-individual-grant-isolation.md
│       ├── 0006-source-aware-priority-model.md
│       ├── 0007-decision-trace-content.md
│       ├── 0008-sqlite-efcore-v1.md
│       └── 0009-typed-constraints-no-dsl.md
├── src/
│   ├── Domain/                 pure C# entities, value objects, domain services
│   ├── Application/            use cases, MediatR, IAccessEvaluator, contracts
│   ├── Infrastructure/         EF Core, persistence, time provider
│   ├── Shared/                 cross-cutting primitives
│   ├── ServiceDefaults/        OpenTelemetry + default service registration
│   └── Web/                    ASP.NET Core minimal API + DI composition
└── tests/
    ├── Domain.UnitTests/                 xUnit/NUnit unit tests for the Domain
    ├── Application.UnitTests/            NUnit unit tests for Application services
    ├── Infrastructure.IntegrationTests/  EF Core + SQLite integration tests
    └── ArchitectureTests/                NetArchTest layering + dependency rules
```

Dependency direction is enforced by `tests/ArchitectureTests/LayeringTests.cs`:

```text
Domain       →  (no references to anything beyond the BCL)
Application  →  Domain
Infrastructure → Application, Domain
Web          → Application, Infrastructure, Domain
```

## 3. Architectural north star

- **Grant = dumb data.** No `CanPropagate()`, `IsDelegationValid()`,
  `CanOverride()`, `EvaluateConstraint()`, or `ResolveConflict()` on `Grant`.
- **Engine = sole owner of Allow/Deny.** No controller, workflow handler, or
  domain object other than the Access Evaluation Engine produces a final
  `AccessDecision`.
- **Computed propagation.** Position Grants propagate to ancestors, Revokes
  propagate to descendants, both computed at evaluation time. The hierarchy
  is read live; we do not materialize derived grant rows.
- **Asymmetric Grant vs. Revoke propagation.** Implemented as separate
  concepts (`ResolveAncestors(P)` and `ResolveDescendants(P)`); not a single
  generic `Propagate(Position, Operation)`.
- **Individual Grant isolation.** A grant with `SubjectType = User` and
  `SourceType = User` is an Individual Grant; it never participates in
  position hierarchy propagation in either direction.
- **Source-aware priority.** Baseline ordering: Individual Override >
  Position Override > Delegation > Role / RoleGroup > Propagated. Equal
  priority: `Deny > Allow`. The order is recorded on each grant and shown
  in the trace.
- **Decision Trace is mandatory.** Every decision returns a trace that
  records subject, requested permission, resource, candidate / applicable /
  rejected grants, scope / validity / conflict results, final decision,
  reason, and a trace id.
- **No DSL, no rule engine.** Constraints are concrete typed classes
  (Amount, Time, Scope). New business rules = new typed classes, not a
  generic expression language.

## 4. Domain model (final, all phases)

```text
Organization
  Personnel
  Position
  PositionAssignment (Personnel ↔ Position with ValidFrom/ValidTo)
  Position.ParentId (self-reference)

Authorization
  Resource           (catalog)
  Action             (catalog)
  Permission         (Resource + Action + Code)
  Role
  RolePermission
  Grant
    Subject (User | Position | Role | RoleGroup)
    SubjectType
    Permission
    Resource / ResourceId
    Scope
    Effect (Allow | Deny)
    SourceType (User | Position | Role | RoleGroup | Delegation)
    SourceId
    ValidFrom / ValidTo
    Priority
  Delegation
    Delegator, Delegate
    Permission
    Scope
    ValidFrom / ValidTo
    Subset (effective access of delegator)
    Delegable (bool)
```

## 5. Application use cases (final, all phases)

- Permission management: create/list permissions.
- Role management: create roles, attach permissions.
- Grant management: create / list / revoke grants.
- Authorization evaluation: evaluate `AccessRequest` → `AccessDecision` + trace.
- Position hierarchy access: get children / ancestors / descendants;
  re-parent with cycle prevention.
- Delegation management: create / list / revoke delegations with subset
  enforcement and chain control.
- Workflow integration: hand a `WorkflowStepRequirement` to the engine and
  get a decision back.

## 6. Persistence model

EF Core 10 with SQLite (file-based, under `qc-authorization.db` in the Web
project). The provider is swap-friendly: switching to SQL Server or
PostgreSQL is a connection-string and a `UseSqlServer` / `UseNpgsql` change.
Migrations live under `src/Infrastructure/Data/Migrations/`.

## 7. API boundaries

ASP.NET Core minimal API with `IEndpointGroup` classes per feature. Domain
entities never appear in API contracts; Application DTOs do. Each endpoint
that requires authorization passes through the engine via a MediatR command
or a direct `IAccessEvaluator` call.

## 8. Testing strategy

- **Domain.UnitTests** — entity invariants, hierarchy traversal, cycle
  detection, propagation rules.
- **Application.UnitTests** — use cases, validators, evaluator pipeline
  (with in-memory fakes for repositories and the time provider).
- **Infrastructure.IntegrationTests** — EF Core + SQLite round-trips for
  grants, roles, and delegations.
- **ArchitectureTests** — NetArchTest rules that fail the build if Domain
  depends on EF Core / ASP.NET / Infrastructure / Web, or if Application
  depends on Infrastructure / Web.

A phase is not complete until:

```text
dotnet restore          PASS
dotnet build            PASS
dotnet test             PASS  (all projects)
```

## 9. Phases and dependencies

| Phase | Title                                | Depends on       | Commit                                                            |
|------:|--------------------------------------|------------------|-------------------------------------------------------------------|
| 01    | Organization Foundation              | —                | `feat(org): implement organization foundation`                    |
| 02    | Access Definitions & Grant           | Phase 01         | `feat(auth): implement access definitions and grant model`        |
| 03    | Minimal Access Evaluation Engine     | Phase 02         | `feat(auth): implement minimal access evaluation engine`           |
| 04    | Asymmetric Position Propagation      | Phase 03         | `feat(auth): implement asymmetric position propagation`            |
| 04b   | Individual Grant Isolation (tests)   | Phase 04         | `test(auth): enforce isolated individual grants`                  |
| 05    | Delegation                           | Phase 04         | `feat(auth): implement delegation`                                |
| 06    | Constraints (Amount / Time / Scope)  | Phase 05         | `feat(auth): implement authorization constraints`                 |
| 07    | Workflow Integration                 | Phase 06         | `feat(auth): integrate authorization with workflow`               |
| 08    | API / Application Layer              | Phase 07         | `feat(api): expose authorization application use cases`           |

Performance & Scale (caching, projection, materialized read models) is
explicitly deferred until a real performance use case exists, per the
architecture spec.

## 10. Per-phase gate (lifecycle)

```text
READ
  ↓
IMPLEMENT
  ↓
TEST
  ↓
SELF-REVIEW
  ↓
ARCHITECTURE CHECK (run tests/ArchitectureTests)
  ↓
BUILD (dotnet build)
  ↓
COMMIT (with the phase commit message)
  ↓
PUSH
  ↓
NEXT PHASE
```

A phase is only complete when its commit is on the public `main` branch and
the build is green.
