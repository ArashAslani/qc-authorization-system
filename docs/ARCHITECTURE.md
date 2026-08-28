# Qc Authorization — Architecture

This document is a short, working summary of the architectural decisions.
The full source of truth is the spec document
`Qc_Authorization_Architecture_Final.md` and the per-decision ADRs in
`docs/decisions/`.

## 1. Core invariant

Two principles govern the whole system:

> **Grant = dumb data.**
>
> **Access Evaluation Engine = sole owner of Allow/Deny.**

`Grant` only stores facts: subject, permission, resource, scope, effect,
source, validity, priority. It does not decide anything.

Every final `ALLOW` / `DENY` decision is produced by the Access Evaluation
Engine. No controller, service, workflow handler, or domain object other
than the engine is allowed to produce a final `AccessDecision`.

## 2. Layered structure

```text
Domain
  ↑ Application
  ↑ Infrastructure
  ↑ Web/API
```

- `Domain` has no references to `Microsoft.EntityFrameworkCore.*`,
  `Microsoft.AspNetCore.*`, `Infrastructure`, or `Web`.
- `Application` has no references to `Infrastructure` or `Web`. Handlers use
  `IApplicationDbContext` (EF Core `DbSet` + `SaveChangesAsync`) directly —
  no Repository or Unit of Work abstractions.
- `Infrastructure` references `Application` and `Domain`; it owns EF Core,
  the time provider, and external integrations.
- `Web` composes the DI graph and exposes endpoints. It contains no
  authorization business logic — every authorization decision is delegated
  to the engine.

These rules are enforced by `tests/ArchitectureTests/LayeringTests.cs`.

## 3. Evaluation pipeline

```text
AccessRequest
  ↓
Find Candidate Grants
  ↓
Check Validity
  ↓
Check Scope
  ↓
Check Constraints (Phase 06+)
  ↓
Resolve Priority
  ↓
Resolve Effect
  ↓
Decision
  ↓
Trace
```

Determinism is required: same inputs ⇒ same outputs, including the trace.

## 4. Propagation rules

Position grants and position revokes are **asymmetric**:

- **Grant(P)** ⇒ effective on `P + Ancestors(P)`.
- **Revoke(P)** ⇒ effective on `P + Descendants(P)`.

These are two independent business rules. They are implemented as two
separate, named concepts (`ResolveAncestors(P)` and `ResolveDescendants(P)`)
and not as a generic `Propagate(Position, Operation)`.

Individual Grants (`SubjectType = User && SourceType = User`) do not
participate in position propagation in either direction. They are bound to
the user, not to a position.

Propagation is **computed** at evaluation time, never materialized into
source-of-truth grant rows. Hierarchy changes immediately affect
evaluation results.

## 5. Priority

```text
Individual Override
    > Position Override
    > Delegation
    > Role / RoleGroup
    > Propagated
```

Within the same priority, `Deny > Allow` to keep the result deterministic.

## 6. Decision Trace

A trace is attached to every `AccessDecision`. It records:

- Subject
- Requested Permission
- Resource, ResourceId
- Candidate Grants
- Applicable Grants
- Rejected Grants
- SourceType / SourceId
- Priority
- Scope result
- Validity result
- Conflict resolution
- Final Decision
- Reason
- Trace identifier

The trace is a first-class part of the engine contract.

## 7. Constraints

Phase 07 introduces three typed constraints — `AmountConstraint`,
`TimeConstraint`, `ScopeConstraint` — each deterministic, testable, and
traceable. No generic DSL, no expression parser. Adding a new constraint
type means adding a new class, not changing the engine.

## 8. Workflow integration

Workflows do not own authorization. They declare a required permission plus
a context; the engine evaluates it. The integration is the
`WorkflowStepAuthorizer` in `Application/Workflow`.

## 9. Authentication vs Authorization

- **ASP.NET Core Identity** (`ApplicationUser` / `ApplicationRole`, Guid keys)
  handles authentication — who is logged in.
- **Qc Authorization** (`Grant`, `AccessEvaluationEngine`) handles business
  authorization — what they may do.
- `Personnel` is a business-domain concept; optional `IdentityUserId` links
  a person to an authenticated account.
- **Identity Role** ≠ **Qc Authorization Role** (`Domain.Authorization.Role`).
  Role-based access flows through `RolePermission` → materialized `Grant` facts
  → Evaluation Engine. `[Authorize(Roles=...)]` does not replace the engine.

## 10. What is explicitly NOT built (yet)

- Generic Rule Engine
- Authorization DSL / Generic Policy Language
- Materialized Propagation
- Distributed Authorization
- Complex Constraint Engine
- Universal Expression Parser
- Speculative infrastructure (event bus, microservices, etc.)

Each of these is added only when a real use case requires it.
