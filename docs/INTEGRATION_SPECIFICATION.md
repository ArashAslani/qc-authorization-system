# Qc Authorization Integration Specification

**Status**: Post-Core Integration Specification  
**Purpose**: Define how the completed, generic Qc Authorization Core is integrated with the real Qc business modules after the Authorization Core implementation is closed.

> **Important**: This document does not redesign or replace the Authorization Core. It defines how Qc business domains consume it.

---

## 1. Purpose

The Qc Authorization Core is intentionally generic.

It answers:
> Can **Subject X** perform **Action Y** on **Resource Z** under **Context C**?

The Core must not hard-code business-specific concepts such as:
- Laboratory
- Workstation
- BOM
- Control Plan
- Tool
- Company
- Holding

Business modules expose the authorization information required by the Core through stable Resources, Permissions, and authorization Context/Scope.

---

## 2. Architectural Boundary

```
Qc Business Domain
        ↓
Application Use Case
        ↓
Authorization Request
        ↓
Access Evaluation Engine
        ↓
Access Decision
        ↓
Business Operation
```

The business domain owns business rules.  
The Authorization Core owns authorization rules.

---

## 3. Core vs Business Responsibility

### Authorization Core owns
- Grant
- Permission
- Role
- RoleGroup
- Subject
- Scope Evaluation
- Validity
- Priority
- Conflict Resolution
- Position Propagation
- Delegation
- Decision Trace
- Authorization Decision

### Qc Business Domains own
- Holding
- Company
- Laboratory
- Workstation
- BOM
- BOM Item
- Tool
- Control Plan
- Control Plan Item
- Business Relationships
- Business State
- Business Validation
- Business Workflow

The Core consumes the information necessary for authorization evaluation but does not become the owner of these domains.

---

## 4. Resource Model

Every protected Qc capability should have a stable Resource identity.

Examples:
- `PERSONNEL`
- `POSITION`
- `LABORATORY`
- `WORKSTATION`
- `BOM`
- `BOM_ITEM`
- `TOOL`
- `CONTROL_PLAN`
- `CONTROL_PLAN_ITEM`

The final list must be derived from actual Qc business modules.  
Do not create a Resource merely because a database entity exists.  
A Resource should represent a meaningful authorization boundary.

---

## 5. Action Model

Actions represent operations over Resources.

Common actions:
- `READ`
- `CREATE`
- `UPDATE`
- `DELETE`
- `APPROVE`
- `EXECUTE`
- `MANAGE`

Do not automatically create every CRUD permission. Define actions according to real business capabilities.

Examples:
- `CONTROL_PLAN.READ`
- `CONTROL_PLAN.CREATE`
- `CONTROL_PLAN.UPDATE`
- `CONTROL_PLAN.DELETE`
- `CONTROL_PLAN.APPROVE`
- `WORKSTATION.READ`
- `WORKSTATION.MANAGE`

---

## 6. Permission Naming

Use stable, human-readable Permission codes.

Recommended:
`{RESOURCE}.{ACTION}`

Examples:
- `PERSONNEL.READ`
- `PERSONNEL.UPDATE`
- `BOM.READ`
- `BOM.UPDATE`
- `CONTROL_PLAN.READ`
- `CONTROL_PLAN.CREATE`
- `CONTROL_PLAN.UPDATE`
- `CONTROL_PLAN.APPROVE`
- `LABORATORY.READ`
- `LABORATORY.UPDATE`

For genuinely distinct sub-resource capabilities, use an explicit stable convention such as:
- `PERSONNEL.SALARY.READ`
- `PERSONNEL.CONTRACT.UPDATE`

Never encode database IDs into Permission codes.

---

## 7. Permission and Scope Are Different

The core distinction is:
- **Permission** = *What* may the subject do?
- **Scope** = *Where* may the subject do it?

Example:
`CONTROL_PLAN.APPROVE` with `Scope = Company 10` is preferable to creating `CONTROL_PLAN.COMPANY10.APPROVE`.  
This keeps Permissions stable and authorization data manageable.

---

## 8. Resource Context

A Permission alone is often insufficient. For example:
`BOM.UPDATE` does not answer: *Which BOM?*

