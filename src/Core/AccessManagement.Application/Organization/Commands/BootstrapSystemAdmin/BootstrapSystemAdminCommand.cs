using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization.Commands.BootstrapSystemAdmin;

/// <summary>
/// The only command that does not implement <c>IRequireUserAdmin</c>.
/// Self-disabling: succeeds only while no Personnel with <c>IsSystemUser</c> exists.
/// </summary>
public sealed record BootstrapSystemAdminCommand(
    string NationalId,
    string FirstName,
    string LastName,
    string PersonnelCode,
    Guid IdentityUserId
) : IRequest<Guid>;

public sealed class BootstrapSystemAdminCommandHandler : IRequestHandler<BootstrapSystemAdminCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public BootstrapSystemAdminCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(BootstrapSystemAdminCommand request, CancellationToken ct)
    {
        var anyAdminExists = await _db.Personnel.AsNoTracking().AnyAsync(p => p.IsSystemUser, ct);
        if (anyAdminExists)
        {
            // This path is permanently locked after the first successful run.
            throw new AuthorizationDomainException(
                "System already has at least one admin. Bootstrap is disabled.");
        }

        var personnel = Personnel.Create(
            request.NationalId,
            request.FirstName,
            request.LastName,
            request.PersonnelCode,
            identityUserId: request.IdentityUserId,
            isSystemUser: true);

        _db.Personnel.Add(personnel);
        await _db.SaveChangesAsync(ct);
        return personnel.Id;
    }
}
