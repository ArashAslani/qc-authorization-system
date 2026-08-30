# QC Access Plugin

Copy this project into the QC product repository. Core (`AccessManagement.*`)
never references `Qc.*`. This plugin never merges to `main`.

## Register on the QC host

After `AddAccessManagementCore()`:

```csharp
builder.Services.AddQcAccessPlugin();
```

That registers:

- `QcAccessSeeder` as `IAccessPluginSeeder` (runs on database initialise)
- `ControlPlanApprovalGuard` and an in-memory `IControlPlanStore` sample

Replace `InMemoryControlPlanStore` with the QC product's persistence.

## What the seeder writes

| Table | Rows |
| --- | --- |
| `Permission` | `LABORATORY.READ/WRITE`, `CONTROLPLAN.READ/UPDATE/APPROVE`, `BOM.UPDATE` with `PluginCode = QC` |
| `ModuleScopeConfig` | LABORATORY max `Workstation`; CONTROLPLAN and BOM max `Company` |

It does **not** insert OrganizationalUnits, Positions, or Grants. Those belong
to the QC product database / admin UI.

Suggested OU types (string `UnitType` values, no Core schema change):

`Holding → Company → Workstation → WorkSite → Shift`

## Grant / Revoke in QC

Use Core commands (`GrantAccessCommand`, `RevokePositionAccessCommand`) with
QC permission ids and `ScopeUnitId` pointing at an `OrganizationalUnit`.
Line-manager subset rules stay in Core.

## Control Plan approval

`ControlPlanApprovalGuard` calls `IAccessEvaluator` for `CONTROLPLAN.APPROVE`,
then enforces the Draft/UnderReview business invariant. The engine has no
`if (permissionCode == "CONTROLPLAN.APPROVE")`.