The application provides the context required for Data Scope evaluation.

Conceptually:
```
AccessRequest
{
    Subject,
    Permission,
    Resource,
    ResourceId,
    Context
}
```

Example:
- Resource = `BOM`
- ResourceId = `500e8400-e29b-41d4-a716-446655440000`
- Context: `CompanyId = aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`

All identifiers (company, resource, personnel, position, grant) use **Guid**. See `docs/ARCHITECTURE.md` §10.

The exact context depends on the business resource.

---

## 9. Organizational Scope

Qc may contain organizational relationships such as:
```
Holding
    ↓
Company
    ↓
Organizational Unit
    ↓
Laboratory
    ↓
Workstation
```

Authorization may therefore need to distinguish:
- Holding Scope
- Company Scope
- Organization Unit Scope
- Laboratory Scope
- Workstation Scope

Do not assume every Resource supports every scope dimension. Each Resource must explicitly define the scope dimensions that make business sense for it.

---

## 10. Business Data Relationships vs Organization Hierarchy

These are different concepts.

### Organization hierarchy
```
Holding
    ↓
Company
    ↓
Position
    ↓
Position
    ↓
Personnel
```
This is relevant to organizational authorization and Position-based grants.

### Business-data relationships
Examples:
```
Company → Laboratory → Workstation → Control Plan
Company → BOM → BOM Item
```
These relationships determine the Data Scope/context of business resources.  
The Authorization Core must not become the owner of these business graphs.

---

## 11. Position-Based Authorization

Position is an authorization subject.

Example:
- Position = `Laboratory Manager`
- Grant: `CONTROL_PLAN.APPROVE`

Position propagation follows the finalized Authorization architecture. For a Position Grant:
`Effective = Position + Ancestors(Position)`

This is organization propagation. It does not mean that the permission automatically propagates through:
`Company → Laboratory → Workstation → Control Plan`

Business-data scope remains a separate concern.

---

## 12. Individual User Grants

Direct User Grants are isolated.

Example:
- User = `Ali`
- Grant = `CONTROL_PLAN.APPROVE`

This must not automatically propagate through Position hierarchy. If Ali changes Position:
Individual Grant remains attached to Ali. It does not move to the new Position.

---

## 13. Role-Based Access

A Role can provide permissions across multiple Qc resources.

Example:
- `ROLE = COMPANY_MANAGER`
  - Permissions:
    - `PERSONNEL.READ`
    - `PERSONNEL.UPDATE`
    - `BOM.READ`
    - `BOM.CREATE`
    - `BOM.UPDATE`
    - `CONTROL_PLAN.READ`
    - `CONTROL_PLAN.APPROVE`
    - `LABORATORY.READ`

The Role remains a Grant Source. The Evaluation Engine determines the final authorization decision.

---

## 14. RoleGroup

RoleGroups package related roles so administrators assign a **bundle** instead of
selecting roles one by one. RoleGroup has **no direct relationship with Permission**;
permissions are defined on member Roles only.

Example:
```
QUALITY_MANAGEMENT
├── QUALITY_MANAGER
├── QUALITY_ENGINEER
└── QUALITY_AUDITOR
```

RoleGroup is a **Grant Source** — not a separate evaluation engine.

### Catalog status

`Role` and `RoleGroup` carry `CatalogStatus` (`Active` / `Inactive`).
Inactive catalog rows cannot be assigned; existing materialized grants from
inactive sources are filtered out at evaluation time.

### Catalog vs assignment

| Operation | Purpose |
|---|---|
| Create RoleGroup / add Role to group | Catalog maintenance |
| Assign RoleGroup to User | Materialize all permissions from all member Roles onto the user |
| Assign RoleGroup to Position | Materialize onto the position (propagation rules apply at evaluation) |
| Revoke RoleGroup from User/Position | Remove all grants with `SourceType.RoleGroup` and matching `SourceId` |
| Update Role / RoleGroup / Position | Name, description, status |

### Materialization flow

