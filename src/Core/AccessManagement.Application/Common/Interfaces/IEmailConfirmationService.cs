namespace AccessManagement.Application.Common.Interfaces;

/// <summary>
/// Issues and validates email-confirmation tokens.
/// TODO: send via a real email provider; the Development implementation only logs the token.
/// </summary>
public interface IEmailConfirmationService
{
    Task<string> CreateTokenAsync(Guid userId, string email, CancellationToken cancellationToken = default);

    Task ConfirmAsync(Guid userId, string token, CancellationToken cancellationToken = default);
}
