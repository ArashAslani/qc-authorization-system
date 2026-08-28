using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using qc_authorization.Application.Common.Exceptions;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Infrastructure.Data;
using ValidationException = qc_authorization.Application.Common.Exceptions.ValidationException;

namespace qc_authorization.Infrastructure.Identity;

public sealed class PersonnelIdentityBridge : IPersonnelIdentityBridge
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PersonnelIdentityBridge(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task LinkAsync(int personnelId, Guid identityUserId, CancellationToken cancellationToken = default)
    {
        var personnel = await _context.Personnel
            .SingleOrDefaultAsync(p => p.Id == personnelId, cancellationToken)
            ?? throw new Application.Common.Exceptions.NotFoundException(nameof(Domain.Organization.Personnel), personnelId);

        var user = await _userManager.FindByIdAsync(identityUserId.ToString())
            ?? throw new Application.Common.Exceptions.NotFoundException(nameof(ApplicationUser), identityUserId);

        if (personnel.IdentityUserId.HasValue
            && personnel.IdentityUserId != identityUserId)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(personnelId), "Personnel is already linked to another identity user."),
            });
        }

        if (user.PersonnelId.HasValue && user.PersonnelId != personnelId)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(identityUserId), "Identity user is already linked to another personnel record."),
            });
        }

        personnel.LinkIdentityUser(identityUserId);
        user.PersonnelId = personnelId;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new ValidationException(updateResult.Errors.Select(e =>
                new ValidationFailure(e.Code, e.Description)));
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
