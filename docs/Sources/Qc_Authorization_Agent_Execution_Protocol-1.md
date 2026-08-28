# Autonomous Implementation Task — Qc Authorization System

## Mission

You are an autonomous senior .NET software architect and implementation agent.

Starting from an empty directory, create, implement, test, document, commit, and push the **Qc Authorization / Access Management System**.

You are not being asked only to make a plan. You must actually implement the system phase by phase.

The authoritative project specification is:

`Qc_Authorization_Architecture_Final.md`

The architectural foundation is the current version of:

`https://github.com/jasontaylordev/CleanArchitecture`

Use the upstream repository as the architecture/template reference. Do not fork it; create a new public repository for this project.

---

## 1. Starting Condition

The directory is intentionally empty.

Before implementation:

1. Inspect the environment.
2. Inspect installed .NET SDKs.
3. Verify Git.
4. Verify GitHub CLI (`gh`) and authentication.
5. Inspect the current upstream CleanArchitecture repository/template.
6. Read `Qc_Authorization_Architecture_Final.md` completely.
7. Create the new public GitHub repository.

Preferred repository name:

`qc-authorization`

If unavailable, choose an appropriate unique name.

Do not invent GitHub usernames, credentials, organizations, or repository URLs.

The user has confirmed that you have GitHub access.

---

## 2. Source of Truth

### Architecture

Follow the current upstream CleanArchitecture repository:

`https://github.com/jasontaylordev/CleanArchitecture`

Do not assume an old .NET version or old template structure. Inspect the current repository first.

### Project behavior

`Qc_Authorization_Architecture_Final.md` is the authoritative specification for the Authorization system.

Do not silently change its architectural decisions.

If there is a genuine contradiction or missing business rule, stop the affected work and report the ambiguity instead of inventing behavior.

---

## 3. Core Architecture

The fundamental model is:

**Grant = dumb data**

**Access Evaluation Engine = sole owner of Authorization rules**

Grant contains data such as:

```text
Subject
Permission
Resource
Scope
Effect
SourceType
SourceId
ValidFrom
ValidTo
Priority
```

Grant itself must not contain authorization business logic.

No other service, controller, workflow, delegation service, or domain object may independently make the final:

```text
ALLOW
DENY
```

decision.

All authorization decisions go through the Access Evaluation Engine.

---

## 4. Clean Architecture Rules

Use the current CleanArchitecture structure and conventions as the baseline.

Respect dependency direction:

```text
Domain
   ↑
Application
   ↑
Infrastructure
   ↑
Web/API
```

The Domain layer must not depend on:

- ASP.NET Core
- EF Core
- Infrastructure
- Web/API
- database providers
- external APIs

Application must not contain infrastructure-specific implementations.

Infrastructure owns persistence and external concerns.

Controllers/endpoints must not contain authorization business rules.

Do not expose domain entities directly as API contracts.

Do not introduce unnecessary abstractions.

---

## 5. Do Not Over-Engineer

The target architecture is:

**Enterprise-ready without being Enterprise-complex.**

Do not introduce speculative infrastructure such as:

- generic policy DSL
- generic expression language
- universal rule engine
- unnecessary microservices
- distributed authorization
- event bus without a real requirement
- premature caching
- materialized propagation
- speculative constraints
- unrelated business modules

Implement the simplest solution that satisfies the specification.

---

## 6. Planning

Before feature implementation create:

`docs/IMPLEMENTATION_PLAN.md`

It must contain:

- solution structure
- architecture decisions
- domain model
- application use cases
- persistence model
- API boundaries
- testing strategy
- implementation phases
- phase dependencies
- acceptance criteria

Do not implement all phases at once.

---

# 7. Phase Execution Protocol

For every phase use exactly this lifecycle:

```text
READ
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
PUSH
↓
NEXT PHASE
```

A phase is not complete until all relevant tests pass.

Never push known-broken code to `main`.

---

# 8. Phase 01 — Organization Foundation

Implement:

- Personnel
- Position
- PositionAssignment
- Position Hierarchy
- Cycle Detection

Hierarchy must support:

- Parent
- Children
- Ancestors
- Descendants

Cycle creation must be rejected.

Mandatory tests:

- create Position
- assign Parent
- read Children
- read Ancestors
- read Descendants
- valid hierarchy
- self-parent
- indirect cycle
- invalid re-parenting
- valid re-parenting

Do not implement Authorization Propagation yet.

Commit:

`feat(org): implement organization foundation`

Push the commit.

---

# 9. Phase 02 — Access Definitions and Grant

Implement:

- Resource
- Action
- Permission
- Role
- RolePermission
- Grant
- Grant Source
- Scope
- Effect
- Validity
- Priority

Grant remains dumb data.

Supported Subject Types:

```text
User
Position
Role
RoleGroup
```

Mandatory tests:

- Permission creation
- Role creation
- Role/Permission assignment
- Grant creation
- Source traceability
- Allow
- Deny
- Validity
- Priority

Do not implement Propagation, Delegation, or a generic Constraint DSL yet.

Commit:

`feat(auth): implement access definitions and grant model`

Push.

---

# 10. Phase 03 — Minimal Access Evaluation Engine

Implement:

- AccessRequest
- AccessDecision
- Candidate Grant resolution
- Validity evaluation
- Scope evaluation
- Priority resolution
- Conflict resolution
- Decision Trace

Pipeline:

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
Resolve Effect
↓
Decision
↓
Trace
```

The result must be deterministic.

Mandatory tests:

- Role Allow
- Role Deny
- Position Allow
- Position Deny
- User Direct Allow
- User Direct Deny
- Expired Grant
- Valid Grant
- In-Scope
- Out-of-Scope
- Multiple Grants
- Priority
- Conflict Resolution
- Decision Trace

Do not implement Propagation or Delegation yet.

Commit:

`feat(auth): implement minimal access evaluation engine`

Push.

---

# 11. Phase 04 — Asymmetric Position Propagation

Implement exactly these rules.

## Grant Propagation

For Position `P`:

```text
Effective = P + Ancestors(P)
```

## Revoke Propagation

For Position `P`:

```text
Effective = P + Descendants(P)
```

Ancestors of `P` must NOT be affected by Revoke.

These are two independent business rules.

Do NOT treat:

```text
Revoke = inverse Grant
```

Do NOT implement one generic symmetric propagation function such as:

```text
Propagate(Position, Operation)
```

Use explicitly separate concepts, for example:

```text
ResolveAncestors(P)
ResolveDescendants(P)
```

Propagation must be computed.

Do not materialize derived Grant records for ancestors/descendants as the Source of Truth.

Mandatory tests:

```text
Position Grant
- P affected
- Ancestors affected
- Descendants not affected as propagation targets

Position Revoke
- P affected
- Descendants affected
- Ancestors NOT affected

