namespace AccessManagement.Application.Common.Security;

/// <summary>
/// Marker for catalog/org mutating requests that only a User Admin
/// (<c>IsSystemUser</c> or <c>ACCESS.ADMINISTER_ALL</c>) may send.
/// </summary>
public interface IRequireUserAdmin;
