# 0006 — Source-aware priority model

- Status: Accepted
- Phase: 03

## Context

A user may simultaneously match a Role Grant, a Position Grant, an
Individual Grant, and a Delegation Grant. The architecture spec defines a
fixed ordering for these sources so the engine can resolve conflicts
deterministically.

## Decision

V1 priority ordering, from highest to lowest:

```text
Individual Override   (priority = 100)
Position Override     (priority = 90)
Delegation            (priority = 70)
Role / RoleGroup      (priority = 50)
Propagated            (priority = 10)
```

`Priority` is a column on `Grant`. Within the same priority, `Deny` wins
over `Allow` so the result is fully deterministic.

## Consequences

- Conflicts are resolved without inspecting trace history.
- The trace can show exactly which source and priority produced the
  winning decision.
- The exact priority numbers are an implementation detail; the relative
  order and the source → priority mapping is the contract.

## Alternatives considered

- Pure `Deny > Allow` everywhere — rejected: the spec explicitly says
  this is not enough when multiple sources exist.
- Per-permission custom priorities — rejected: that is a generic policy
  language, which the spec forbids.
