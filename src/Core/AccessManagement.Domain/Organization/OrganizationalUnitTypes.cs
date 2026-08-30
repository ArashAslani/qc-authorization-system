namespace AccessManagement.Domain.Organization;

/// <summary>
/// Well-known unit types stored as strings so products can add their own
/// without changing Core schema. <see cref="Company"/> is required by A1.4.
/// </summary>
public static class OrganizationalUnitTypes
{
    public const string Holding = "Holding";
    public const string Company = "Company";
}
