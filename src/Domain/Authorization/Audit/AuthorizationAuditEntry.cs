using qc_authorization.Domain.Common;

namespace qc_authorization.Domain.Authorization.Audit;

public class AuthorizationAuditEntry : BaseAuditableEntity
{
    private AuthorizationAuditEntry() { }

    public string EventType { get; private set; } = string.Empty;
    public Guid? ActorUserId { get; private set; }
    public string Payload { get; private set; } = string.Empty;

    public static AuthorizationAuditEntry Create(string eventType, Guid? actorUserId, string payload) =>
        new()
        {
            EventType = eventType,
            ActorUserId = actorUserId,
            Payload = payload,
        };
}
