# Qc Authorization System — Implementation Plan

> **Authoritative spec:** `docs/Sources/Qc_Authorization_Master_Implementation_Spec.md`  
> **Architecture summary:** `docs/ARCHITECTURE.md`  
> **ADRs:** `docs/decisions/`

## 1. Goal

Deliver a Clean Architecture + DDD-oriented, .NET 10, SQLite-backed Authorization /
Access Management system. `Grant = dumb data`; `Access Evaluation Engine = sole
owner of Allow/Deny`.

## 2. Repository assessment (Phase 00)

### Current structure (aligned)

```text
src/Domain          → entities, value objects, domain services, evaluation engine
src/Application     → MediatR commands, repository ports, evaluator façade
src/Infrastructure  → EF Core, repository implementations
src/Web             → composition root (feature endpoints pending)
tests/*             → Domain, Application, Integration, Architecture tests
```

Layering enforced by `tests/ArchitectureTests/LayeringTests.cs`. Application has
no EF Core reference.

### Reusable / correct

| Area | Status |
|------|--------|
| Grant as dumb data | Aligned — `Grant.Create()` validates facts only |
| Access Evaluation Engine | Aligned — `AccessEvaluationEngine` in Domain |
| Asymmetric propagation | Aligned — `GrantApplicabilityService` |
| Individual grant isolation | Aligned + tested |
| Repository pattern | Aligned — ports in Application, EF in Infrastructure |
| Decision trace | Aligned |
| Source-aware priority | Aligned — `SourcePriority` |
| Position hierarchy service | Aligned — ancestors/descendants/cycle detection |

### Gaps vs master spec

All Phase 00–12 gaps from the original assessment are closed. See §9 below.

### Risky / replace candidates

- `DirectCandidateGrantResolver` — unused; remove or keep for Phase 03 regression
- Application tests reference Infrastructure for in-memory EF — acceptable for now
- `SubjectType.User` uses integer IDs without explicit User aggregate — clarify at API layer

## 3. Solution structure

Unchanged from prior plan. See master spec §6.

## 4. Architectural north star

Unchanged. See `docs/ARCHITECTURE.md` and ADRs 0001–0009.

## 5. Domain model (target)

### Organization

```text
Personnel (NationalId, FirstName, LastName, PersonalCode, PhoneNumber, Gender, Status)
Position (CompanyId, Code, Title, Description, ParentPositionId, Status)
PositionAssignment (PersonnelId, PositionId, ValidFrom, ValidTo)
```

### Access definition (Phase 02)

```text
Resource, Action, Permission
Role, RolePermission, RoleGroup
Grant (dumb data)
```

### Evaluation (Phase 03–05)

```text
AccessRequest, AccessDecision, DecisionTrace
AccessEvaluationEngine
GrantApplicabilityService
```

### Delegation (Phase 06)

```text
Delegation, Delegable, subset enforcement, chain control
```

## 6. Application use cases (target)

- Organization: create personnel, positions, assignments, re-parent
- Authorization: create/list grants, evaluate access
- Delegation: create/revoke with subset check
- Workflow: step authorization via engine
- API: thin endpoints over MediatR commands (Phase 09)

## 7. Persistence

EF Core 10 + SQLite. Migrations under `src/Infrastructure/Data/Migrations/` (Phase 10).
No materialized propagation grants.

## 8. Testing strategy

See master spec §§45–52 and `docs/TESTING.md`. Every phase gate:

```text
dotnet restore && dotnet build && dotnet test
```

## 9. Phase plan (master spec §55)

| Phase | Title | Status |
|------:|-------|--------|
| 00 | Repository recovery & refactoring | **Complete** |
| 01 | Organization foundation | **Complete** |
| 02 | Access definition & grant | **Complete** |
| 03 | Minimal access evaluation | **Complete** |
| 04 | Asymmetric position propagation | **Complete** |
| 05 | Individual grant isolation | **Complete** |
| 06 | Delegation | **Complete** — subset, chain, revoke |
| 07 | Constraints | **Complete** — Amount/Time/Scope |
| 08 | Workflow integration | **Complete** |
| 09 | Application/API | **Complete** |
| 10 | Persistence hardening | **Complete** — EF migrations |
| 11 | Audit | **Complete** |
| 12 | Final hardening | **Complete** |

## 11. Final acceptance checklist (master spec §63)

- [x] Grant remains dumb data; engine is sole Allow/Deny owner
- [x] Asymmetric position propagation (Allow → ancestors, Deny → descendants)
- [x] Individual grants isolated from position propagation
- [x] Delegation subset enforcement and chain control
- [x] Typed constraints (Amount, Time, Scope) in evaluation pipeline
- [x] Workflow consumes engine via `WorkflowStepAuthorizer`
- [x] API endpoints expose application use cases (not entity CRUD)
- [x] EF Core migrations replace `EnsureCreated` in production path
- [x] Authorization audit log separate from decision trace
- [x] `dotnet build && dotnet test` green (108 tests)

## 12. Next actions

All phases 00–12 are complete. Future work: authentication integration, production database provider, and additional business constraints as needed.
