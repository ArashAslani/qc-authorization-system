# Qc Authorization System — Master Implementation & Refactoring Specification

> **Document Status:** Authoritative Implementation Specification  
> **Purpose:** Rebuild/refactor the existing Qc backend codebase to conform to the finalized Authorization architecture, then implement the complete system phase-by-phase.  
> **Primary Audience:** Cursor / Senior Backend Engineer  
> **Architecture:** Clean Architecture + DDD-oriented Domain Model + EF Core + ASP.NET Core Web API  
> **Core Principle:** `Grant = dumb data` and `Access Evaluation Engine = sole owner of authorization rules`

---

# 1. Mission

The current repository is **not the source of truth for architecture**.

The existing codebase is a relatively small implementation baseline. Its current entities, services, repositories, naming, folder structure, abstractions, and partial implementations may be incomplete or incorrect.

Your job is **not** to preserve the current implementation merely because it exists.

Your job is to:

1. Inspect the entire existing repository.
2. Understand what is reusable and what is not.
3. Compare it against this specification.
4. Refactor or replace incorrect parts.
5. Establish a clean, maintainable foundation.
6. Implement the complete Qc Authorization system.
7. Keep the architecture simple and enterprise-ready without speculative complexity.
8. Test every business rule.
9. Never silently invent business behavior where the specification is ambiguous.

The final result must be a coherent system, not a collection of locally correct classes.

---

# 2. Source of Truth and Precedence

When making implementation decisions, use this precedence:

1. **This specification / finalized Qc Authorization architecture**
2. Existing Clean Architecture conventions in the repository
3. Existing project coding conventions where they do not conflict
4. Existing implementation only when it is compatible with the above

Do NOT treat existing code as authoritative.

If existing code conflicts with the specification:

> Refactor the code.

If the specification is genuinely ambiguous:

```text
STOP
↓
Report:
- Ambiguity
- Context
- Possible interpretations
- Architectural/business impact
- Recommended interpretation
```

Do not silently invent a business rule.

---

# 3. Core Business Model

Qc Authorization is based on two strictly separated responsibilities.

## 3.1 Grant

A Grant is raw authorization data.

Conceptually:

```text
Grant
├── Subject
├── Permission
├── Resource
├── Scope
├── Effect
├── SourceType
├── SourceId
├── ValidFrom
├── ValidTo
└── Priority
```

Grant is **dumb data**.

Grant must NOT decide:

```text
ALLOW
DENY
Propagation
Conflict Resolution
Delegation validity
Constraint evaluation
```

No authorization business rule belongs inside the Grant entity.

---

## 3.2 Access Evaluation Engine

The Access Evaluation Engine is the **single owner of authorization rules**.

All final authorization decisions must flow through it:

```text
Workflow / Application / API
        ↓
AccessRequest
        ↓
Access Evaluation Engine
        ↓
AccessDecision
```

No controller, workflow, service, delegation service, role service, or domain object may independently produce the final authorization decision.

---

# 4. Architectural Boundaries

Use Clean Architecture.

Dependency direction:

```text
Domain
   ↑
Application
   ↑
Infrastructure
   ↑
Web/API
```

Equivalent conceptual dependency:

```text
Web/API
   ↓
Application
   ↓
Domain

Infrastructure
   ↓
Application / Domain contracts
```

## Domain must not depend on

- ASP.NET Core
- EF Core
- Infrastructure
- Web/API
- Database providers
- External APIs

## Application must not contain

- EF Core-specific persistence implementation
- Database-specific implementation details
- HTTP concerns
- Controller logic

## Web/API must not contain

- Authorization business rules
- Grant conflict logic
- Propagation logic
- Delegation subset logic

Controllers/endpoints should be thin.

Do not expose Domain entities directly as API contracts.

---

# 5. Engineering Philosophy

Target:

> **Enterprise-ready without being Enterprise-complex.**

Prefer:

```text
Simple
Explicit
Deterministic
Testable
Traceable
Maintainable
```

Avoid speculative infrastructure.

Do NOT introduce:

```text
Generic Policy DSL
Generic Expression Language
Universal Rule Engine
Generic Constraint Framework
Unnecessary Microservices
Distributed Authorization
Premature Caching
Materialized Propagation
Unnecessary Event Bus
Speculative Infrastructure
```

Do not abstract something merely because it can theoretically be abstracted.

---

# 6. Initial Repository Assessment

Before changing code:

