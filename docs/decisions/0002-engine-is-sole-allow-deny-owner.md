# 0002 — Engine is the sole owner of Allow/Deny

- Status: Accepted
- Phase: 03

## Context

The architecture spec requires that only the Access Evaluation Engine
produces a final Allow/Deny decision. Controllers, services, workflow
handlers, and domain objects other than the engine must not.

## Decision

A single interface `IAccessEvaluator` in `Application/Authorization`
exposes the only method that returns an `AccessDecision`:

```csharp
public interface IAccessEvaluator
{
    Task<AccessDecision> EvaluateAsync(AccessRequest request, CancellationToken ct);
}
```

`AccessDecision` is the only type in the system that carries a final
`Effect` (`Allow` or `Deny`). No other type may return one.

## Consequences

- All paths that need a final decision go through one component. Easier to
  test, easier to audit, easier to reason about.
- New "decision-like" features (workflow gating, API authorization,
  background-job authorization) all reuse the same component.
- Decision Trace is naturally centralized.

## Alternatives considered

- A `IAuthorizationService` per use case (e.g. one for API, one for
  workflows) — rejected: that violates the "single owner" rule.
- Returning Allow/Deny from a query method on `Grant` — rejected: the spec
  forbids decision logic on `Grant`.