```text
RoleGroup
  → RoleGroupMember (Roles)
    → RolePermission (Permissions)
      → Grant (SourceType.RoleGroup, SourceId = RoleGroupId)
        → CatalogGrantFilter (active sources only)
          → AccessEvaluationEngine
```

Re-assigning the same RoleGroup to the same subject replaces existing
materialized grants (idempotent). Changing Role membership inside a group does
**not** auto-update prior assignments — re-assign or revoke/re-assign.

### Admin APIs

- `PUT /api/access-definitions/roles/{id}`
- `POST /api/access-definitions/roles/assign-position`
- `POST /api/access-definitions/roles/revoke-position`
- `PUT /api/access-definitions/role-groups/{id}`
- `POST /api/access-definitions/role-groups/assign-user`
- `POST /api/access-definitions/role-groups/revoke-user`
- `POST /api/access-definitions/role-groups/assign-position`
- `POST /api/access-definitions/role-groups/revoke-position`
- `PUT /api/organization/positions/{id}`
- `GET /api/organization/positions/{id}/authorization-summary`

Do not duplicate authorization evaluation logic inside RoleGroup.

---

## 15. Application Integration Pattern

Every protected application use case should follow:
```
API / Application Use Case
        ↓
Load required business context
        ↓
Build AccessRequest
        ↓
Access Evaluation Engine
        ↓
ALLOW / DENY
        ↓
Execute business operation
```

Do not execute a protected operation first and check authorization afterward.

---

## 16. Example: Update Control Plan

Conceptually:
```
UpdateControlPlan
        ↓
Load ControlPlan
        ↓
Build authorization context
        ↓
Evaluate CONTROL_PLAN.UPDATE
        ↓
DENY → stop
        ↓
ALLOW
        ↓
Apply business changes
        ↓
Save
```

The Control Plan domain owns business invariants.  
Authorization owns: *Is this subject allowed to perform `CONTROL_PLAN.UPDATE`?*  
These are different questions.

---

## 17. Business Rule vs Authorization Rule

Do not confuse **Authorization** with **Business Validation**.

Example:  
User has `CONTROL_PLAN.APPROVE` does not necessarily mean the Control Plan can currently transition to Approved (e.g., it may need to be in `UnderReview` status first).

The operation requires:
`Authorization Check + Business Invariant → Operation`

---

## 18. Resource Context Provider

If a Resource requires business relationships to evaluate Scope, use an application-level context mechanism.

Conceptually:
`IResourceAuthorizationContextProvider`

It may provide:
- `CompanyId`
- `HoldingId`
- `LaboratoryId`
- `WorkstationId`
- `OrganizationUnitId`

for a requested Resource. Keep this abstraction small. Do not create a reflection-heavy metadata framework without a real requirement.

---

## 19. Do Not Put Business Resource Knowledge in the Engine

Avoid:
```csharp
if (resource == "BOM") query BOM;
if (resource == "CONTROL_PLAN") query ControlPlan;
if (resource == "LABORATORY") query Laboratory;
```
inside the Authorization Engine. That would couple the Core to every Qc module.

Preferred:
`Business/Application Layer → Resource Authorization Context → Authorization Engine`

---

## 20. Authorization Context Contract

Conceptually:
```
AuthorizationContext
{
    Resource,
    ResourceId,
    ScopeValues,
    EvaluationTime,
    AdditionalTypedContext
}
```

The Engine evaluates the authorization model against this context. Context must be explicit, deterministic, and testable. Avoid arbitrary dictionaries when a typed model is practical.

---

## 21. Resource Authorization Contract

Each Qc module should define its authorization contract.

Example:
```
ControlPlanAuthorization
--------------------------------
Resource: CONTROL_PLAN
Actions: READ, CREATE, UPDATE, DELETE, APPROVE
Scope Dimensions: Company, Laboratory
```

---

## 22. Resource Catalog

