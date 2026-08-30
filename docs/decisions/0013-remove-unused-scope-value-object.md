# 0013 — Remove unused Scope value object

- Status: Accepted
- Phase: Remediation

## Context

`AccessManagement.Domain.Authorization.ValueObjects.Scope` (with
`ScopeKind.Unbounded | Company | Branch | Custom`) was an unused
abstraction. It was never connected to Grant write paths, evaluation,
or queries. The only remaining consumer was `ScopeTests`.

The live scope model is already `Grant.ScopeUnitId` pointing at an
`OrganizationalUnit`, with subtree matching performed by
`ScopeMatcher` during evaluation.

Keeping two parallel models for the same concept invited accidental
reintroduction of the unused value object and confused contributors.

## Decision

Delete `Scope` / `ScopeKind`. Do not recreate them. Official scope is:

- `Grant.ScopeUnitId` (null = unrestricted)
- `OrganizationalUnit` hierarchy
- `IScopeMatcher` / `ScopeMatcher` at evaluation time

## Consequences

- One scope model. Evaluation remains the sole Allow/Deny owner.
- Grant stays dumb data: it stores `ScopeUnitId`, it does not interpret it.

## Alternatives considered

Keep the value object as documentation of a possible future custom
scope kind. Rejected: unused types rot, and custom scope is already
expressible as an `OrganizationalUnit` node plus `ScopeUnitId`.
