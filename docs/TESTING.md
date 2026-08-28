# Qc Authorization — Testing

## Principles

Tests are first-class deliverables. Critical business rules must have tests
in the test matrix defined by the architecture spec.

## Test projects

| Project | Purpose | What lives here |
|---|---|---|
| `tests/Domain.UnitTests` | Pure C# entity / value-object / domain-service tests | Hierarchy traversal, cycle detection, propagation rules, individual isolation |
| `tests/Application.UnitTests` | Application service / use-case tests | Evaluator pipeline, propagation, delegation, workflow |
| `tests/Infrastructure.IntegrationTests` | EF Core round-trip tests | Persistence, migrations, audit, API queries |
| `tests/ArchitectureTests` | NetArchTest layering rules | Dependency direction; no EF Core in Domain; no Infrastructure in Web |

## Build / Test gate

A phase is not complete until:

```text
dotnet restore   PASS
dotnet build     PASS
dotnet test      PASS
```

In particular `ArchitectureTests` must pass.

## Phase test matrices

See `IMPLEMENTATION_PLAN.md` for the per-phase matrix. The minimum per phase
is the matrix defined in the spec; tests are added at the same time as the
implementation that satisfies them, never after.

## Conventions

- NUnit (`[TestFixture]`, `[Test]`) for all test projects.
- `Shouldly` for assertions.
- `Moq` for fakes when in-memory fakes are too cumbersome.
- Tests do not weaken or delete the spec matrix merely to make
  implementation pass.
