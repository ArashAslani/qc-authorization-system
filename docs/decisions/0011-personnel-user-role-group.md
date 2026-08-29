# 0011 — Personnel vs System User and RoleGroup assignment

- Status: Accepted
- Phase: Business alignment

## Context

Qc distinguishes **Personnel** (organization domain) from **ApplicationUser**
(authentication). Business rules require:

- Personnel may exist without a system account.
- System users may exist without a Personnel record (e.g. external consultants).
- A Personnel record may be linked to a system user later.
- **RoleGroup** bundles multiple Qc Roles so administrators assign a group
  instead of selecting roles one by one.

RoleGroup is a **catalog grouping** of Roles, not a separate evaluation engine.
Assignment materializes permissions into `Grant` rows (Grant-as-dumb-data).

## Decision

### Personnel vs User

| Case | Personnel | ApplicationUser | Position grants | User grants | Workspace |
|------|-----------|-----------------|-----------------|-------------|-----------|
| Personnel only | Yes | No | Via assignment when evaluated as position subject | No | N/A |
| User only (external) | No | Yes | No | Yes | No multi-company panel |
| Linked | Yes | Yes | Yes (active company) | Yes | Yes |
| Neither | — | — | — | — | — |

- `Personnel.IdentityUserId` and `ApplicationUser.PersonnelId` are optional,
  bidirectional links enforced by `PersonnelIdentityBridge`.
- Multi-company workspace (`active_company_id`, workspaces API) applies only
  when the authenticated user is linked to Personnel with position assignments.

### RoleGroup assignment

- **AssignRoleGroupToUser** expands `RoleGroup` → member `Role`s →
  `RolePermission`s → `Grant` rows with `SourceType.RoleGroup`,
  `SourceId = roleGroupId`, `SubjectType.User`.
- **AssignRoleGroupToPosition** uses the same expansion with
  `SubjectType.Position`.
- **Revoke** removes all grants matching `(subject, SourceType.RoleGroup, SourceId)`.
- Re-assign is idempotent: existing grants for the same assignment are removed
  before materialization.
- Changing Role membership inside a RoleGroup does **not** auto-sync existing
  assignments; administrators must re-assign or revoke/re-assign.

### Evaluation

`GrantApplicabilityService` treats user grants with `SourceType.RoleGroup` the
same as `SourceType.Role` for `SubjectType.User` requests.

## Consequences

- External users receive authorization only through direct user grants,
  role assignment, or role-group assignment — never through position propagation.
- Admin APIs expose role-group assign/revoke for users and positions.
- `GET /api/users/me/workspaces` returns 403 when the user has no linked Personnel.

## Alternatives considered

- **Evaluate RoleGroup at runtime without materialization** — rejected; breaks
  revoke/trace semantics and ADR 0001 (Grant as dumb data).
- **Require Personnel for every ApplicationUser** — rejected; external accounts
  are a stated business requirement.
