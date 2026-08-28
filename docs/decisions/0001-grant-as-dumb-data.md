# 0001 — Grant as dumb data

- Status: Accepted
- Phase: 02

## Context

The architecture spec mandates that the `Grant` aggregate is purely data. It
must not contain behavior that decides whether it is valid, propagable, or
overriding.

## Decision

`Grant` is an anemic data carrier. Its members are:

- Id
- Subject
- SubjectType
- Permission
- Resource / ResourceId
- Scope
- Effect (Allow / Deny)
- SourceType (User | Position | Role | RoleGroup | Delegation)
- SourceId
- ValidFrom / ValidTo
- Priority

Grant has no methods that decide, evaluate, or override anything. All
authorization decisions live in the Access Evaluation Engine.

## Consequences

- Easy to reason about: a grant is a fact, not a rule.
- New rules do not require changes to `Grant`.
- All grant-creation paths (Roles, Positions, RoleGroups, Users,
  Delegations) converge on the same `Grant` shape, which keeps the engine
  source-agnostic.

## Alternatives considered

- Putting `CanPropagate()` on `Grant` — rejected: it duplicates engine
  logic and bleeds the propagation rule into the data layer.
- A generic rule object attached to `Grant` — rejected: it is a step
  toward a rule engine, which the spec explicitly forbids.
