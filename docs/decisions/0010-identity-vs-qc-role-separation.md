# 0010 — Identity Role vs Qc Authorization Role separation

- Status: Accepted
- Phase: Identity integration audit

## Context

After integrating ASP.NET Core Identity (`ApplicationUser` / `ApplicationRole`,
Guid keys), we audited whether Identity's built-in role model could absorb
Qc's `Domain.Authorization.Role` + `RolePermission` catalog without losing
authorization semantics.

The audit evaluated three approaches:

| Path | Description |
|------|-------------|
| **A — Bridge** | Keep both models; optional `IdentityRoleQcRoleMap` for admin UX sync |
| **B — Extend** | Extend `ApplicationRole` (Code, Description) and subclass `IdentityRoleClaim` with `PermissionId`; delete `Domain.Authorization.Role` / `RolePermission` |
| **C — Separate (current)** | Thin `ApplicationRole` for auth membership only; Qc `Role` + `RolePermission` in Domain; materialize to `Grant` at assignment time |

Sixteen criteria were evaluated: role identity, membership, role→permission,
permission identification, resource, action, scope, effect, validity,
priority, source type, source id, delegation, position propagation, revoke,
and decision trace.

## Decision

**Adopt Path C — keep Identity and Qc authorization roles separate.**

- `ApplicationRole` remains a thin `IdentityRole<Guid>` for authentication /
  app membership only. Do **not** extend it with Qc-specific properties.
- `Domain.Authorization.Role` + `RolePermission` remain the business
  authorization role catalog (definition-time data).
- `IdentityRoleClaim` is **not** used for Qc permissions. Default
  `ClaimType`/`ClaimValue` strings lack FK integrity to the `Permission`
  catalog and cannot carry scope, effect, priority, or source traceability.
- Role-based business access flows:

```text
RolePermission (catalog)
  → AssignAuthorizationRoleToUser (materialization)
  → Grant rows (SourceType.Role, SourceId=RoleId)
  → AccessEvaluationEngine
```

- `[Authorize(Roles=...)]` is for app-layer access only. It does **not**
  replace the evaluation engine for business permissions.

Path B (extend `ApplicationRole` + custom `ApplicationRoleClaim`) is
**technically feasible** but deferred. It would merge auth and authZ
concerns, require `Grant` schema changes (`SourceRoleId Guid?`), move the
role catalog from Domain to Infrastructure, and increase the risk of
bypassing the engine. Pursue Path B only if a product requirement mandates
a single role table.

## Audit findings (16 criteria)

| # | Criterion | Default IdentityRoleClaim | Qc Role + RolePermission |
|---|-----------|---------------------------|--------------------------|
| 1 | Role identity | Guid, Name only | int Id, Code, Name, Description |
| 2 | Role membership | IdentityUserRole (auth) | AssignAuthorizationRoleToUser → Grants |
| 3 | Role → Permission | String claim | FK to Permission |
| 4 | Permission ID | No FK | Permission.Id + Code |
| 5–6 | Resource / Action | Encode in string | Permission catalog |
| 7–8 | Scope / Effect | N/A at catalog layer | On Grant at materialization |
| 9 | Validity | No per-assignment window | ValidFrom/ValidTo on materialized Grant |
| 10 | Priority | None | SourcePriority.RoleOrRoleGroup on Grant |
| 11–12 | SourceType / SourceId | None | SourceType.Role + SourceId on Grant |
| 13–14 | Delegation / Position | Not in Identity | Qc Domain |
| 15 | Revoke | Claim removal ≠ Grant removal | RevokeAuthorizationRoleFromUser |
| 16 | Decision trace | Claims not in engine | Full trace from Grant candidates |

**Verdict:** Default `IdentityRole` + `IdentityRoleClaim` cannot fully
replace Qc `Role` + `RolePermission` without a materialization layer and
without losing RoleGroup, FK integrity, and revoke/trace semantics.

## What Identity owns

```text
ApplicationUser, ApplicationRole (thin)
IdentityUserRole, IdentityUserClaim, IdentityUserLogin, IdentityUserToken
Authentication (password, lockout, tokens)
```

## What Qc Authorization owns

```text
Permission, ResourceCatalog, ActionCatalog
Domain.Authorization.Role, RolePermission, RoleGroup
Grant (scope, effect, validity, priority, source)
Delegation, Position propagation
AccessEvaluationEngine, DecisionTrace
```

## Consequences

- No duplicate authorization systems: Identity handles authentication;
  the engine handles all business Allow/Deny.
- `RolePermission` is retained as definition-time catalog data.
- `AssignAuthorizationRoleToUser` remains the bridge from role catalog to
  runtime `Grant` facts.
- Future admin UX sync (Path A) may add `IdentityRoleQcRoleMap` without
  merging the models.
- Auth-layer gaps (login endpoints, JWT/cookie scheme, `personnel_id`
  claim issuance) are separate follow-up work and do not change this
  decision.

## Alternatives considered

- **Path B — Extend ApplicationRole + ApplicationRoleClaim with
  PermissionId:** Covers catalog semantics with FK but merges auth/authZ,
  moves role catalog to Infrastructure, requires Grant Guid migration,
  and weakens DDD layering. Deferred.
- **Path A — Bridge map table:** Lowest risk for admin UX sync; does not
  replace RolePermission. Available as future option.
- **Replace Qc Role with default IdentityRoleClaim strings:** Rejected —
  no FK, no RoleGroup, no revoke/trace integrity.
