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
- `ApplicationRole` is intentionally thin — do not extend it with Qc
  permission catalog data. `IdentityRoleClaim` is not used for business
  permissions. See ADR
  [0010 — Identity Role vs Qc Authorization Role separation](decisions/0010-identity-vs-qc-role-separation.md).
- See ADR
  [0011 — Personnel vs System User and RoleGroup assignment](decisions/0011-personnel-user-role-group.md).

### Personnel vs system user

`Personnel` and `ApplicationUser` are **independent** concepts with an
optional link:

| Case | Access path | Multi-company workspace |
|---|---|---|
| Personnel only (no login) | Position grants only when evaluated as position subject | N/A |
| User only (external account) | User grants, Role, RoleGroup | Not available |
| Linked Personnel + User | Union of user grants + position grants in active company | Available |

Rules:

- Register/login may create or use a user **without** `PersonnelId`.
- `LinkPersonnelToIdentityUser` connects records later (bidirectional).
- Workspace APIs (`/me/workspaces`, `/me/switch-company`) require linked Personnel.
- External users are authorized only via `SubjectType.User` materialized grants.

### RoleGroup

`RoleGroup` bundles Roles for admin convenience. It has no direct link to
`Permission`. Assigning a group to a User or Position **materializes** all
permissions from member Roles into `Grant` rows (`SourceType.RoleGroup`).
Revoking the assignment removes those grants. See ADR 0011 and ADR 0012.

### Catalog status and Role→Position

- `Role` and `RoleGroup` expose `CatalogStatus` (`Active` / `Inactive`).
  Inactive sources cannot be assigned; `CatalogGrantFilter` excludes their
  grants at evaluation time.
- Roles may be assigned directly to Positions (`AssignAuthorizationRoleToPosition`),
  materializing `SourceType.Role` grants on the position subject.
- Admin update commands exist for Role, RoleGroup, and Position metadata/status.

## 10. Identifier strategy

All entity and foreign-key identifiers use **`Guid`** — no integer primary keys.

| Area | Identifier |
|---|---|
| Aggregates (`Personnel`, `Position`, `Grant`, `Role`, …) | `Guid Id` via `BaseEntity` |
| Company reference on `Position` | `Guid CompanyId` (no `Company` aggregate in this bounded context) |
| Identity users | `Guid` (`ApplicationUser` / `IdentityUser<Guid>`) |
| JWT workspace claim | `active_company_id` as Guid string |
| User-scoped grants | `SubjectUserId` / `SourceUserId` (`Guid?`) |

`Priority` remains `int` (ordering, not identity). Enum underlying values are unchanged.

## 11. Company Workspace Context

One person (`Personnel`) may hold multiple active `PositionAssignment` rows
across multiple companies. The system never unions position grants across
companies in a single evaluation.

### Active company

- JWT carries `active_company_id` (and optionally `national_id`).
- `ICurrentUser.ActiveCompanyId` exposes the claim to application code.
- `PositionAwareCandidateGrantResolver` loads position IDs only for
  assignments whose `Position.CompanyId` matches the active company.
- Within the active company, grants from **all** active positions are
  unioned — no engine algorithm change is required.
- If a user subject has no active company, position-based grants are
  excluded; direct user grants may still apply.
- Admin simulate/evaluate endpoints may override via
  `AccessRequest.Context["CompanyId"]`.

### Login and switch

- Login accepts national ID (`کد ملی`) or email.
- Default `active_company_id` comes from the assignment marked
  `IsPrimary`; if none is set, the first company by sorted `CompanyId`
  with active assignments is used (deterministic fallback).
- `POST /api/users/me/switch-company` validates membership and reissues JWT.
- `GET /api/users/me/workspaces` lists companies and positions per company.

### Primary assignment

- `PositionAssignment.IsPrimary` marks the default company at login.
- `SetPrimaryPositionAssignmentCommand` clears other primaries for the same
  personnel and sets one — at most one primary per person (application rule).

## 12. What is explicitly NOT built (yet)

- Generic Rule Engine
- Authorization DSL / Generic Policy Language
- Materialized Propagation
- Distributed Authorization
- Complex Constraint Engine
- Universal Expression Parser
- Speculative infrastructure (event bus, microservices, etc.)

Each of these is added only when a real use case requires it.
