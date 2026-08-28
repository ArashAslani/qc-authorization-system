# 0005 — Individual Grant isolation

- Status: Accepted
- Phase: 04

## Context

A grant with `SubjectType = User` and `SourceType = User` is an
"Individual Grant". It is bound to the user, not to a position. The
architecture spec says it must not propagate to ancestors or descendants,
and it must not move when the user changes position.

## Decision

The engine treats Individual Grants as a completely separate authorization
path:

- No `ResolveAncestors` is applied to Individual Grants.
- No `ResolveDescendants` is applied to Individual Grants.
- Re-assigning the user to a different position does not move the
  Individual Grant to the new position.

The `Subject` of an Individual Grant is the user's id; the `Source` is
the same user's id. There is no position in the picture.

## Consequences

- Position-based and Individual Grants are independent paths. An
  Individual Grant can be Allow while a Position Grant is Deny, or vice
  versa. The priority model resolves the conflict.
- Removing a user from a position does not silently revoke the user's
  Individual Grants. Revoke is an explicit operation that targets the
  Individual Grant's `SourceId`.

## Alternatives considered

- Storing Individual Grants as a special case on `PositionAssignment` —
  rejected: the architecture spec treats Individual Grants as a fully
  independent concept.
- "Translating" Individual Grants to Position Grants when the user
  changes position — rejected: that would be a silent data movement,
  which the spec forbids.
