using qc_authorization.Domain.Common;
using qc_authorization.Domain.Organization.Enums;
using qc_authorization.Domain.Organization.Exceptions;

namespace qc_authorization.Domain.Organization;

/// <summary>
/// A real person in the organization. Not the same concept as an authorization
/// <c>SubjectType.User</c>; system users are mapped at the application boundary.
/// </summary>
public class Personnel : BaseAuditableEntity, IAggregateRoot
{
    private Personnel() { }

    public string NationalId { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PersonalCode { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public PersonnelGender Gender { get; private set; } = PersonnelGender.Unknown;
    public PersonnelStatus Status { get; private set; } = PersonnelStatus.Active;

    /// <summary>
    /// Optional link to the authenticated system user used in authorization requests.
    /// Personnel and User remain distinct concepts.
    /// </summary>
    public int? SystemUserId { get; private set; }

    public List<PositionAssignment> Assignments { get; private set; } = new();

    public static Personnel Create(
        string nationalId,
        string firstName,
        string lastName,
        string personalCode,
        string? phoneNumber = null,
        PersonnelGender gender = PersonnelGender.Unknown,
        PersonnelStatus status = PersonnelStatus.Active,
        int? systemUserId = null)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            throw new OrganizationDomainException("NationalId is required.");
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new OrganizationDomainException("FirstName is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new OrganizationDomainException("LastName is required.");
        }

        if (string.IsNullOrWhiteSpace(personalCode))
        {
            throw new OrganizationDomainException("PersonalCode is required.");
        }

        return new Personnel
        {
            NationalId = nationalId.Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PersonalCode = personalCode.Trim(),
            PhoneNumber = phoneNumber?.Trim(),
            Gender = gender,
            Status = status,
            SystemUserId = systemUserId,
        };
    }

    public void LinkSystemUser(int systemUserId)
    {
        if (systemUserId <= 0)
        {
            throw new OrganizationDomainException("SystemUserId must be a positive identifier.");
        }

        SystemUserId = systemUserId;
    }
}