1. Inspect solution/projects.
2. Inspect `.csproj` files.
3. Inspect existing Domain entities.
4. Inspect Value Objects.
5. Inspect Enums.
6. Inspect Domain Events.
7. Inspect Application use cases.
8. Inspect repositories/interfaces.
9. Inspect EF Core configurations.
10. Inspect DbContext.
11. Inspect migrations.
12. Inspect API endpoints/controllers.
13. Inspect dependency injection.
14. Inspect tests.
15. Inspect naming conventions.
16. Inspect existing error/exception strategy.
17. Inspect existing authentication integration if present.

Create:

```text
docs/IMPLEMENTATION_PLAN.md
```

The plan must include:

- Current repository assessment
- Target architecture
- Domain model
- Application use cases
- Persistence model
- API boundaries
- Testing strategy
- Refactoring decisions
- Implementation phases
- Dependencies
- Acceptance criteria

Do not implement all phases in one pass.

---

# 7. Domain Model

The target system contains the following major concepts.

## Organization Foundation

```text
Personnel
Position
PositionAssignment
Position Hierarchy
Cycle Detection
```

## Access Definition

```text
Resource
Action
Permission

Role
RolePermission
RoleGroup
```

## Authorization

```text
Grant
Grant Source
Scope
Effect
Validity
Priority
```

## Evaluation

```text
AccessRequest
AccessDecision
Candidate Grants
Priority Resolution
Conflict Resolution
Decision Trace
```

## Delegation

```text
Delegation
Delegation Validity
Delegation Subset
Delegation Chain
Delegation Source
```

## Constraints

Only real requirements.

Potential typed constraints:

```text
AmountConstraint
TimeConstraint
ScopeConstraint
```

Do not create a generic DSL.

## Audit

Audit is separate from Decision Trace.

---

# 8. Organization Foundation

## 8.1 Personnel

Personnel represents a real person.

A person may or may not be a system user.

Do not assume:

```text
Personnel == User
```

These are different concepts.

Personnel may have:

```text
Personnel
├── NationalId
├── FirstName
├── LastName
├── PersonalCode
├── PhoneNumber
├── Gender
├── Status
└── PositionAssignments
```

Position assignments represent the person's organizational relationship.

---

# 9. Position

Position represents an organizational position.

Minimum conceptual model:

```text
Position
├── Id
├── CompanyId
├── Code
├── Title
├── Description
├── ParentPositionId
└── Status
```

A Position has at most one parent.

A Position may have many children.

Root:

```text
ParentPositionId = null
```

Self-parenting is forbidden.

A Position hierarchy belongs to the organizational domain.

Authorization must consume hierarchy information but must not own organizational hierarchy rules.

---

# 10. Position Hierarchy

Hierarchy must support:

```text
Parent
Children
Ancestors
Descendants
```

Example:

```text
CEO
│
├── Director A
│   │
│   ├── Manager A1
│   └── Manager A2
│
└── Director B
```

For `Manager A1`:

```text
Parent:
Director A

Ancestors:
Director A
CEO

Descendants:
none
```

Hierarchy queries must be deterministic.

Do not store duplicated authorization grants for every ancestor/descendant.

---

# 11. Cycle Detection

Hierarchy must never contain cycles.

Invalid:

```text
A → B → C → A
```

Also invalid:

```text
A → A
```

Cycle detection must work for indirect cycles.

Re-parenting must be validated.

Example:

```text
A
└── B
    └── C
```

Changing `A.Parent = C` must be rejected.

Hierarchy validation belongs to Organization Domain/application orchestration as appropriate, but must not leak into Authorization Engine.

---

# 12. Company Boundary

A Position must not have a parent from another Company.

Invalid:

```text
Position A
Company = X

Parent Position B
Company = Y
```

The implementation must enforce organizational consistency.

Do not solve this by coupling Domain entities directly to EF Core.

Use the appropriate domain/application boundary to validate references to existing aggregates.

---

# 13. Access Definition

## 13.1 Resource

Represents the protected business resource.

Examples may include:

```text
Personnel
Position
ControlPlan
BaseInformation
```

Resource must remain a stable authorization concept.

---

## 13.2 Action

Represents an operation.

Examples:

```text
READ
CREATE
UPDATE
DELETE
APPROVE
EXECUTE
```

Do not hardcode business-specific authorization logic into Action.

---

## 13.3 Permission

Permission is the authorization capability being requested.

Conceptually:

```text
Permission
├── Resource
└── Action
```

A permission should be uniquely identifiable.

Example:

```text
PERSONNEL.READ
PERSONNEL.UPDATE
```

---

# 14. Role

Role is a source of permissions.

