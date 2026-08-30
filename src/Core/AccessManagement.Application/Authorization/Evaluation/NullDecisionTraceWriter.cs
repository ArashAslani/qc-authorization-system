using AccessManagement.Application.Abstractions;
using AccessManagement.Domain.Authorization.Evaluation;

namespace AccessManagement.Application.Authorization.Evaluation;

public sealed class NullDecisionTraceWriter : IDecisionTraceWriter
{
    public Task<AccessDecision> WriteAsync(
        AccessRequest request,
        IReadOnlyList<Domain.Authorization.Grant> candidates,
        Domain.Authorization.Grant? winner,
        AccessDecision decision,
        CancellationToken ct = default) =>
        Task.FromResult(decision);
}
