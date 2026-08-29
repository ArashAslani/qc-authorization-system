# 0012 — RoleGroup is role bundle only (hybrid model rejected)

- Status: Superseded (hybrid rejected)
- Phase: US-ACCESS-01 backend alignment

## Context

An earlier draft introduced a **hybrid RoleGroup** model where groups could hold
group-level `RoleGroupPermission` rows in addition to member `Role` rows.

Product direction changed: **RoleGroup means role group** — it categorizes
`Role` entities only and has no direct relationship with `Permission`.

## Decision

- **RoleGroup** contains only `RoleGroupMember` links to `Role`.
- Permissions live on `Role` via `RolePermission`.
- Assigning a RoleGroup to User/Position materializes:
  `RoleGroup → member Roles → RolePermissions → Grant (SourceType.RoleGroup)`.
- No `RoleGroupPermission` entity, commands, or APIs.

This aligns with [ADR 0011](0011-personnel-user-role-group.md).

## Consequences

- Simpler catalog model; administrators manage permissions on Roles, not groups.
- `RoleGroupGrantMaterializer` requires at least one member role.
- Other US-ACCESS-01 items remain: `CatalogStatus`, Role→Position assign,
  delegation hierarchy, enriched admin queries.

## Alternatives considered

- **Hybrid group-level + role permissions** — rejected; RoleGroup must not
  know about permissions.