Example:

```text
HR_MANAGER
    ↓
PERSONNEL.READ
PERSONNEL.UPDATE
PERSONNEL.APPROVE
```

Role assignment must be represented explicitly.

Do not make Role itself responsible for final authorization evaluation.

---

# 15. RoleGroup

RoleGroup groups roles.

Example:

```text
HR_ROLE_GROUP
├── HR_MANAGER
├── HR_SPECIALIST
└── HR_AUDITOR
```

RoleGroup is another Grant Source.

Do not duplicate authorization evaluation logic inside RoleGroup.

---

# 16. Grant Model

A Grant is a raw authorization fact.

Conceptual schema:

```text
Grant
--------------------------------
Id
SubjectType
SubjectId
PermissionId
ResourceId / Resource
Scope
Effect
SourceType
SourceId
ValidFrom
ValidTo
Priority
```

Exact relational representation may differ if a better normalized schema is justified.

The behavior must remain equivalent.

---

# 17. Subject Types

The system must distinguish:

```text
User
Position
Role
RoleGroup
```

At the data-model level, SubjectType must be explicit.

Do not infer the subject type from arbitrary IDs.

Example:

```text
SubjectType = Position
SubjectId   = 205
```

is fundamentally different from:

```text
SubjectType = User
SubjectId   = 205
```

This distinction is mandatory.

---

# 18. Grant Sources

Initial Grant Sources:

```text
Role
Position
RoleGroup
User
Delegation
```

Every Grant must be traceable to its source.

Minimum:

```text
SourceType
SourceId
```

Examples:

```text
SourceType = Role
SourceId   = 100
```

```text
SourceType = Position
SourceId   = 205
```

```text
SourceType = Delegation
SourceId   = 5001
```

Source traceability is required for:

- Revoke
- Audit
- Decision Trace
- Debugging
- Chain Revoke

Adding a new source later must not require rewriting the core evaluation algorithm.

---

# 19. Effect

Authorization effects:

```text
ALLOW
DENY
```

Do not assume:

```text
DENY always beats ALLOW
```

Conflict resolution is priority-aware.

---

# 20. Validity

Grant validity is time-based.

Conceptually:

```text
ValidFrom
ValidTo
```

Expired grants remain historical data.

Do not delete expired grants merely because they are no longer effective.

The Engine evaluates validity at request time.

---

# 21. Scope

Authorization may be scoped.

Examples:

```text
Company
Department
Organization Unit
Resource
```

The exact V1 scope model must remain typed and explicit.

Do not introduce a generic query language for scopes.

Scope evaluation belongs to the Access Evaluation Engine.

---

# 22. Priority

V1 priority is source-aware.

Baseline ordering:

```text
Individual Override
        >
Position Override
        >
Delegation
        >
Role / RoleGroup
        >
Propagated
```

Priority must be represented and traceable.

Do not rely only on:

```text
Deny > Allow
```

When source precedence determines the outcome.

Example:

```text
Role Grant
Effect   = ALLOW
Priority = 30

Individual Override
Effect   = DENY
Priority = 100
```

Result:

```text
DENY
```

Conflict resolution must be deterministic.

---

# 23. Access Evaluation Engine

The Engine is the only component allowed to make final authorization decisions.

Input:

```text
AccessRequest
```

Output:

```text
AccessDecision
```

Conceptual pipeline:

```text
AccessRequest
      ↓
Find Candidate Grants
      ↓
Check Validity
      ↓
Check Scope
      ↓
Resolve Priority
      ↓
Resolve Conflict
      ↓
ALLOW / DENY
      ↓
Decision Trace
```

The result must be deterministic.

---

# 24. AccessRequest

Conceptually contains enough information to evaluate:

```text
Subject
Permission
Resource
ResourceId
Evaluation Time
Relevant Context
```

Do not put business rules inside the request DTO/value object.

---

# 25. AccessDecision

Must provide at least:

```text
Decision
Reason
TraceId
```

The decision must be explainable through Decision Trace.

---

# 26. Candidate Grant Resolution

The Engine first determines which grants may be relevant.

Candidate resolution must consider the request subject and applicable authorization sources.

Do not prematurely optimize with caching/materialized authorization.

Correctness comes first.

---

# 27. Decision Trace

Decision Trace answers:

> Why did this request receive ALLOW or DENY?

At minimum capture enough information to explain:

```text
TraceId
Subject
Requested Permission
Resource
ResourceId
Candidate Grants
Applicable Grants
Rejected Grants
SourceType
SourceId
Priority
Scope Result
Validity Result
Conflict Resolution
Final Decision
Reason
```

