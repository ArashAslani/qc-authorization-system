using qc_authorization.Application.Authorization.Queries.EvaluateAccess;
using qc_authorization.Domain.Authorization.Evaluation;
using Mapster;

namespace qc_authorization.Application.Common.Mappings;

public static class MappingConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<AccessDecision, AccessDecisionDto>
            .NewConfig()
            .Map(d => d.Effect, s => s.Effect.ToString())
            .Map(d => d.Reason, s => s.Reason.ToString())
            .Map(d => d.Trace, s => s.Trace);

        TypeAdapterConfig<DecisionTrace, AccessDecisionTraceDto>
            .NewConfig()
            .Map(d => d.FinalDecision, s => s.FinalDecision.ToString())
            .Map(d => d.CandidateCount, s => s.CandidateGrants.Count)
            .Map(d => d.ApplicableCount, s => s.ApplicableGrants.Count);
    }
}
