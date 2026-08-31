using AccessManagement.Domain.Common;
using AccessManagement.Domain.Organization.Enums;
using AccessManagement.Domain.Organization.Exceptions;

namespace AccessManagement.Domain.Organization;

/// <summary>
/// A real person in the organization. Not the same concept as an authorization
/// <c>SubjectType.User</c>; authenticated users are mapped at the application boundary.
/// </summary>
public class Personnel : BaseAuditableEntity, IAggregateRoot
{
    private Personnel() { }

    public string? NationalId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? PersonnelCode { get; private set; }
    public string? PhoneNumber { get; private set; }
    public PersonnelGender Gender { get; private set; } = PersonnelGender.Unknown;
    public PersonnelStatus Status { get; private set; } = PersonnelStatus.Active;
    public bool IsSystemUser { get; private set; }

    /// <summary>
    /// Optional link to the ASP.NET Core Identity user (Account).
    /// </summary>
    public Guid? IdentityUserId { get; private set; }

    public List<PositionAssignment> Assignments { get; private set; } = new();

    public static Personnel Create(
        string? nationalId,
        string firstName,
        string lastName,
        string? personalCode,
        string? phoneNumber = null,
        PersonnelGender gender = PersonnelGender.Unknown,
        PersonnelStatus status = PersonnelStatus.Active,
        Guid? identityUserId = null,
        bool isSystemUser = false)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new OrganizationDomainException("FirstName is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new OrganizationDomainException("LastName is required.");
        }

        return new Personnel
        {
            NationalId = string.IsNullOrWhiteSpace(nationalId) ? null : nationalId.Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PersonnelCode = string.IsNullOrWhiteSpace(personalCode) ? null : personalCode.Trim(),
            PhoneNumber = phoneNumber?.Trim(),
            Gender = gender,
            Status = status,
            IdentityUserId = identityUserId,
            IsSystemUser = isSystemUser,
        };
    }

    public void LinkIdentityUser(Guid identityUserId)
    {
        if (identityUserId == Guid.Empty)
        {
            throw new OrganizationDomainException("IdentityUserId must be a valid identifier.");
        }

        IdentityUserId = identityUserId;
    }

    public void UnlinkIdentityUser() => IdentityUserId = null;

    public void SetStatus(PersonnelStatus status) => Status = status;
}