Keep this simple.

Decision Trace is not a generic logging platform.

---

# 28. Position Propagation

Propagation is computed by the Engine.

It is NOT materialized as duplicate grants.

## 28.1 Position Grant

For Position `P`:

```text
Effective = P + Ancestors(P)
```

Example:

```text
A
│
B
│
C
```

Grant on `C`:

```text
C = affected
B = affected
A = affected
```

Descendants of C are not propagation targets.

---

# 29. Position Revoke

Revoke is an independent authorization operation.

For Position `P`:

```text
Effective = P + Descendants(P)
```

Example:

```text
A
│
B
│
C
```

Revoke on `B`:

```text
B = affected
C = affected
A = NOT affected
```

This asymmetry is mandatory.

Do NOT model:

```text
Revoke = inverse Grant
```

Do NOT use a generic symmetric implementation that accidentally makes Revoke affect ancestors.

Prefer explicit concepts such as:

```text
ResolveAncestors(position)
ResolveDescendants(position)
```

---

# 30. Individual Grant Isolation

When:

```text
SubjectType = User
SourceType = User
```

the Grant is an Individual/Direct Grant.

It is completely isolated from Position Propagation.

Therefore:

```text
Individual Grant
→ User only
→ no Ancestor propagation
→ no Descendant propagation
→ no automatic Position transfer
```

If a person changes Position:

```text
Individual Grant remains with User
```

It does not move to the new Position.

Individual Revoke is equally isolated.

---

# 31. Revoke and Source Traceability

Revoke must not simply mean:

```text
DELETE FROM Grants
```

The system must be able to identify what authorization source is being revoked.

Use:

```text
SourceType
SourceId
```

for traceability.

Revoke must obey priority/conflict rules and must not bypass the evaluation model.

---

# 32. Hierarchy Changes

Propagation is computational.

If the hierarchy changes:

```text
A
│
B
│
C
```

and a relationship changes, future authorization evaluation must reflect the current hierarchy according to the propagation rules.

Do not create permanent duplicated propagated grants.

This keeps hierarchy changes naturally reflected in evaluation.

---

# 33. Delegation

Delegation is introduced only after the base Evaluation Engine and Propagation are stable.

Conceptually:

```text
Delegator
    ↓
Delegation
    ↓
Grant
    ↓
Evaluation Engine
```

Delegation itself does not independently decide final authorization.

---

# 34. Delegation Validity

Delegation supports:

```text
ValidFrom
ValidTo
```

Expired delegation must not grant effective access.

---

# 35. Delegation Subset Enforcement

A user must not delegate more access than they effectively possess.

Mandatory rule:

```text
Delegated Access ⊆ Effective Access of Delegator
```

Example:

```text
Ali:
READ
UPDATE
```

Ali cannot delegate:

```text
DELETE
```

unless Ali effectively has DELETE.

Subset enforcement must be evaluated using the same authorization model.

Avoid creating a second authorization engine for Delegation.

---

# 36. Delegation Chain

Example:

```text
Ali
 ↓
Sara
 ↓
Reza
```

If Sara received an authorization from Ali but does not have permission to delegate it further, Sara cannot delegate it to Reza.

Delegation therefore eventually needs a concept equivalent to:

```text
Delegable
```

or another explicit rule that provides the same behavior.

Delegation chains must be traceable.

---

# 37. Constraints

Do not create a generic constraint DSL.

Do not build:

```text
IF Amount > ...
AND Department = ...
AND ...
```

as a general-purpose expression engine.

Implement only real requirements.

Potential typed constraints:

```text
AmountConstraint
TimeConstraint
ScopeConstraint
```

Each constraint must be:

```text
Deterministic
Testable
Traceable
Maintainable
```

---

# 38. Workflow Integration

Workflow must not implement its own authorization engine.

Correct:

```text
Workflow
   ↓
Required Permission
   ↓
AccessRequest
   ↓
Access Evaluation Engine
   ↓
Decision
```

Workflow should consume the authorization decision.

Only implement workflow integrations actually required by the business.

---

# 39. API/Application Layer

Do not create CRUD endpoints merely because entities exist.

Expose actual application use cases.

Potential capabilities:

```text
Permission Management
Role Management
RoleGroup Management
Grant Management
Authorization Evaluation
Position Hierarchy
Delegation Management
```

Use Application DTOs/contracts.

Controllers/endpoints remain thin.

---

# 40. Persistence / Database Design

Use EF Core unless the repository specification explicitly requires another persistence mechanism.

