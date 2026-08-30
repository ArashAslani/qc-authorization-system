namespace AccessManagement.Domain.Authorization.Exceptions;

public class AuthorizationDomainException : Exception
{
    public AuthorizationDomainException(string message) : base(message) { }

    public AuthorizationDomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
