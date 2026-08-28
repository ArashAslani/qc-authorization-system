namespace qc_authorization.Domain.Authorization.ValueObjects;

/// <summary>
/// Optional data-scope attached to a Grant. A null <see cref="ScopeKind"/>
/// means the grant is unbounded (applies to any data scope).
/// </summary>
public enum ScopeKind
{
    Unbounded = 0,
    Company = 1,
    Branch = 2,
    Custom = 3,
}

public sealed class Scope
{
    public ScopeKind Kind { get; }
    public string? Identifier { get; }

    public Scope(ScopeKind kind, string? identifier)
    {
        if (kind != ScopeKind.Unbounded && string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException(
                $"Scope kind '{kind}' requires a non-empty identifier.", nameof(identifier));
        }
        Kind = kind;
        Identifier = identifier;
    }

    public static Scope Unbounded() => new(ScopeKind.Unbounded, null);
    public static Scope Company(string companyId) => new(ScopeKind.Company, companyId);
    public static Scope Branch(string branchId) => new(ScopeKind.Branch, branchId);
    public static Scope Custom(string id) => new(ScopeKind.Custom, id);

    public override string ToString() =>
        Kind == ScopeKind.Unbounded ? "*" : $"{Kind}:{Identifier}";
}
