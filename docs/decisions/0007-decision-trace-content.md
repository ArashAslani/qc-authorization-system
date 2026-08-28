# 0007 — Decision Trace content

- Status: Accepted
- Phase: 03

## Context

The architecture spec requires that every `AccessDecision` carries enough
information for an operator to answer "why did this request receive ALLOW
or DENY?".

## Decision

Every `AccessDecision` includes a `Trace` object with the following
fields:

- `Subject`
- `RequestedPermission`
- `Resource`
- `ResourceId`
- `CandidateGrants`
- `ApplicableGrants`
- `RejectedGrants`
- `SourceType` and `SourceId` for each grant
- `Priority` for each grant
- `ScopeResult`
- `ValidityResult`
- `ConflictResolution`
- `FinalDecision`
- `Reason`
- `TraceId` (a stable identifier)

The trace is returned synchronously with the decision. Persistence of the
trace is not part of V1; audit (i.e. "what changed") is a separate concept
from the trace (i.e. "why this decision") and is also out of V1 scope.

## Consequences

- Operators can debug a denied request by reading the trace.
- Tests can assert on the trace contents, not just the boolean outcome.
- The trace is intentionally not a generic logging platform. It exists
  to explain decisions, nothing more.

## Alternatives considered

- Returning only the final `Effect` — rejected: violates the spec.
- A separate audit pipeline writing to disk on every decision — rejected:
  the spec separates audit and trace, and out-of-scope for V1.
