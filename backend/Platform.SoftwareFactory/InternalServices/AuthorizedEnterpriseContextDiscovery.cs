using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.Knowledge.Retrieval;

namespace Platform.SoftwareFactory.InternalService;

public sealed record AuthorizedEnterpriseContextDiscoveryRequest(
    Guid DiscoveryId,
    Guid RegistrationId,
    long ExpectedRegistrationVersion,
    string ExpectedIntentSha256Digest,
    GovernedIdentity Identity,
    string Purpose,
    DataClassification RegistrationClassification,
    string AuthorizationEvidenceReference,
    string Environment,
    IntentPolicyBundleReference PolicyBundle,
    DateTimeOffset RequestedAt)
{
    public AuthorizedEnterpriseContextDiscoveryRequest Validate()
    {
        if (DiscoveryId == Guid.Empty)
            throw new InvalidOperationException("Enterprise Context discovery identity is required.");
        if (RegistrationId == Guid.Empty)
            throw new InvalidOperationException("Governed intent registration identity is required.");
        if (ExpectedRegistrationVersion < 0)
            throw new InvalidOperationException("A persisted intent registration version is required.");
        ValidateSha256(ExpectedIntentSha256Digest, "Expected intent");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated)
            throw new UnauthorizedAccessException("Enterprise Context discovery requires an authenticated identity.");
        if (!Identity.Permissions.Contains("developer.internal-service.context.discover"))
            throw new UnauthorizedAccessException("The developer.internal-service.context.discover permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Identity.SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Identity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(RegistrationClassification) || Identity.Clearance < RegistrationClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Enterprise Context discovery.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment))
            throw new InvalidOperationException("Enterprise Context policy environment does not match the request.");
        if (RequestedAt == default || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Enterprise Context discovery time is invalid for the active policy bundle.");
        return this;
    }

    private static void ValidateSha256(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException($"{field} requires a SHA-256 digest.");
    }
}

public interface IGovernedIntentRegistrationReader
{
    Task<RegisteredGovernedIntent?> LoadAsync(
        Guid registrationId,
        string tenantId,
        CancellationToken cancellationToken);
}

public sealed record EnterpriseContextPolicyInput(
    Guid DecisionRequestId,
    Guid DiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string SubjectId,
    string Purpose,
    DataClassification RegistrationClassification,
    string Environment,
    string Action,
    string IntentSha256Digest,
    IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record EnterpriseContextPolicyDecision(
    Guid DecisionRequestId,
    Guid DiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string BundleId,
    string BundleVersion,
    string BundleSha256Digest,
    string Environment,
    bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome,
    DataClassification MaximumClassification,
    ImmutableHashSet<string> AllowedResourceIds,
    ImmutableHashSet<RetrievalModality> AllowedModalities,
    ImmutableHashSet<string> RequiredRoles,
    int MaximumResults,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IEnterpriseContextPolicyGate
{
    Task<EnterpriseContextPolicyDecision> EvaluateAsync(
        EnterpriseContextPolicyInput input,
        CancellationToken cancellationToken);
}

public sealed record AuthorizedEnterpriseContextItem(
    string ResourceId,
    DataClassification Classification,
    string Content,
    RetrievalModality Modality,
    decimal Relevance,
    string Source,
    ImmutableArray<string> EvidenceReferences);

public sealed record EnterpriseContextEvidenceRecord(
    Guid DiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string SubjectId,
    string Purpose,
    string IntentSha256Digest,
    Guid PolicyDecisionRequestId,
    string PolicyBundleId,
    string PolicyBundleVersion,
    string PolicyBundleSha256Digest,
    string ContextSha256Digest,
    ImmutableArray<AuthorizedEnterpriseContextItem> Items,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DiscoveredAt);

public sealed record EnterpriseContextEvidenceReceipt(
    Guid DiscoveryId,
    Guid RegistrationId,
    string TenantId,
    string ContextSha256Digest,
    string EvidenceReference,
    DateTimeOffset RecordedAt);

public interface IEnterpriseContextEvidenceRecorder
{
    Task<EnterpriseContextEvidenceReceipt> RecordAsync(
        EnterpriseContextEvidenceRecord record,
        CancellationToken cancellationToken);
}

public sealed class EnterpriseContextDependencyUnavailableException(string message) : Exception(message);

public sealed record AuthorizedEnterpriseContextDiscoveryReceipt(
    Guid DiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string IntentSha256Digest,
    GovernedIntentPolicyOutcome PolicyOutcome,
    bool IsContextReleased,
    bool CanAdvance,
    string? ContextSha256Digest,
    ImmutableArray<AuthorizedEnterpriseContextItem> Items,
    string? DiscoveryEvidenceReference,
    ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate,
    DateTimeOffset CompletedAt);

public sealed class AuthorizedEnterpriseContextDiscoveryEngine(
    AuthorizedKnowledgeRetriever knowledgeRetriever,
    IEnumerable<IKnowledgeRetrievalSource> retrievalSources)
{
    private readonly AuthorizedKnowledgeRetriever _knowledgeRetriever = knowledgeRetriever;
    private readonly ImmutableHashSet<RetrievalModality> _availableRetrievalModalities = retrievalSources
        .Select(source => source.Modality)
        .ToImmutableHashSet();

    public async Task<AuthorizedEnterpriseContextDiscoveryReceipt> DiscoverAsync(
        AuthorizedEnterpriseContextDiscoveryRequest request,
        IGovernedIntentRegistrationReader registrationReader,
        IEnterpriseContextPolicyGate policyGate,
        IEnterpriseContextEvidenceRecorder evidenceRecorder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(registrationReader);
        ArgumentNullException.ThrowIfNull(policyGate);
        ArgumentNullException.ThrowIfNull(evidenceRecorder);
        request.Validate();

        var registered = await registrationReader.LoadAsync(
            request.RegistrationId,
            request.Identity.TenantId,
            cancellationToken) ?? throw new KeyNotFoundException("Governed intent registration was not found.");
        ValidateRegistration(request, registered);

        var decisionRequestId = Guid.NewGuid();
        var policyInput = new EnterpriseContextPolicyInput(
            decisionRequestId,
            request.DiscoveryId,
            registered.RegistrationId,
            registered.Version,
            registered.TenantId,
            request.Identity.SubjectId,
            registered.Purpose,
            registered.Classification,
            request.Environment,
            "internal-service.enterprise-context.discover",
            registered.IntentSha256Digest,
            request.PolicyBundle,
            registered.EvidenceReferences
                .Append(registered.RegistrationEvidenceReference)
                .Append(request.AuthorizationEvidenceReference)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToImmutableArray(),
            request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(policyInput, cancellationToken);
        ValidateDecision(policyInput, request.Identity, decision);

        var decisionEvidence = policyInput.EvidenceReferences
            .Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new AuthorizedEnterpriseContextDiscoveryReceipt(
                request.DiscoveryId,
                registered.RegistrationId,
                registered.Version,
                registered.TenantId,
                registered.IntentSha256Digest,
                decision.Outcome,
                IsContextReleased: false,
                CanAdvance: false,
                ContextSha256Digest: null,
                Items: [],
                DiscoveryEvidenceReference: null,
                decisionEvidence,
                "Policy denial requires a new authorized Enterprise Context request",
                decision.DecidedAt);

        if (!decision.AllowedModalities.IsSubsetOf(_availableRetrievalModalities))
            throw new EnterpriseContextDependencyUnavailableException(
                "One or more OPA-authorized Enterprise Context retrieval modalities are unavailable.");

        var query = new KnowledgeQuery(
            request.Identity,
            registered.Purpose,
            BuildQueryText(registered),
            registered.TenantId,
            decision.MaximumClassification,
            decision.AllowedResourceIds,
            decision.RequiredRoles,
            decision.AllowedModalities,
            decision.MaximumResults);
        var context = await _knowledgeRetriever.RetrieveAsync(query, cancellationToken);
        var items = ValidateAndMapContext(registered, decision, context);
        var contextDigest = Digest(registered, decision, items);
        var contextEvidence = decisionEvidence
            .Concat(items.SelectMany(item => item.EvidenceReferences))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var evidenceRecord = new EnterpriseContextEvidenceRecord(
            request.DiscoveryId,
            registered.RegistrationId,
            registered.Version,
            registered.TenantId,
            request.Identity.SubjectId,
            registered.Purpose,
            registered.IntentSha256Digest,
            decision.DecisionRequestId,
            decision.BundleId,
            decision.BundleVersion,
            decision.BundleSha256Digest,
            contextDigest,
            items,
            contextEvidence,
            decision.DecidedAt);
        var evidenceReceipt = await evidenceRecorder.RecordAsync(evidenceRecord, cancellationToken);
        ValidateEvidenceReceipt(evidenceRecord, evidenceReceipt);

        return new AuthorizedEnterpriseContextDiscoveryReceipt(
            request.DiscoveryId,
            registered.RegistrationId,
            registered.Version,
            registered.TenantId,
            registered.IntentSha256Digest,
            decision.Outcome,
            IsContextReleased: true,
            CanAdvance: false,
            contextDigest,
            items,
            evidenceReceipt.EvidenceReference,
            contextEvidence
                .Append(evidenceReceipt.EvidenceReference)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToImmutableArray(),
            "Separately approved Existing Systems discovery",
            evidenceReceipt.RecordedAt);
    }

    private static void ValidateRegistration(
        AuthorizedEnterpriseContextDiscoveryRequest request,
        RegisteredGovernedIntent registered)
    {
        ArgumentNullException.ThrowIfNull(registered);
        if (registered.RegistrationId != request.RegistrationId ||
            !StringComparer.Ordinal.Equals(registered.TenantId, request.Identity.TenantId) ||
            registered.Version != request.ExpectedRegistrationVersion ||
            !StringComparer.OrdinalIgnoreCase.Equals(
                registered.IntentSha256Digest,
                request.ExpectedIntentSha256Digest) ||
            !StringComparer.Ordinal.Equals(registered.Purpose, request.Purpose) ||
            registered.Classification != request.RegistrationClassification ||
            string.IsNullOrWhiteSpace(registered.RegistrationEvidenceReference) ||
            StringComparer.Ordinal.Equals(registered.RegistrationEvidenceReference, "pending-atomic-registration") ||
            registered.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Registered governed intent does not match the authorized discovery request.");
        foreach (var evidenceReference in registered.EvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
    }

    private static void ValidateDecision(
        EnterpriseContextPolicyInput input,
        GovernedIdentity identity,
        EnterpriseContextPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(decision.AllowedResourceIds);
        ArgumentNullException.ThrowIfNull(decision.AllowedModalities);
        ArgumentNullException.ThrowIfNull(decision.RequiredRoles);
        if (decision.DecisionRequestId != input.DecisionRequestId ||
            decision.DiscoveryId != input.DiscoveryId ||
            decision.RegistrationId != input.RegistrationId ||
            decision.RegistrationVersion != input.RegistrationVersion ||
            !StringComparer.Ordinal.Equals(decision.TenantId, input.TenantId) ||
            !StringComparer.Ordinal.Equals(decision.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(decision.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.BundleSha256Digest, input.PolicyBundle.Sha256Digest) ||
            !StringComparer.Ordinal.Equals(decision.Environment, input.Environment) ||
            !decision.PolicySignatureValid ||
            !Enum.IsDefined(decision.Outcome) ||
            !Enum.IsDefined(decision.MaximumClassification) ||
            decision.MaximumClassification > identity.Clearance)
            throw new UnauthorizedAccessException("OPA returned a mismatched Enterprise Context decision; discovery denied fail closed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(decision.PolicyVerificationEvidenceReference);
        if (decision.Reasons.IsDefaultOrEmpty || decision.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("OPA Enterprise Context decisions require reasons and evidence.");
        foreach (var reason in decision.Reasons) ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        foreach (var evidenceReference in decision.EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        if (decision.DecidedAt < input.EvaluatedAt)
            throw new InvalidOperationException("OPA Enterprise Context decision predates evaluation.");
        if (decision.Outcome == GovernedIntentPolicyOutcome.Permit &&
            (decision.AllowedResourceIds.IsEmpty || decision.AllowedModalities.IsEmpty || decision.MaximumResults <= 0))
            throw new UnauthorizedAccessException("OPA permit did not establish an explicit Enterprise Context retrieval scope.");
        if (decision.AllowedResourceIds.Any(string.IsNullOrWhiteSpace) ||
            decision.AllowedModalities.Any(modality => !Enum.IsDefined(modality)) ||
            decision.RequiredRoles.Any(string.IsNullOrWhiteSpace))
            throw new UnauthorizedAccessException("OPA returned an invalid Enterprise Context retrieval scope.");
    }

    private static ImmutableArray<AuthorizedEnterpriseContextItem> ValidateAndMapContext(
        RegisteredGovernedIntent registered,
        EnterpriseContextPolicyDecision decision,
        AuthorizedKnowledgeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!StringComparer.Ordinal.Equals(context.TenantId, registered.TenantId) ||
            !StringComparer.Ordinal.Equals(context.Purpose, registered.Purpose) ||
            context.Candidates.IsDefault || context.Candidates.Length > decision.MaximumResults)
            throw new InvalidOperationException("Knowledge retrieval returned a mismatched Enterprise Context boundary.");

        var items = context.Candidates.Select(candidate =>
        {
            if (!decision.AllowedResourceIds.Contains(candidate.ResourceId) ||
                string.IsNullOrWhiteSpace(candidate.ResourceId) ||
                !StringComparer.Ordinal.Equals(candidate.TenantId, registered.TenantId) ||
                candidate.Classification > decision.MaximumClassification ||
                !decision.AllowedModalities.Contains(candidate.Modality) ||
                candidate.Relevance is < 0 or > 1 ||
                string.IsNullOrWhiteSpace(candidate.Content) ||
                string.IsNullOrWhiteSpace(candidate.Source) ||
                candidate.EvidenceReferences.IsDefaultOrEmpty)
                throw new InvalidOperationException("Knowledge retrieval released an out-of-scope Enterprise Context candidate.");
            foreach (var evidenceReference in candidate.EvidenceReferences)
                ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
            return new AuthorizedEnterpriseContextItem(
                candidate.ResourceId,
                candidate.Classification,
                candidate.Content,
                candidate.Modality,
                candidate.Relevance,
                candidate.Source,
                candidate.EvidenceReferences
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToImmutableArray());
        }).ToImmutableArray();

        if (items.Select(item => $"{item.ResourceId}\u001f{item.Modality}\u001f{item.Source}")
            .Distinct(StringComparer.Ordinal).Count() != items.Length)
            throw new InvalidOperationException("Enterprise Context contains duplicate authorized candidates.");
        return items;
    }

    private static void ValidateEvidenceReceipt(
        EnterpriseContextEvidenceRecord record,
        EnterpriseContextEvidenceReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (receipt.DiscoveryId != record.DiscoveryId ||
            receipt.RegistrationId != record.RegistrationId ||
            !StringComparer.Ordinal.Equals(receipt.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.ContextSha256Digest, record.ContextSha256Digest) ||
            string.IsNullOrWhiteSpace(receipt.EvidenceReference) ||
            receipt.EvidenceReference.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
            receipt.EvidenceReference.Contains("pending", StringComparison.OrdinalIgnoreCase) ||
            receipt.RecordedAt < record.DiscoveredAt)
            throw new InvalidOperationException("Enterprise Context evidence recorder returned a mismatched receipt.");
    }

    private static string BuildQueryText(RegisteredGovernedIntent registered) =>
        string.Join('\n', registered.ServiceName, registered.Mission, registered.PrimaryUsers);

    private static string Digest(
        RegisteredGovernedIntent registered,
        EnterpriseContextPolicyDecision decision,
        ImmutableArray<AuthorizedEnterpriseContextItem> items)
    {
        var canonicalItems = items.Select(item => string.Join('\u001e',
            item.ResourceId,
            item.Classification,
            item.Modality,
            item.Relevance.ToString(CultureInfo.InvariantCulture),
            item.Source,
            item.Content,
            string.Join('\u001d', item.EvidenceReferences)));
        var canonical = string.Join('\u001f',
            registered.RegistrationId.ToString("D"),
            registered.Version.ToString(CultureInfo.InvariantCulture),
            registered.TenantId,
            registered.IntentSha256Digest.ToLowerInvariant(),
            decision.DecisionRequestId.ToString("D"),
            decision.BundleId,
            decision.BundleVersion,
            decision.BundleSha256Digest.ToLowerInvariant(),
            string.Join('\u001c', canonicalItems));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