The database must persist the authoritative business data.

Core tables/entities should conceptually cover:

```text
Personnel
Position
PositionAssignment

Resource
Action
Permission

Role
RolePermission
RoleGroup

Grant

Delegation

Audit
```

Depending on the chosen relational model, some concepts may be represented through lookup/value tables or owned structures.

## Important persistence rules

### Grant

Store:

```text
SubjectType
SubjectId
PermissionId
Scope
Effect
SourceType
SourceId
ValidFrom
ValidTo
Priority
```

### Position

Store:

```text
Id
CompanyId
ParentPositionId
Code
Title
Description
Status
```

### Position hierarchy

Use adjacency-list style hierarchy unless there is a proven need for another model:

```text
ParentPositionId
```

Do not prematurely introduce closure tables/materialized paths solely for authorization propagation.

If scale later proves that hierarchy traversal is a bottleneck, address it as a measured optimization.

---

# 41. Database Integrity

Where appropriate, enforce invariants at database level in addition to application/domain validation.

Examples:

- Primary keys
- Foreign keys where representable
- Unique constraints
- Indexes
- Required columns
- Valid relationships

Be careful with polymorphic references such as:

```text
SubjectType + SubjectId
SourceType + SourceId
```

These cannot always be represented as normal foreign keys.

Application/domain rules and indexes must compensate appropriately.

---

# 42. Indexing Strategy

Indexes should be driven by actual query patterns.

Likely important access patterns include:

```text
Grant by SubjectType + SubjectId
Grant by Permission
Grant by SourceType + SourceId
Grant by Validity
Grant by Scope
Position by ParentPositionId
Position by CompanyId
RolePermission by RoleId
```

Do not add dozens of speculative indexes.

---

# 43. Audit

Audit and Decision Trace are different concepts.

## Decision Trace

Answers:

> Why did this authorization evaluation result in ALLOW/DENY?

## Audit

Answers:

> What changed in the authorization system?

Examples:

```text
Role Created
Permission Added
Grant Created
Grant Revoked
Delegation Created
Delegation Revoked
Position Permission Changed
```

Keep these separate.

Audit should preserve historical authorization changes.

---

# 44. Authentication vs Authorization

Do not confuse:

```text
Authentication
```

with:

```text
Authorization
```

Authentication identifies the current system user.

Authorization determines whether that subject can perform an action.

The Authorization Engine should consume an already-established identity/context rather than becoming an identity provider.

---

# 45. Testing Strategy

Tests are first-class deliverables.

Use appropriate layers:

```text
Domain Unit Tests
Application Unit Tests
Infrastructure Tests
Integration Tests
API/Functional Tests
Architecture Tests
```

Every critical business rule requires explicit tests.

---

# 46. Mandatory Organization Tests

At minimum:

```text
Create Position
Assign Parent
Read Children
Read Ancestors
Read Descendants

Valid Hierarchy
Self Parent
Indirect Cycle
Invalid Re-parenting
Valid Re-parenting

Cross-company Parent Rejection
```

---

# 47. Mandatory Access Definition Tests

At minimum:

```text
Permission Creation
Role Creation
Role/Permission Assignment
RoleGroup
Grant Creation
Source Traceability
Allow
Deny
Validity
Priority
```

---

# 48. Mandatory Evaluation Tests

At minimum:

```text
Role Allow
Role Deny

Position Allow
Position Deny

User Direct Allow
User Direct Deny

Expired Grant
Valid Grant

In Scope
Out of Scope

Multiple Grants
Priority
Conflict Resolution

Decision Trace
```

---

# 49. Mandatory Propagation Tests

Use a hierarchy:

```text
A
│
B
│
C
```

Test:

```text
Position Grant on C
→ C affected
→ B affected
→ A affected

Position Grant on B
→ B affected
→ A affected
→ C NOT affected as propagation target
```

Revoke:

```text
Position Revoke on B
→ B affected
→ C affected
→ A NOT affected
```

Hierarchy changes:

```text
Grant result reflects hierarchy changes
Revoke result reflects hierarchy changes
```

---

# 50. Mandatory Individual Isolation Tests

```text
Individual Grant
Individual Revoke
User changes Position

No Ancestor propagation
No Descendant propagation
No automatic Position transfer
```

These four behaviors must be independently covered.

---

# 51. Mandatory Delegation Tests

```text
Valid Delegation
Expired Delegation
Allowed Subset
Subset Violation
Source Traceability
Delegation Revoke
Delegation Chain
Non-delegable Access
```

