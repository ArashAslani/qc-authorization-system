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

| Gap | Priority | Phase |
|-----|----------|-------|
| Personnel expanded model (NationalId, PersonalCode, …) | Done in Phase 01 refresh | 01 |
| Position CompanyId + cross-company parent rejection | Done in Phase 01 refresh | 01 |
| Personnel ≠ User identity mapping at app boundary | Documented; not unified yet | 01+ |
| Resource / Action as catalog entities | Embedded in Permission strings | 02 |
| RoleGroup entity + membership | Enum only | 02 |
| Delegation subset enforcement | Missing | 06 |
| Delegation chain tests | Missing | 06 |
| Constraints (Amount/Time/Scope) | Not started | 07 |
| Workflow integration | Not started | 08 |
| API use-case endpoints | Not started | 09 |
| EF migrations (uses EnsureCreated) | Missing | 10 |
| Audit (separate from trace) | Not started | 11 |

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
| 00 | Repository recovery & refactoring | **In progress** — DDD refactor done; plan updated |
| 01 | Organization foundation | **In progress** — CompanyId, Personnel model, cross-company |
| 02 | Access definition & grant | ~70% — missing Resource/Action/RoleGroup catalog |
| 03 | Minimal access evaluation | ~95% |
| 04 | Asymmetric position propagation | ~95% |
| 05 | Individual grant isolation | ~95% |
| 06 | Delegation | ~40% — entity/resolver; no subset/chain |
| 07 | Constraints | 0% |
| 08 | Workflow integration | 0% |
| 09 | Application/API | ~10% |
| 10 | Persistence hardening | 0% — no migrations |
| 11 | Audit | 0% |
| 12 | Final hardening | 0% |

## 10. Per-phase gate

```text
READ → DESIGN CHECK → IMPLEMENT → TEST → SELF-REVIEW
→ ARCHITECTURE CHECK → BUILD → COMMIT → NEXT PHASE
```

## 11. Next actions

1. Complete Phase 01 acceptance (Personnel commands, assignment commands, all §46 tests).
2. Phase 02: Resource/Action catalog, RoleGroup entity.
3. Phase 06: Delegation subset enforcement via existing engine.
4. Phase 10: Replace `EnsureCreated` with migrations.
