namespace qc_authorization.Domain.Authorization.ValueObjects;

/// <summary>
/// Numeric priority. Higher wins. The source-aware priority map is
/// defined in <see cref="SourcePriority"/>.
/// </summary>
public readonly record struct Priority(int Value)
{
    public static Priority Default => new(0);

    public static implicit operator int(Priority p) => p.Value;
    public static implicit operator Priority(int value) => new(value);
}

/// <summary>
/// Canonical source -> default priority mapping. The numbers are the
/// contract; the relative ordering is what matters. See
/// docs/decisions/0006-source-aware-priority-model.md.
/// </summary>
public static class SourcePriority
{
    public const int IndividualOverride = 100;
    public const int PositionOverride = 90;
    public const int Delegation = 70;
    public const int RoleOrRoleGroup = 50;
    public const int Propagated = 10;
}