Maintain a central catalog of stable authorization Resources/Permissions.  
The catalog answers: *What can be authorized?*  
It does not answer: *Who is authorized right now?* (which is the Engine's responsibility).

---

## 23. API Enforcement

Every protected endpoint/use case must ultimately enforce authorization through the backend authorization boundary.  
Never rely on frontend hiding buttons as security. Frontend permission checks are UX; backend evaluation is the security boundary.

---

## 24. Multiple Levels of Access

The system supports:
- **WHO** → User / Position / Role / RoleGroup / Delegation
- **WHAT** → Resource + Action
- **WHERE** → Holding / Company / Laboratory / Workstation / Resource Scope
- **WHEN** → Grant / Delegation Validity
- **WHICH RULE WINS** → Priority + Conflict Resolution

This allows different users to have different authorization levels without separate authorization systems per module.

---

## 25. Example: Three Users

1. **Holding Manager**:
   - Role: `HOLDING_MANAGER`
   - Permissions: `BOM.READ`, `CONTROL_PLAN.READ`, `LABORATORY.READ`
   - Scope: `Holding = H1`
2. **Company Manager**:
   - Role: `COMPANY_MANAGER`
   - Permissions: `BOM.READ`, `BOM.CREATE`, `BOM.UPDATE`, `CONTROL_PLAN.READ`, `CONTROL_PLAN.APPROVE`
   - Scope: `Company = A`
3. **Laboratory Manager**:
   - Role: `LAB_MANAGER`
   - Permissions: `LABORATORY.READ`, `LABORATORY.UPDATE`, `WORKSTATION.READ`, `WORKSTATION.MANAGE`, `CONTROL_PLAN.READ`, `CONTROL_PLAN.CREATE`, `CONTROL_PLAN.UPDATE`
   - Scope: `Laboratory = Lab-01`

All three use the same Evaluation Engine. Only their Subjects, Grants, Permissions, Scope, and Priority differ.

---

## 26. Cross-Module Authorization

A business operation may touch multiple Resources.  
For example, *Approve Control Plan* may require `CONTROL_PLAN.APPROVE` and read `WORKSTATION.READ`, `TOOL.READ`.  
Do not automatically combine these into one giant Permission. Evaluate independent permissions explicitly.

---

## 27. Authorization Must Be Composable

A complex operation may perform:
`Authorization Check → Business Validation → Authorization Check for secondary Resource → Business Operation`

All authorization checks use the same Evaluation Engine.

---

## 28. Future Qc Modules

A future module (e.g. Inspection, Calibration, Measurement, Supplier, Product) integrates without modifying the Authorization Core's decision algorithm. It defines:
- Resources
- Actions
- Permissions
- Scope Context
- Application authorization requirements
- Tests

If adding a normal business module requires modifying core authorization rules, treat that as an architectural smell.

---

## 29. Scope Ownership

The business module owns the meaning of its data relationships (e.g., `ControlPlan → Laboratory`). The Authorization layer consumes the resulting context. Do not duplicate business relationship logic inside authorization tables.

---

## 30. Avoid Scope Explosion

Do not create Permissions like `BOM.COMPANY10.UPDATE`.  
Correct: `BOM.UPDATE + Scope`.

---

## 31. Decision Trace

A Decision Trace explains not only the permission requested, but also relevant context:
- `Resource = CONTROL_PLAN`
- `ResourceId = 500e8400-e29b-41d4-a716-446655440000`
- `CompanyId = aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`
- `LaboratoryId = 11111111-1111-1111-1111-111111111101`
- Candidate Grants, Applicable Grants, SourceType, SourceId, Priority, Scope Result, Validity Result, Final Decision, and Reason.

Decision Trace and Audit remain separate:
- **Decision Trace**: *Why was this decision made?*
- **Audit**: *What changed in the authorization system?*

---

## 32. Revoke and Propagation

- For Position:
  - Grant: `P + Ancestors(P)`
  - Revoke: `P + Descendants(P)`
- For Direct User Grants:
  - User only (no hierarchy propagation)

Propagation is computed dynamically, not represented as duplicated authoritative Grants.

---

## 33. Delegation

Delegation remains inside the same Authorization model.  
Mandatory rule: **Delegated Access ⊆ Effective Access of Delegator**.  
The business module must not implement its own delegation engine.

---

## 34. Performance

Start with correctness. Do not introduce premature caching or precomputed permissions. Measure real Qc workloads first and optimize only when an actual bottleneck is identified.

---

## 35. Database Separation

- Business tables remain the source of business truth.
- Authorization tables remain the source of authorization truth.

Do not copy complete business records into authorization tables merely to evaluate Scope.

---

## 36. Integration Order

1. Resource Catalog
2. Permission Catalog
3. Organization Scope
4. Company Scope
5. First real business Resource
6. First protected Application Use Case
7. Decision Trace verification
8. Second Resource
9. Cross-module scenarios
10. Full Qc authorization matrix

---

## 37. Integration Phase 1 — Authorization Catalog

Document for every protected module:
- Resource
- Actions
- Permission Codes
- Scope Dimensions
- Protected Use Cases
- Required Context

---

## 38. Integration Phase 2 — Scope Context

Determine for each Resource what business/organizational context determines access:
- `BOM` → `Company`
- `ControlPlan` → `Company`, `Laboratory`
- `Workstation` → `Company`, `Laboratory`

---

## 39. Integration Phase 3 — Protect Use Cases

For each application operation:
`Use Case → Required Permission → Resource → ResourceId → Scope Context → Evaluation`

---

## 40. Integration Phase 4 — Authorization Matrix

Create a business-facing matrix documenting roles, resources, actions, and scopes. This matrix is for documentation and validation, not a second runtime engine.

---

## 41. Acceptance Criteria

Integration is complete only when:
- [x] Every protected Qc module has a defined Resource model
- [x] Every required action has a stable Permission
- [x] Permission and Scope remain separate
- [x] Business Resources provide required authorization context
- [x] Authorization Core remains business-module agnostic
- [x] Position hierarchy remains owned by Organization
- [x] Business data relationships remain owned by business domains
- [x] Individual Grants remain isolated
- [x] Position propagation follows finalized asymmetric rules
- [x] Delegation uses the same Evaluation Engine
- [x] Protected Application Use Cases enforce authorization
- [x] Frontend-only checks are never treated as security
- [x] Decision Trace explains important authorization decisions
- [x] Audit remains separate from Decision Trace
- [x] Cross-module scenarios are tested
- [x] No business module contains a second authorization engine
- [x] No generic policy/constraint DSL has been introduced without a real requirement

---

## 42. Final Architecture

```
                         Qc Application
                              │
          ┌───────────────────┼───────────────────┐
          ↓                   ↓                   ↓
     Laboratory              BOM             Control Plan
          │                   │                   │
     Workstation          BOM Item            Operations
          │                   │                   │
          └───────────────────┼───────────────────┘
                              │
                       Business Context
                              │
                              ↓
                     AccessRequest
                              │
                              ↓
                Access Evaluation Engine
                              │
             ┌────────────────┼────────────────┐
             ↓                ↓                ↓
          Grants          Position         Delegation
             │           Propagation             │
             └────────────────┼────────────────┘
                              ↓
                     Priority / Conflict
                              ↓
                        ALLOW / DENY
                              ↓
                       Decision Trace
```

The final principle is:
- Business Domains define what exists.
- Authorization defines who may do what.
- Scope defines where.
- Validity defines when.
- Priority defines which rule wins.
- The Evaluation Engine makes the final decision.

---

## 43. Architectural Test

Adding a new Qc module should normally require only:
- Define Resource
- Define Actions
- Define Permissions
- Define Scope Context
- Protect Application Use Cases
- Add Tests

It should **NOT** require:
- Changing the core authorization algorithm
- Adding module-specific branches to the Engine
- Creating another permission system
- Creating another role system
- Creating another authorization service
- Duplicating propagation logic
- Duplicating delegation logic

---

## 44. Multi-Company Workspace Panel

When a user works inside a company panel (see `docs/ARCHITECTURE.md` §11):

- JWT carries `active_company_id` (Guid).
- Position-based grants are unioned **only within the active company**.
- Business modules pass `CompanyId` via `IResourceAuthorizationContextProvider`; the engine matches `ScopeKind.Company` grants against that context.
- Switching company reissues JWT and excludes positions from the previous company — no cross-company permission bleed.
- Holding-level unbounded grants (e.g. holding manager) still apply across companies when scope is `Unbounded`.
