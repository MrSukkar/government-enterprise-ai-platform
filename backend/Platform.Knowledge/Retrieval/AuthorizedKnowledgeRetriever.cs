using System.Collections.Immutable;
using Platform.Identity.Access;

namespace Platform.Knowledge.Retrieval;

public sealed class AuthorizedKnowledgeRetriever(
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IEnumerable<IKnowledgeRetrievalSource> retrievalSources,
    IResultFusionService resultFusionService)
{
    private readonly IAccessPolicyEvaluator _accessPolicyEvaluator = accessPolicyEvaluator;
    private readonly IReadOnlyCollection<IKnowledgeRetrievalSource> _retrievalSources = retrievalSources.ToArray();
    private readonly IResultFusionService _resultFusionService = resultFusionService;

    public async Task<AuthorizedKnowledgeContext> RetrieveAsync(
        KnowledgeQuery query,
        CancellationToken cancellationToken)
    {
        query.Validate();
        AuthorizeOrThrow(new AccessRequest(
            query.Identity,
            query.Purpose,
            "knowledge.retrieve",
            "knowledge-scope",
            query.TenantId,
            query.MaximumClassification,
            query.RequiredRoles,
            RequiredPermissions: [],
            InitiatorSubjectId: null,
            RequiresDistinctApprover: false));

        var scope = new AuthorizedRetrievalScope(
            query.TenantId,
            query.Purpose,
            query.MaximumClassification,
            query.AllowedResourceIds,
            query.Modalities,
            query.MaximumResults);

        var selectedSources = _retrievalSources
            .Where(source => query.Modalities.Contains(source.Modality))
            .ToArray();

        var sourceResults = await Task.WhenAll(selectedSources.Select(source =>
            source.RetrieveAsync(query.QueryText, scope, cancellationToken)));

        var candidates = sourceResults.SelectMany(result => result).ToArray();
        ValidateSourceScope(candidates, scope);

        var reranked = _resultFusionService.FuseAndRerank(candidates, query.MaximumResults);
        var authorized = ImmutableArray.CreateBuilder<KnowledgeCandidate>(reranked.Count);

        foreach (var candidate in reranked)
        {
            var decision = _accessPolicyEvaluator.Evaluate(new AccessRequest(
                query.Identity,
                query.Purpose,
                "knowledge.context.read",
                candidate.ResourceId,
                candidate.TenantId,
                candidate.Classification,
                query.RequiredRoles,
                RequiredPermissions: [],
                InitiatorSubjectId: null,
                RequiresDistinctApprover: false));

            if (decision.IsAllowed)
            {
                authorized.Add(candidate);
            }
        }

        return new AuthorizedKnowledgeContext(query.Purpose, query.TenantId, authorized.ToImmutable());
    }

    private void AuthorizeOrThrow(AccessRequest request)
    {
        var decision = _accessPolicyEvaluator.Evaluate(request);
        if (!decision.IsAllowed)
        {
            throw new UnauthorizedAccessException($"Knowledge retrieval denied: {decision.Code}.");
        }
    }

    private static void ValidateSourceScope(
        IEnumerable<KnowledgeCandidate> candidates,
        AuthorizedRetrievalScope scope)
    {
        foreach (var candidate in candidates)
        {
            if (!StringComparer.Ordinal.Equals(candidate.TenantId, scope.TenantId) ||
                !scope.AllowedResourceIds.Contains(candidate.ResourceId) ||
                candidate.Classification > scope.MaximumClassification ||
                !scope.Modalities.Contains(candidate.Modality) ||
                candidate.Relevance is < 0 or > 1)
            {
                throw new InvalidOperationException(
                    $"Retrieval source returned candidate '{candidate.ResourceId}' outside the authorized scope.");
            }
        }
    }
}
