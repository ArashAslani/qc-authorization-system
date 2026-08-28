namespace qc_authorization.Domain.Organization.Exceptions;

public class HierarchyCycleException : OrganizationDomainException
{
    public HierarchyCycleException(string message) : base(message) { }
}