Hierarchy change
- Grant result reflects hierarchy changes
- Revoke result reflects hierarchy changes
```

Commit:

`feat(auth): implement asymmetric position propagation`

Push.

---

# 12. Individual Grant Isolation

This rule is mandatory.

When:

```text
SubjectType = User
SourceType = User
```

the Grant is an Individual / Direct Grant.

It is completely isolated from Position Propagation.

Therefore:

```text
Individual Grant
→ no Ancestor propagation
→ no Descendant propagation
→ no automatic Position transfer
```

If a person changes Position, the Individual Grant does not move to the new Position.

Individual Revoke is equally isolated.

Mandatory tests:

- Individual Grant
- Individual Revoke
- User changes Position
- no Ancestor propagation
- no Descendant propagation

Commit:

`test(auth): enforce isolated individual grants`

If implementation requires code changes, use an appropriate feature/fix commit and still push the completed phase.

---

# 13. Priority Model

The V1 Priority model is source-aware.

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

Do not rely only on a universal:

```text
Deny > Allow
```

rule when Source Priority determines precedence.

Conflict resolution must be deterministic.

Important future case:

Position-level Override must eventually have explicit tests comparable to Individual Override.

Do not postpone the existing Priority foundation merely because full Delegation/Constraint behavior comes later.

---

# 14. Decision Trace

Decision Trace is required from the beginning.

It must allow an operator/developer to answer:

**Why did this request receive ALLOW or DENY?**

At minimum capture enough information to explain:

- Subject
- requested Permission
- Resource
- ResourceId
- Candidate Grants
- Applicable Grants
- Rejected Grants
- SourceType
- SourceId
- Priority
- Scope result
- Validity result
- Conflict resolution
- Final Decision
- Reason
- Trace identifier

Keep Trace simple and maintainable. It is not a separate generic logging platform.

---

# 15. Phase 05 — Delegation

Only after Phases 01–04 are complete.

Implement:

- Delegation
- ValidFrom
- ValidTo
- Subset Enforcement
- Delegation Source
- Delegation Chain

Delegation creates Grants.

Delegation service must not independently decide final authorization.

Required rule:

```text
Delegated Access ⊆ Effective Access of Delegator
```

Mandatory tests:

- valid delegation
- expired delegation
- allowed subset
- subset violation
- source traceability
- revoke
- delegation chain

Commit:

`feat(auth): implement delegation`

Push.

---

# 16. Phase 06 — Constraints

Do not create a generic Constraint DSL.

Only implement constraints required by real project use cases.

Potential typed constraints:

```text
AmountConstraint
TimeConstraint
ScopeConstraint
```

Every constraint must be:

- deterministic
- testable
- traceable
- maintainable

Include positive and negative tests.

Commit:

`feat(auth): implement authorization constraints`

Push.

---

# 17. Phase 07 — Workflow Integration

Workflow must not contain an independent Authorization engine.

Workflow requests an authorization decision:

```text
Workflow
↓
Required Permission
↓
AccessRequest
↓
Evaluation Engine
↓
Decision
```

Implement only the Workflow integration actually required by the specification.

Tests:

- authorized workflow step
- unauthorized workflow step
- workflow context
- trace contains relevant context

Commit:

`feat(auth): integrate authorization with workflow`

Push.

---

# 18. Phase 08 — API/Application Layer

Expose actual application use cases.

Potential capabilities:

- Permission management
- Role management
- Grant management
- Authorization evaluation
- Position hierarchy access
- Delegation management

Do not create CRUD endpoints merely because entities exist.

Use Application DTOs/contracts according to CleanArchitecture conventions.

Tests:

- valid requests
- invalid requests
- validation
- authorization
- persistence
- error handling

Commit:

`feat(api): expose authorization application use cases`

Push.

---

# 19. Database and Persistence

Use the persistence approach from the current CleanArchitecture template unless the project specification requires otherwise.

Persistence must support the required concepts:

- Permissions
- Roles
- RolePermissions
- Grants
- Grant Sources
- Scopes
- Validity
- Priority
- Personnel
- Positions
- PositionAssignments
- Delegations

Use EF Core according to the architecture.

Create migrations.

Do not store computed Position Propagation as duplicate Grant Source-of-Truth records.

---

# 20. Testing Requirements

Tests are first-class deliverables.

Use appropriate:

- Domain Unit Tests
- Application Unit Tests
- Infrastructure Tests
- Integration Tests
- API/Functional Tests
- Architecture Tests

Critical business rules must have tests, especially:

- Position hierarchy
- Cycle detection
- Grant
- Priority
- Allow/Deny
- Validity
- Scope
- Decision Trace
- Grant Propagation
- Revoke Propagation
- Individual Grant isolation
- Delegation
- Subset enforcement

Do not weaken or delete tests merely to make implementation pass.

---

# 21. Architecture Compliance Gate

At the end of every phase check:

- dependency violations
- business logic in controllers
- authorization logic outside the Engine
- business logic inside Grant
- propagation materialization
- unnecessary abstractions
- unused packages
- dead code
- missing tests
- architecture test failures

Fix violations before marking the phase complete.

---

# 22. Build/Test Gate

A phase is complete only if:

```text
dotnet restore  PASS
dotnet build    PASS
dotnet test     PASS
```

Run additional repository quality checks where available.

---

# 23. Git Commit/Push Gate

Before every phase commit:

```text
git status
git diff
git log --oneline
dotnet build
dotnet test
```

Review the changes.

Then:

```text
git add .
git commit -m "<phase commit message>"
git push
```

Verify that the remote contains the commit.

Every completed phase must be visible in the public GitHub repository history.

---

# 24. Documentation

Maintain:

```text
README.md
docs/IMPLEMENTATION_PLAN.md
docs/ARCHITECTURE.md
docs/TESTING.md
docs/decisions/
```

Document significant decisions such as:

- Grant as dumb data
- Evaluation Engine as sole rule owner
- Computed Position Propagation
- Asymmetric Grant/Revoke Propagation
- Individual Grant isolation
- Priority model

README must explain:

- project purpose
- architecture
- repository structure
- how to run
- how to test
- database setup
- authorization concepts

---

# 25. Handling Ambiguity

If an implementation detail is ambiguous, choose the simplest solution consistent with:

1. `Qc_Authorization_Architecture_Final.md`
2. current CleanArchitecture conventions
3. existing project conventions

If a business rule is genuinely ambiguous:

STOP the affected work.

Report:

```text
AMBIGUITY
Context
Possible interpretations
Impact
Recommended resolution
```

Do not silently invent business behavior.

If the specification contradicts itself, STOP and report the contradiction.

---

# 26. Final Acceptance Criteria

The project is complete only when:

```text
[ ] CleanArchitecture-based solution created
[ ] ASP.NET Core Web API created
[ ] Organization foundation implemented
[ ] Position hierarchy implemented
[ ] Cycle detection implemented
[ ] Permission model implemented
[ ] Role model implemented
[ ] Grant model implemented
[ ] Source traceability implemented
[ ] Priority model implemented
[ ] Access Evaluation Engine implemented
[ ] Decision Trace implemented
[ ] Data Scope implemented
[ ] Position Grant Propagation implemented
[ ] Position Revoke Propagation implemented
[ ] Grant/Revoke asymmetry tested
[ ] Individual Grant isolation implemented
[ ] Individual Grant isolation tested
[ ] Delegation implemented
[ ] Delegation subset enforcement implemented
[ ] Required constraints implemented
[ ] Required Workflow integration implemented
[ ] API/Application use cases implemented
[ ] Database persistence implemented
[ ] Migrations created
[ ] Unit tests passing
[ ] Integration tests passing
[ ] Architecture checks passing
[ ] Documentation complete
[ ] ADRs complete
[ ] Public GitHub repository created
[ ] Every completed phase committed
[ ] Every completed phase pushed
[ ] main branch buildable
[ ] main branch tests passing
```

---

# 27. Final Report

At completion report:

- public repository URL
- GitHub owner
- default branch
- final commit SHA
- .NET version
- database provider
- implemented phases
- test count
- passing test count
- important architectural decisions
- known limitations
- deferred features

Also provide:

```text
git log --oneline
```

The final repository must be public and contain the complete implementation and test suite.

---

# 28. Start Now

Execute these steps immediately:

```text
1. Inspect environment
2. Read Qc_Authorization_Architecture_Final.md completely
3. Inspect the current CleanArchitecture repository
4. Verify GitHub authentication
5. Create the public GitHub repository
6. Initialize the project from the CleanArchitecture foundation
7. Create docs/IMPLEMENTATION_PLAN.md
8. Implement Phase 01
9. Run all relevant tests
10. Review architecture compliance
11. Commit Phase 01
12. Push Phase 01
13. Continue phase by phase
```

Do not skip directly to later phases.

The objective is not merely to produce code.

The objective is to leave behind a **working, tested, documented, CleanArchitecture-compliant public GitHub repository whose commit history demonstrates the incremental implementation of the Qc Authorization System.**