---

# 52. Architecture Tests

Architecture tests should verify at least:

```text
Domain does not depend on Infrastructure
Domain does not depend on ASP.NET Core
Domain does not depend on EF Core

Application does not depend on Web/API
Application does not contain infrastructure implementation

API does not contain authorization business rules

Domain entities are not exposed directly as API contracts
```

---

# 53. Refactoring Rules

When inspecting existing code:

## Keep existing code when

- It matches the target model.
- Its boundaries are correct.
- Its behavior is tested.
- Its naming is consistent.
- Refactoring would add no value.

## Refactor when

- Business rules live in the wrong layer.
- Authorization is duplicated.
- Grant contains evaluation logic.
- Controllers decide authorization.
- Propagation is materialized.
- Individual grants accidentally propagate.
- Domain depends on infrastructure.
- API exposes domain entities.
- Repository abstractions are unnecessary or misplaced.
- Existing abstractions add complexity without value.

## Replace when

The existing implementation fundamentally contradicts the architecture.

Do not preserve bad abstractions merely to minimize diff size.

---

# 54. No Big-Bang Implementation

Never implement all phases in one change.

Use this lifecycle for every phase:

```text
READ
↓
DESIGN CHECK
↓
IMPLEMENT
↓
TEST
↓
SELF-REVIEW
↓
ARCHITECTURE CHECK
↓
BUILD
↓
COMMIT
↓
NEXT PHASE
```

A phase is complete only when its tests pass.

Never intentionally leave the repository in a known-broken state at a phase boundary.

---

# 55. Phase Plan

## Phase 00 — Repository Recovery & Refactoring

Deliver:

```text
Repository assessment
Target architecture
docs/IMPLEMENTATION_PLAN.md
Clean Architecture alignment
Removal/replacement of conflicting code
Buildable baseline
Passing existing/reworked tests
```

Acceptance:

```text
[ ] Repository understood
[ ] Architecture aligned
[ ] Build passes
[ ] Tests pass
[ ] No known architectural contradiction
```

---

## Phase 01 — Organization Foundation

Implement:

```text
Personnel
Position
PositionAssignment
Position Hierarchy
Cycle Detection
```

Hierarchy:

```text
Parent
Children
Ancestors
Descendants
```

Acceptance:

```text
[ ] Personnel
[ ] Position
[ ] PositionAssignment
[ ] Parent
[ ] Children
[ ] Ancestors
[ ] Descendants
[ ] Self-parent rejection
[ ] Indirect-cycle rejection
[ ] Re-parent validation
[ ] Cross-company validation
[ ] Tests pass
```

---

## Phase 02 — Access Definition & Grant

Implement:

```text
Resource
Action
Permission

Role
RolePermission
RoleGroup

Grant
Grant Source
Scope
Effect
Validity
Priority
```

Grant remains dumb data.

Do not implement:

```text
Propagation
Complex Delegation
Generic Constraint DSL
```

yet.

Acceptance:

```text
[ ] Access definitions complete
[ ] Grant model complete
[ ] Subject types explicit
[ ] Source traceability complete
[ ] Validity complete
[ ] Priority foundation complete
[ ] Tests pass
```

---

## Phase 03 — Minimal Access Evaluation

Implement:

```text
AccessRequest
AccessDecision
Candidate Grant Resolver
Validity Evaluation
Scope Evaluation
Priority Resolver
Conflict Resolver
Decision Trace
```

Pipeline:

```text
Request
↓
Candidates
↓
Validity
↓
Scope
↓
Priority
↓
Conflict
↓
Decision
↓
Trace
```

Do not implement Propagation or Delegation yet.

Acceptance:

```text
[ ] Deterministic
[ ] ALLOW
[ ] DENY
[ ] Validity
[ ] Scope
[ ] Priority
[ ] Conflict resolution
[ ] Trace
[ ] Tests pass
```

---

## Phase 04 — Asymmetric Position Propagation

Implement:

```text
Ancestors
Descendants
Grant Propagation
Revoke Propagation
```

Exact contract:

```text
Grant on P
→ P + Ancestors(P)

Revoke on P
→ P + Descendants(P)
```

Do not materialize derived grants.

Acceptance:

```text
[ ] Grant propagation
[ ] Revoke propagation
[ ] Asymmetry preserved
[ ] Hierarchy changes reflected
[ ] No materialized propagated Source-of-Truth
[ ] Tests pass
```

---

## Phase 05 — Individual Grant Isolation

Ensure:

```text
User Grant
→ User only
```

No organizational propagation.

