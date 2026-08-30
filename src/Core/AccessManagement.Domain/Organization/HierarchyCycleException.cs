namespace AccessManagement.Domain.Organization.Exceptions;

public class HierarchyCycleException : OrganizationDomainException
{
    public HierarchyCycleException(string message) : base(message) { }
}
