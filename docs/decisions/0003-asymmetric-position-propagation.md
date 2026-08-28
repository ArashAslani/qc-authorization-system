# 0003 — Asymmetric Position propagation

- Status: Accepted
- Phase: 04

## Context

The architecture spec defines two independent business rules:

- **Grant(P)** ⇒ effective on `P + Ancestors(P)`.
- **Revoke(P)** ⇒ effective on `P + Descendants(P)`.

Revoke is not the inverse of Grant. Treating them as a single symmetric
operation would be a behavior bug.

## Decision

Two separate, explicitly named concepts in the evaluation engine:

- `ResolveAncestors(P)` — used by the Grant direction.
- `ResolveDescendants(P)` — used by the Revoke direction.

There is no generic `Propagate(Position, Operation)` helper.

## Consequences

- The asymmetry is visible in the code, the tests, and the trace.
- A change in one direction cannot silently affect the other.
- Future readers cannot accidentally "fix" the asymmetry by unifying
  the two paths.

## Alternatives considered

- `Propagate(Position, Operation)` with an `Operation` enum — rejected:
  it makes the asymmetry easy to lose track of.
- A single `ResolveRelatedPositions(P)` that returns both ancestors and
  descendants — rejected: it weakens the rule that grant and revoke are
  two independent operations.
