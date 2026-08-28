# 0009 — Typed constraints, no DSL

- Status: Accepted
- Phase: 06

## Context

The architecture spec explicitly forbids a generic Constraint DSL or a
universal expression parser. It does allow typed constraints when real
project use cases need them. Three candidates are listed: Amount, Time,
Scope.

## Decision

Phase 06 implements exactly three constraint types, each as a concrete
class:

- `AmountConstraint` — numeric ceiling (e.g. approval amount).
- `TimeConstraint` — windows of validity (e.g. business hours).
- `ScopeConstraint` — data-scope matching (e.g. company, branch).

Each constraint implements a small `IAuthorizationConstraint` interface.
Adding a new constraint type is a new class, not a change to the engine.

## Consequences

- Constraints are easy to test in isolation.
- The engine remains a simple pipeline.
- The trace can show exactly which constraint passed and which failed.
- The constraint vocabulary is finite and documented.

## Alternatives considered

- A generic `Expression<Func<...>>` constraint — rejected: it is a step
  toward an expression language, which the spec forbids.
- A textual DSL with a parser — rejected: same reason.
- Implicit constraints baked into the engine — rejected: it would
  couple the engine to specific business concepts.
