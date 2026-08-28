# 0004 — Computed, not materialized propagation

- Status: Accepted
- Phase: 04

## Context

The architecture spec explicitly forbids storing computed propagation as
physical grant rows. Hierarchy changes must immediately affect evaluation
results, so propagation cannot be a snapshot in a table.

## Decision

Position hierarchy lives only in the `Position` table (via `ParentId`).
The engine reads the live hierarchy on every evaluation and computes
ancestors / descendants. There is no `GrantAncestor` / `GrantDescendant`
table.

## Consequences

- Re-parenting a position immediately changes who can do what.
- No need to invalidate or refresh materialized grant rows.
- Storage stays minimal: one `Grant` per (subject, permission, scope,
  source) tuple, no duplicates.
- Slightly more work per evaluation; acceptable for V1. Caching is a
  Phase-08-style optimization that can be added later without changing
  the model.

## Alternatives considered

- A `GrantProjection` table maintained by triggers — rejected: a
  synchronization bug would silently change authorization behavior.
- Snapshotting grants per hierarchy change — rejected: violates the
  spec and makes audit harder.
