using System.Text.Json;
using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Audit;
using AccessManagement.Domain.Authorization.Evaluation;

namespace AccessManagement.Application.Authorization.Evaluation;

public sealed class DecisionTraceWriter : IDecisionTraceWriter
{
    private readonly IApplicationDbContext _db;

    public DecisionTraceWriter(IApplicationDbContext db) => _db = db;

    public Task<AccessDecision> WriteAsync(
        AccessRequest request,
        IReadOnlyList<Grant> candidates,
        Grant? winner,
        AccessDecision decision,
        CancellationToken ct = default)
    {
        var payload = candidates.Select(g => new
        {
            g.Id,
            g.Priority,
            Effect = g.Effect.ToString(),
            g.SourceType,
            g.SubjectType,
            g.SubjectId,
            Won = winner is not null && g.Id == winner.Id,
        });

        _db.AccessDecisionLogs.Add(new AccessDecisionLog
        {
            RequestedByUserId = request.SubjectUserId,
            ActivePositionId = request.ActivePositionId,
            PermissionCode = request.PermissionCode,
            ScopeUnitId = request.ResourceScopeUnitId,
            Decision = decision.Allowed ? "Allow" : "Deny",
            Reason = decision.Reason,
            CandidateGrantsJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTimeOffset.UtcNow,
            TraceId = decision.TraceId,
        });

        return Task.FromResult(decision);
    }
}
