using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Understanding;

public sealed class GovernedUnderstandingEngine(
    IUnderstandingContextProvider contextProvider,
    IUnderstandingAnalyzer analyzer)
{
    public async Task<UnderstandingReport> UnderstandAsync(
        UnderstandingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var snapshot = await contextProvider.LoadAuthorizedSnapshotAsync(request, cancellationToken);
        ValidateSnapshot(request, snapshot);
        var candidate = await analyzer.AnalyzeAsync(request, snapshot, cancellationToken);
        ValidateCandidate(request, snapshot, candidate);
        return new(
            request.Id,
            candidate.Summary,
            candidate.SummaryClassification,
            candidate.Claims,
            candidate.AnalysisEvidenceReference,
            DateTimeOffset.UtcNow);
    }

    private static void ValidateSnapshot(
        UnderstandingRequest request,
        UnderstandingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.RequestId != request.Id || !StringComparer.Ordinal.Equals(snapshot.TenantId, request.TenantId))
            throw new UnauthorizedAccessException("Understanding snapshot is outside the authorized request scope.");
        if (snapshot.CapturedAt < request.RequestedAt)
            throw new InvalidOperationException("Understanding snapshot cannot precede the request.");
        if (snapshot.EnterpriseObjects.IsDefault || snapshot.Facts.IsDefault || snapshot.AuthorizedEvidenceReferences is null)
            throw new InvalidOperationException("Understanding snapshot collections must be explicit.");

        foreach (var enterpriseObject in snapshot.EnterpriseObjects)
        {
            enterpriseObject.Validate();
            if (!StringComparer.Ordinal.Equals(enterpriseObject.TenantId, request.TenantId) ||
                !request.ObjectScope.Contains(enterpriseObject.Id) ||
                enterpriseObject.Classification > request.MaximumClassification)
                throw new UnauthorizedAccessException("Understanding snapshot contains an unauthorized Enterprise Object.");
        }

        var loadedObjectIds = snapshot.EnterpriseObjects.Select(item => item.Id).ToHashSet();
        foreach (var fact in snapshot.Facts)
        {
            fact.Validate();
            if (fact.Classification > request.MaximumClassification ||
                fact.EnterpriseObjectReferences.Any(id =>
                    !request.ObjectScope.Contains(id) || !loadedObjectIds.Contains(id)) ||
                fact.EvidenceReferences.Any(reference => !snapshot.AuthorizedEvidenceReferences.Contains(reference)))
                throw new UnauthorizedAccessException("Understanding snapshot contains an unauthorized fact.");
        }
    }

    private static void ValidateCandidate(
        UnderstandingRequest request,
        UnderstandingSnapshot snapshot,
        UnderstandingCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (candidate.RequestId != request.Id || candidate.IsExecutable)
            throw new InvalidOperationException("Understanding candidate has an invalid identity or execution posture.");
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Summary);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.AnalysisEvidenceReference);
        if (candidate.GeneratedAt < snapshot.CapturedAt)
            throw new InvalidOperationException("Understanding analysis cannot precede its snapshot.");
        if (candidate.Claims.IsDefaultOrEmpty)
            throw new InvalidOperationException("Understanding analysis requires at least one classified claim.");
        if (candidate.SummaryClassification > request.MaximumClassification ||
            candidate.SummaryClassification < candidate.Claims.Max(claim => claim.Classification))
            throw new InvalidOperationException("Understanding summary cannot downgrade its claims or exceed authorization.");

        var loadedObjectIds = snapshot.EnterpriseObjects.Select(item => item.Id).ToHashSet();
        foreach (var claim in candidate.Claims)
        {
            claim.Validate();
            if (claim.Classification > request.MaximumClassification ||
                claim.EnterpriseObjectReferences.Any(id =>
                    !request.ObjectScope.Contains(id) || !loadedObjectIds.Contains(id)) ||
                claim.EvidenceReferences.Any(reference => !snapshot.AuthorizedEvidenceReferences.Contains(reference)))
                throw new UnauthorizedAccessException("Understanding candidate contains an unauthorized claim.");

            if (claim.KnowledgeState is RelationshipKnowledgeState.Confirmed or RelationshipKnowledgeState.Discovered)
            {
                var grounded = snapshot.Facts.Any(fact =>
                    fact.KnowledgeState == claim.KnowledgeState &&
                    StringComparer.Ordinal.Equals(fact.Statement, claim.Statement) &&
                    fact.Classification == claim.Classification &&
                    claim.EnterpriseObjectReferences.ToHashSet().SetEquals(fact.EnterpriseObjectReferences) &&
                    claim.EvidenceReferences.All(fact.EvidenceReferences.Contains));
                if (!grounded)
                    throw new InvalidOperationException("Confirmed and discovered claims must match grounded facts exactly.");
            }
            else
            {
                var supportingFacts = snapshot.Facts
                    .Where(fact => claim.EvidenceReferences.Any(fact.EvidenceReferences.Contains))
                    .ToArray();
                if (claim.KnowledgeState == RelationshipKnowledgeState.Inferred && supportingFacts.Length == 0)
                    throw new InvalidOperationException("Inferred claims require grounded supporting facts.");
                if (supportingFacts.Any(fact => fact.Classification > claim.Classification) ||
                    (supportingFacts.Length == 0 && claim.Classification != request.MaximumClassification))
                    throw new InvalidOperationException("Understanding claims cannot downgrade source classification.");
                if (claim.KnowledgeState == RelationshipKnowledgeState.Inferred &&
                    claim.EnterpriseObjectReferences.Any(id =>
                        supportingFacts.All(fact => !fact.EnterpriseObjectReferences.Contains(id))))
                    throw new InvalidOperationException("Inferred claims must retain their supporting object scope.");
            }
        }
    }
}