Acceptance:

```text
[ ] Individual Grant isolation
[ ] Individual Revoke isolation
[ ] Position change does not move Grant
[ ] Tests pass
```

---

## Phase 06 — Delegation

Implement:

```text
Delegation
Validity
Subset Enforcement
Delegation Source
Delegation Chain
Delegable behavior
```

Mandatory rule:

```text
Delegated Access ⊆ Effective Access of Delegator
```

Acceptance:

```text
[ ] Delegation
[ ] Validity
[ ] Subset enforcement
[ ] Chain
[ ] Revoke
[ ] Traceability
[ ] Tests pass
```

---

## Phase 07 — Constraints

Implement only real required constraints.

Possible:

```text
Amount
Time
Scope
```

No generic DSL.

Acceptance:

```text
[ ] Required constraints only
[ ] Deterministic
[ ] Testable
[ ] Traceable
[ ] Tests pass
```

---

## Phase 08 — Workflow Integration

Integrate:

```text
Workflow
↓
AccessRequest
↓
Evaluation Engine
↓
Decision
```

Acceptance:

```text
[ ] Authorized workflow
[ ] Unauthorized workflow
[ ] Relevant trace context
[ ] Tests pass
```

---

## Phase 09 — Application/API

Implement actual use cases:

```text
Permission Management
Role Management
RoleGroup Management
Grant Management
Evaluation
Hierarchy
Delegation
```

Do not expose unnecessary CRUD.

Acceptance:

```text
[ ] DTOs/contracts
[ ] Validation
[ ] Error handling
[ ] Authorization
[ ] Persistence
[ ] Integration tests
[ ] API tests
```

---

## Phase 10 — Persistence Hardening

Complete:

```text
EF Core Configurations
Indexes
Constraints
Migrations
Transactions where required
Concurrency handling where required
```

Validate:

```text
[ ] Schema matches domain
[ ] Queries are correct
[ ] No materialized propagation
[ ] Migrations apply cleanly
[ ] Integration tests pass
```

---

## Phase 11 — Audit & Operational Completeness

Implement:

```text
Authorization Audit
Decision Trace persistence/observability as required
Trace identifiers
Revoke traceability
Delegation traceability
```

Acceptance:

```text
[ ] Authorization changes auditable
[ ] Decisions explainable
[ ] Source chain traceable
```

---

## Phase 12 — Final Hardening

Perform:

```text
Full test suite
Architecture tests
Integration tests
API tests
Migration verification
Code review
Dead-code cleanup
Naming consistency
Documentation
```

Do not add speculative features.

---

# 56. Important Non-Goals

Do NOT implement unless a later requirement explicitly demands them:

```text
❌ Generic Policy DSL
❌ Generic Rule Engine
❌ Generic Constraint Language
❌ Distributed Authorization Service
❌ Authorization Microservice
❌ Materialized Propagation
❌ Premature Authorization Cache
❌ Event Bus solely for Authorization
❌ Complex graph database
❌ Unnecessary CQRS
❌ Unnecessary MediatR-style abstraction
❌ Speculative multi-tenant framework
❌ Speculative permission inheritance system
```

The goal is a strong core, not architectural decoration.

---

# 57. Code Quality Standards

Use modern C#/.NET conventions already established by the repository where compatible.

Prefer:

```text
Sealed domain entities where appropriate
Private setters
Explicit factory methods
Small methods
Meaningful names
Guard clauses
Immutable/value-oriented concepts where appropriate
Explicit domain exceptions
Deterministic behavior
```

Avoid:

```text
God classes
Static global state
Anemic services containing all business rules
Deep inheritance hierarchies
Magic strings where a typed concept is appropriate
Overly generic repositories
Repositories with dozens of speculative methods
```

Do not blindly force every entity into the same pattern.

Choose the simplest correct model per concept.

---

# 58. Domain Events

Use Domain Events only where they represent meaningful domain facts and the existing architecture supports them.

Do not introduce events merely to make the architecture look sophisticated.

Examples of legitimate domain facts:

```text
PersonnelCreated
PositionParentChanged
PositionAssignmentStarted
GrantCreated
GrantRevoked
DelegationCreated
```

Events must not become a hidden second authorization engine.

---

# 59. Transactions and Consistency

Use transactions around application operations that modify multiple pieces of authorization state and require atomic consistency.

Examples:

```text
Role + RolePermission changes
Delegation + generated Grant
Grant revoke + related state
```

Do not create distributed transactions.

Do not introduce an event bus merely to avoid straightforward transactional application logic.

