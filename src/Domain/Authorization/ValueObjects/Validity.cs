namespace qc_authorization.Domain.Authorization.ValueObjects;

/// <summary>
/// Time-bounded validity window. <see cref="ValidFrom"/> is required;
/// <see cref="ValidTo"/> is null for open-ended grants.
/// </summary>
public sealed class Validity
{
    public DateTimeOffset ValidFrom { get; }
    public DateTimeOffset? ValidTo { get; }

    public Validity(DateTimeOffset validFrom, DateTimeOffset? validTo)
    {
        if (validTo is { } end && end < validFrom)
        {
            throw new ArgumentException(
                "ValidTo cannot be earlier than ValidFrom.", nameof(validTo));
        }
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public bool IsActiveAt(DateTimeOffset when) =>
        when >= ValidFrom && (ValidTo is null || when <= ValidTo);
}
