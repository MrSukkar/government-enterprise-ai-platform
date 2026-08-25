using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Platform.SoftwareFactory.ClosedLoop;

public sealed class ClosedLoopEngine(
    IClosedLoopContextProvider contextProvider,
    IClosedLoopAnalyzer analyzer,
    IImprovementProposalRepository proposalRepository,
    TimeProvider timeProvider)
{
    public async Task<ImmutableArray<ImprovementProposal>> EvaluateAsync(
        ClosedLoopEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var context = await contextProvider.LoadAuthorizedContextAsync(request, cancellationToken);
        var evaluatedAt = timeProvider.GetUtcNow();
        ValidateContext(request, context, evaluatedAt);
        var candidates = await analyzer.AnalyzeAsync(request, context, cancellationToken);
        if (candidates.IsDefault) throw new InvalidOperationException("Closed-loop analyzer returned no explicit result.");

        var authorizedEvidence = context.DeliveryEvidenceReferences
            .AddRange(context.RegistrationEvidenceReferences)
            .AddRange(context.TelemetryEvidenceReferences)
            .Add(context.PolicyVerificationEvidenceReference)
            .ToHashSet(StringComparer.Ordinal);
        var proposals = ImmutableArray.CreateBuilder<ImprovementProposal>();
        foreach (var candidate in candidates)
        {
            ValidateCandidate(candidate, authorizedEvidence);
            var fingerprintInput = string.Join('|', request.TenantId, request.EnterpriseObjectReference,
                request.ReleaseArtifactSha256Digest, request.Policy.PolicyId, request.Policy.Version,
                candidate.Kind, candidate.ProposedIntent);
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintInput))).ToLowerInvariant();
            var proposal = new ImprovementProposal(
                Guid.NewGuid(), fingerprint, request.RequestId, request.TenantId,
                request.EnterpriseObjectReference, request.ReleaseArtifactSha256Digest,
                candidate.Kind, candidate.Title, candidate.Rationale, candidate.ProposedIntent,
                candidate.Confidence, candidate.EvidenceReferences.Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal).ToImmutableArray(), evaluatedAt);
            var persisted = await proposalRepository.CreateAtomicallyAsync(proposal, cancellationToken);
            ValidatePersisted(proposal, persisted);
            proposals.Add(persisted);
        }
        if (proposals.Select(item => item.Fingerprint).Distinct(StringComparer.Ordinal).Count() != proposals.Count)
            throw new InvalidOperationException("Closed-loop analyzer returned duplicate improvement intents.");
        return proposals.OrderBy(item => item.Fingerprint, StringComparer.Ordinal).ToImmutableArray();
    }

    private static void ValidateContext(
        ClosedLoopEvaluationRequest request,
        ClosedLoopContext context,
        DateTimeOffset evaluatedAt)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.RequestId != request.RequestId ||
            !StringComparer.Ordinal.Equals(context.TenantId, request.TenantId) ||
            !StringComparer.Ordinal.Equals(context.EnterpriseObjectReference, request.EnterpriseObjectReference) ||
            !StringComparer.OrdinalIgnoreCase.Equals(context.ReleaseArtifactSha256Digest, request.ReleaseArtifactSha256Digest) ||
            !StringComparer.Ordinal.Equals(context.ReleaseProvenanceReference, request.ReleaseProvenanceReference) ||
            !context.PolicySignatureValid ||
            !StringComparer.Ordinal.Equals(context.VerifiedPolicyId, request.Policy.PolicyId) ||
            !StringComparer.Ordinal.Equals(context.VerifiedPolicyVersion, request.Policy.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(context.VerifiedPolicySha256Digest, request.Policy.Sha256Digest))
            throw new UnauthorizedAccessException("Closed-loop context verification failed closed.");
        if (context.DeliveryEvidenceReferences.IsDefaultOrEmpty ||
            context.RegistrationEvidenceReferences.IsDefaultOrEmpty ||
            context.TelemetryEvidenceReferences.IsDefaultOrEmpty ||
            context.CapturedAt < request.ObservationWindowEnd || context.CapturedAt > evaluatedAt)
            throw new InvalidOperationException("Closed-loop context is incomplete or has invalid time.");
        ArgumentException.ThrowIfNullOrWhiteSpace(context.PolicyVerificationEvidenceReference);
    }

    private static void ValidateCandidate(ImprovementCandidate candidate, HashSet<string> authorizedEvidence)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (!Enum.IsDefined<ImprovementKind>(candidate.Kind) || candidate.Confidence is < 0 or > 1)
            throw new InvalidOperationException("Improvement candidate kind or confidence is invalid.");
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Rationale);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.ProposedIntent);
        if (candidate.EvidenceReferences.IsDefaultOrEmpty ||
            candidate.EvidenceReferences.Any(reference => !authorizedEvidence.Contains(reference)))
            throw new UnauthorizedAccessException("Improvement candidate cited evidence outside the closed-loop context.");
    }

    private static void ValidatePersisted(ImprovementProposal expected, ImprovementProposal persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        if (persisted.ProposalId != expected.ProposalId ||
            !StringComparer.Ordinal.Equals(persisted.Fingerprint, expected.Fingerprint) ||
            persisted.RequestId != expected.RequestId || !StringComparer.Ordinal.Equals(persisted.TenantId, expected.TenantId) ||
            !StringComparer.Ordinal.Equals(persisted.EnterpriseObjectReference, expected.EnterpriseObjectReference) ||
            !StringComparer.OrdinalIgnoreCase.Equals(persisted.ReleaseArtifactSha256Digest, expected.ReleaseArtifactSha256Digest) ||
            persisted.Kind != expected.Kind || !StringComparer.Ordinal.Equals(persisted.ProposedIntent, expected.ProposedIntent) ||
            !persisted.EvidenceReferences.SequenceEqual(expected.EvidenceReferences, StringComparer.Ordinal) ||
            persisted.CreatedAt != expected.CreatedAt || persisted.IsExternallyEffecting ||
            !persisted.RequiresHumanReview || !persisted.RequiresNewSoftwareDeliveryRun)
            throw new InvalidOperationException("Improvement repository changed governed proposal state.");
    }
}