---

# 60. Concurrency

Identify concurrency-sensitive operations.

Especially:

```text
Position re-parenting
Grant modification/revoke
Delegation modification
Role permission modification
```

If optimistic concurrency is required by the actual persistence model, implement it explicitly.

Do not add concurrency tokens everywhere without a reason.

---

# 61. Security Rules

Authorization is security-sensitive.

Never:

```text
Trust SubjectId from client without identity validation
Trust SourceId from client blindly
Allow caller to bypass Evaluation Engine
Return sensitive authorization internals unnecessarily
Expose domain entities directly
```

The authenticated identity must be mapped to the authorization subject through the application boundary.

---

# 62. Performance Strategy

Correctness first.

Do not prematurely optimize.

The first implementation should use clear deterministic database queries and in-memory hierarchy traversal where appropriate.

Measure before introducing:

```text
Caching
Materialized hierarchy
Closure tables
Distributed cache
Precomputed authorization
```

If performance becomes a real issue, optimize the measured bottleneck without changing the business contract.

---

# 63. Final Acceptance Criteria

The system is complete only when:

```text
[ ] Clean Architecture aligned
[ ] Organization foundation implemented
[ ] Position hierarchy implemented
[ ] Cycle detection implemented

[ ] Resource implemented
[ ] Action implemented
[ ] Permission implemented
[ ] Role implemented
[ ] RolePermission implemented
[ ] RoleGroup implemented

[ ] Grant implemented
[ ] Grant is dumb data
[ ] SubjectType implemented
[ ] SourceType implemented
[ ] SourceId implemented
[ ] Scope implemented
[ ] Effect implemented
[ ] Validity implemented
[ ] Priority implemented

[ ] Access Evaluation Engine implemented
[ ] Candidate resolution implemented
[ ] Validity evaluation implemented
[ ] Scope evaluation implemented
[ ] Priority resolution implemented
[ ] Conflict resolution implemented
[ ] Decision Trace implemented

[ ] Position Grant Propagation implemented
[ ] Position Revoke Propagation implemented
[ ] Grant/Revoke asymmetry tested

[ ] Individual Grant isolation implemented
[ ] Individual Grant isolation tested

[ ] Delegation implemented
[ ] Delegation subset enforcement implemented
[ ] Delegation chain implemented
[ ] Delegation revoke implemented

[ ] Required constraints implemented
[ ] Workflow integration implemented

[ ] Application use cases implemented
[ ] API boundaries implemented
[ ] DTOs/contracts implemented

[ ] EF Core persistence complete
[ ] Migrations created
[ ] Indexes reviewed
[ ] Database constraints reviewed

[ ] Unit tests passing
[ ] Integration tests passing
[ ] API/functional tests passing
[ ] Architecture tests passing

[ ] Audit implemented
[ ] Decision trace explainability verified

[ ] Documentation complete
[ ] No known architectural contradictions
[ ] No known broken tests
```

---

# 64. Final Working Rule for Cursor

You are not being asked to blindly generate code.

You are being asked to act as a senior backend engineer performing a controlled architectural migration.

For every phase:

```text
Inspect
↓
Understand
↓
Compare
↓
Design
↓
Refactor
↓
Implement
↓
Test
↓
Review
↓
Verify architecture
↓
Build
↓
Proceed
```

If existing code is wrong, replace it.

If existing code is correct, reuse it.

If the specification is ambiguous, stop and report.

If a proposed abstraction is not needed, do not add it.

If a business rule belongs to the Evaluation Engine, do not put it in Grant or Controller.

If a derived authorization result can be computed, do not materialize it as authoritative data.

The final system must be:

```text
Correct
Deterministic
Traceable
Tested
Maintainable
Simple
Extensible
```

---

# 65. Start Here

The first execution must be:

```text
1. Read this document completely.

2. Inspect the entire current repository.

3. Produce a concise repository assessment:
   - Current structure
   - Existing reusable components
   - Conflicting components
   - Missing components
   - Risky components
   - Proposed refactoring

4. Create:
   docs/IMPLEMENTATION_PLAN.md

5. Establish the target architecture.

6. Refactor only the foundation required for Phase 01.

7. Implement Phase 01.

8. Run tests.

9. Perform architecture review.

10. Stop and report Phase 01 result.

11. Only after Phase 01 passes, continue to Phase 02.
```

Do not skip directly to Phase 03 or later.

Do not implement the entire system in one change.

The architecture is intentionally incremental because authorization rules are security-sensitive and each phase must establish a tested foundation for the next phase.
