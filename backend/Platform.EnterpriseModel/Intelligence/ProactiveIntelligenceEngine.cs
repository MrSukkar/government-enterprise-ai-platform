using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Intelligence;

public sealed class ProactiveIntelligenceEngine(
    IProactiveIntelligenceContextProvider contextProvider,
    IProactiveIntelligenceAnalyzer analyzer,
    TimeProvider timeProvider)
{
    public async Task<ProactiveIntelligenceReport> EvaluateAsync(
        ProactiveIntelligenceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var snapshot = await contextProvider.LoadAuthorizedContextAsync(request, cancellationToken);
        var generatedAt = timeProvider.GetUtcNow();
        ValidateSnapshot(request, snapshot, generatedAt);
        var candidates = await analyzer.AnalyzeAsync(request, snapshot, cancellationToken);
        if (candidates.IsDefault) throw new InvalidOperationException("Intelligence analyzer returned no explicit result.");

        var objects = snapshot.Objects.ToDictionary(item => item.Id);
        var signals = snapshot.Signals.ToDictionary(item => item.SignalId);
        var authorizedEvidence = snapshot.AuthorizationEvidenceReferences
            .Add(snapshot.PolicyVerificationEvidenceReference)
            .AddRange(snapshot.Objects.SelectMany(item => item.EvidenceReferences))
            .AddRange(snapshot.Signals.SelectMany(item => item.EvidenceReferences))
            .ToHashSet(StringComparer.Ordinal);
        var findings = candidates
            .Select(candidate => ValidateAndCreateFinding(candidate, objects, signals, authorizedEvidence, request))
            .OrderBy(item => item.FindingFingerprint, StringComparer.Ordinal)
            .ToImmutableArray();
        if (findings.Select(item => item.FindingFingerprint).Distinct(StringComparer.Ordinal).Count() != findings.Length)
            throw new InvalidOperationException("Intelligence analyzer returned duplicate findings.");

        var evidence = snapshot.AuthorizationEvidenceReferences
            .Add(snapshot.PolicyVerificationEvidenceReference)
            .AddRange(findings.SelectMany(item => item.EvidenceReferences))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        return new ProactiveIntelligenceReport(
            request.RequestId, request.TenantId, request.DetectionPolicy.PolicyId,
            request.DetectionPolicy.Version, request.DetectionPolicy.Sha256Digest,
            findings, evidence, request.WindowStart, request.WindowEnd, generatedAt);
    }

    private static void ValidateSnapshot(
        ProactiveIntelligenceRequest request,
        ProactiveIntelligenceSnapshot snapshot,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.RequestId != request.RequestId ||
            !StringComparer.Ordinal.Equals(snapshot.TenantId, request.TenantId) ||
            !snapshot.PolicySignatureValid ||
            !StringComparer.Ordinal.Equals(snapshot.VerifiedPolicyId, request.DetectionPolicy.PolicyId) ||
            !StringComparer.Ordinal.Equals(snapshot.VerifiedPolicyVersion, request.DetectionPolicy.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(snapshot.VerifiedPolicySha256Digest, request.DetectionPolicy.Sha256Digest))
            throw new UnauthorizedAccessException("Proactive intelligence policy or request verification failed closed.");
        if (snapshot.Objects.IsDefaultOrEmpty || snapshot.Signals.IsDefault ||
            snapshot.AuthorizationEvidenceReferences.IsDefaultOrEmpty || snapshot.CapturedAt < request.WindowEnd ||
            snapshot.CapturedAt > generatedAt)
            throw new InvalidOperationException("Proactive intelligence snapshot is incomplete or has invalid time.");
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.PolicyVerificationEvidenceReference);
        if (snapshot.Objects.Select(item => item.Id).Distinct().Count() != snapshot.Objects.Length ||
            snapshot.Signals.Select(item => item.SignalId).Distinct().Count() != snapshot.Signals.Length)
            throw new InvalidOperationException("Proactive intelligence snapshot contains duplicate identities.");
        var objectIds = snapshot.Objects.Select(item => item.Id).ToHashSet();
        foreach (var enterpriseObject in snapshot.Objects)
        {
            enterpriseObject.Validate();
            if (!StringComparer.Ordinal.Equals(enterpriseObject.TenantId, request.TenantId) ||
                !request.AuthorizedObjectScope.Contains(enterpriseObject.Id) ||
                enterpriseObject.Classification > request.MaximumClassification)
                throw new UnauthorizedAccessException("Intelligence provider exceeded authorized object scope.");
        }
        foreach (var signal in snapshot.Signals)
        {
            signal.Validate();
            if (!objectIds.Contains(signal.ObjectId) || !request.AuthorizedObjectScope.Contains(signal.ObjectId) ||
                signal.Classification > request.MaximumClassification ||
                signal.ObservedAt < request.WindowStart || signal.ObservedAt > request.WindowEnd)
                throw new UnauthorizedAccessException("Intelligence provider returned an unauthorized signal.");
        }
    }

    private static ProactiveFinding ValidateAndCreateFinding(
        ProactiveFindingCandidate candidate,
        IReadOnlyDictionary<EnterpriseObjectId, EnterpriseObject> objects,
        IReadOnlyDictionary<Guid, EnterpriseOperationalSignal> signals,
        HashSet<string> authorizedEvidence,
        ProactiveIntelligenceRequest request)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (!objects.ContainsKey(candidate.ObjectId) || candidate.SignalIds.IsDefaultOrEmpty ||
            candidate.Confidence is < 0 or > 1 || !Enum.IsDefined<ProactiveFindingDisposition>(candidate.Disposition))
            throw new UnauthorizedAccessException("Intelligence finding exceeded authorized context.");
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Rationale);
        if (candidate.Disposition == ProactiveFindingDisposition.RecommendGovernedAction)
            ArgumentException.ThrowIfNullOrWhiteSpace(candidate.RecommendedActionName);
        else if (candidate.RecommendedActionName is not null)
            throw new InvalidOperationException("Only governed-action recommendations may name an action.");
        if (candidate.SignalIds.Distinct().Count() != candidate.SignalIds.Length ||
            candidate.SignalIds.Any(id => !signals.TryGetValue(id, out var signal) || signal.ObjectId != candidate.ObjectId))
            throw new UnauthorizedAccessException("Finding signals do not match the governed enterprise object.");
        if (candidate.EvidenceReferences.IsDefaultOrEmpty ||
            candidate.EvidenceReferences.Any(reference => !authorizedEvidence.Contains(reference)))
            throw new UnauthorizedAccessException("Finding cited evidence outside authorized context.");

        var fingerprintInput = string.Join('|',
            request.DetectionPolicy.PolicyId, request.DetectionPolicy.Version, candidate.ObjectId,
            candidate.Disposition, candidate.RecommendedActionName ?? string.Empty,
            string.Join(',', candidate.SignalIds.Order()));
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintInput))).ToLowerInvariant();
        return new ProactiveFinding(
            fingerprint, candidate.ObjectId, candidate.SignalIds.Order().ToImmutableArray(), candidate.Disposition,
            candidate.Title, candidate.Rationale, candidate.RecommendedActionName, candidate.Confidence,
            candidate.EvidenceReferences.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray());
    }
}
