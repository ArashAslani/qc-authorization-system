using AccessManagement.Domain.Organization;

namespace Qc.AccessPlugin;

/// <summary>
/// QC-owned permission codes. Core never references these strings.
/// </summary>
public static class QcPermissions
{
    public const string PluginCode = "QC";

    public const string LaboratoryRead = "LABORATORY.READ";
    public const string LaboratoryWrite = "LABORATORY.WRITE";
    public const string ControlPlanRead = "CONTROLPLAN.READ";
    public const string ControlPlanUpdate = "CONTROLPLAN.UPDATE";
    public const string ControlPlanApprove = "CONTROLPLAN.APPROVE";
    public const string BomUpdate = "BOM.UPDATE";

    /// <summary>
    /// Suggested OU types for a QC host. Core only requires
    /// <see cref="OrganizationalUnitTypes.Company"/>.
    /// </summary>
    public static class SuggestedUnitTypes
    {
        public const string Holding = OrganizationalUnitTypes.Holding;
        public const string Company = OrganizationalUnitTypes.Company;
        public const string Workstation = "Workstation";
        public const string WorkSite = "WorkSite";
        public const string Shift = "Shift";
    }
}
