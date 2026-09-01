using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Identity.Access;
using Platform.Integrations.ExistingArchitecture;

namespace Platform.SoftwareFactory.InternalService;

public sealed record AuthorizedExistingArchitectureDiscoveryRequest(
    Guid DiscoveryId,
    Guid SystemsDiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long ExpectedRegistrationVersion,
    string ExpectedIntentSha256Digest,
    string ExpectedContextSha256Digest,
    string ExpectedInventorySha256Digest,
    GovernedIdentity Identity,
    string Purpose,
    DataClassification MaximumClassification,
    string AuthorizationEvidenceReference,
    string Environment,
    IntentPolicyBundleReference PolicyBundle,
    DateTimeOffset RequestedAt)
{
    public AuthorizedExistingArchitectureDiscoveryRequest Validate()
    {
        if (DiscoveryId == Guid.Empty) throw new InvalidOperationException("Existing Architecture discovery identity is required.");
        if (SystemsDiscoveryId == Guid.Empty) throw new InvalidOperationException("Existing Systems discovery identity is required.");
        if (ContextDiscoveryId == Guid.Empty) throw new InvalidOperationException("Enterprise Context discovery identity is required.");
        if (RegistrationId == Guid.Empty) throw new InvalidOperationException("Governed intent registration identity is required.");
        if (ExpectedRegistrationVersion < 0) throw new InvalidOperationException("A persisted registration version is required.");
        ValidateSha256(ExpectedIntentSha256Digest, "Expected intent");
        ValidateSha256(ExpectedContextSha256Digest, "Expected Enterprise Context");
        ValidateSha256(ExpectedInventorySha256Digest, "Expected Existing Systems inventory");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated) throw new UnauthorizedAccessException("Existing Architecture discovery requires authentication.");
        if (!Identity.Permissions.Contains("developer.internal-service.architecture.discover"))
            throw new UnauthorizedAccessException("The developer.internal-service.architecture.discover permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Identity.SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Identity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Existing Architecture discovery.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment))
            throw new InvalidOperationException("Existing Architecture policy environment does not match the request.");
        if (RequestedAt == default || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Existing Architecture discovery time is invalid for the active policy bundle.");
        return this;
    }

    internal static void ValidateSha256(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException($"{field} requires a SHA-256 digest.");
    }
}

public interface IAuthorizedExistingSystemsSnapshotReader
{
    Task<AuthorizedExistingSystemsDiscoveryReceipt?> LoadAsync(
        Guid systemsDiscoveryId,
        string tenantId,
        CancellationToken cancellationToken);
}

public sealed record ExistingArchitecturePolicyInput(
    Guid DecisionRequestId,
    Guid DiscoveryId,
    Guid SystemsDiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string SubjectId,
    string Purpose,
    DataClassification MaximumClassification,
    string Environment,
    string Action,
    string IntentSha256Digest,
    string ContextSha256Digest,
    string InventorySha256Digest,
    IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record ExistingArchitecturePolicyDecision(
    Guid DecisionRequestId,
    Guid DiscoveryId,
    Guid SystemsDiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string IntentSha256Digest,
    string ContextSha256Digest,
    string InventorySha256Digest,
    string BundleId,
    string BundleVersion,
    string BundleSha256Digest,
    string Environment,
    bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference,
    GovernedIntentPolicyOutcome Outcome,
    DataClassification MaximumClassification,
    ImmutableHashSet<EnterpriseObjectId> AllowedSystemIds,
    ImmutableHashSet<ExistingArchitectureItemKind> AllowedItemKinds,
    ImmutableHashSet<string> AllowedRelationshipTypes,
    ImmutableHashSet<string> AllowedSourceKinds,
    int MaximumResults,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IExistingArchitecturePolicyGate
{
    Task<ExistingArchitecturePolicyDecision> EvaluateAsync(
        ExistingArchitecturePolicyInput input,
        CancellationToken cancellationToken);
}

public sealed record ExistingArchitectureConformanceScopeRequest(
    Guid DiscoveryId,
    string TenantId,
    string ArchitectureAuthorityReference,
    ImmutableHashSet<EnterpriseObjectId> AllowedSystemIds,
    ImmutableHashSet<ExistingArchitectureItemKind> AllowedItemKinds,
    ImmutableHashSet<string> AllowedRelationshipTypes,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record ExistingArchitectureItemConformanceRequest(
    Guid DiscoveryId,
    string TenantId,
    ExistingArchitectureCandidate Candidate,
    string ArchitectureAuthorityReference,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record ExistingArchitectureConformanceDecision(
    Guid DiscoveryId,
    Guid? ArchitectureItemId,
    string TenantId,
    bool IsConformant,
    string Code,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IExistingArchitectureConformanceValidator
{
    Task<ExistingArchitectureConformanceDecision> ValidateScopeAsync(
        ExistingArchitectureConformanceScopeRequest request,
        CancellationToken cancellationToken);

    Task<ExistingArchitectureConformanceDecision> ValidateItemAsync(
        ExistingArchitectureItemConformanceRequest request,
        CancellationToken cancellationToken);
}

public sealed record ExistingArchitectureResultAuthorizationRequest(
    Guid AuthorizationRequestId,
    Guid DiscoveryId,
    string TenantId,
    string SubjectId,
    string Purpose,
    string Action,
    Guid ArchitectureItemId,
    EnterpriseObjectId SystemId,
    EnterpriseObjectId? RelatedSystemId,
    ExistingArchitectureItemKind Kind,
    string? RelationshipType,
    DataClassification Classification,
    string SourceKind,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record ExistingArchitectureResultAuthorizationDecision(
    Guid AuthorizationRequestId,
    Guid DiscoveryId,
    Guid ArchitectureItemId,
    string TenantId,
    string Action,
    bool IsAllowed,
    string Code,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IExistingArchitectureResultAuthorizer
{
    Task<ExistingArchitectureResultAuthorizationDecision> AuthorizeAsync(
        ExistingArchitectureResultAuthorizationRequest request,
        CancellationToken cancellationToken);
}

public sealed record AuthorizedExistingArchitectureItem(
    Guid ArchitectureItemId,
    Guid SystemId,
    Guid? RelatedSystemId,
    ExistingArchitectureItemKind Kind,
    string Name,
    string Description,
    string? RelationshipType,
    string Version,
    DataClassification Classification,
    string Environment,
    LifecycleState Lifecycle,
    string SourceKind,
    ImmutableArray<string> EvidenceReferences,
    ImmutableArray<string> ConformanceEvidenceReferences,
    ImmutableArray<string> AuthorizationEvidenceReferences,
    DateTimeOffset ApprovedAt,
    DateTimeOffset UpdatedAt);

public sealed record ExistingArchitectureEvidenceRecord(
    Guid DiscoveryId,
    Guid SystemsDiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string SubjectId,
    string Purpose,
    string IntentSha256Digest,
    string ContextSha256Digest,
    string InventorySha256Digest,
    Guid PolicyDecisionRequestId,
    string PolicyBundleId,
    string PolicyBundleVersion,
    string PolicyBundleSha256Digest,
    string ArchitectureSha256Digest,
    ImmutableArray<AuthorizedExistingArchitectureItem> Items,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DiscoveredAt);

public sealed record ExistingArchitectureEvidenceReceipt(
    Guid DiscoveryId,
    Guid SystemsDiscoveryId,
    Guid RegistrationId,
    string TenantId,
    string ArchitectureSha256Digest,
    string EvidenceReference,
    DateTimeOffset RecordedAt);

public interface IExistingArchitectureEvidenceRecorder
{
    Task<ExistingArchitectureEvidenceReceipt> RecordAsync(
        ExistingArchitectureEvidenceRecord record,
        CancellationToken cancellationToken);
}

public sealed class ExistingArchitectureDependencyUnavailableException(string message) : Exception(message);

public sealed record AuthorizedExistingArchitectureDiscoveryReceipt(
    Guid DiscoveryId,
    Guid SystemsDiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string IntentSha256Digest,
    string ContextSha256Digest,
    string InventorySha256Digest,
    GovernedIntentPolicyOutcome PolicyOutcome,
    bool IsArchitectureReleased,
    bool CanAdvance,
    string? ArchitectureSha256Digest,
    ImmutableArray<AuthorizedExistingArchitectureItem> Items,
    string? DiscoveryEvidenceReference,
    ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate,
    DateTimeOffset CompletedAt);

public sealed class AuthorizedExistingArchitectureDiscoveryEngine(
    IEnumerable<IExistingArchitectureSource> architectureSources)
{
    private const string ArchitectureAuthority = "docs/PROJECT_MASTER_SPECIFICATION_V2.md";
    private readonly IReadOnlyCollection<IExistingArchitectureSource> _architectureSources = architectureSources.ToArray();

    public async Task<AuthorizedExistingArchitectureDiscoveryReceipt> DiscoverAsync(
        AuthorizedExistingArchitectureDiscoveryRequest request,
        IAuthorizedExistingSystemsSnapshotReader systemsReader,
        IExistingArchitecturePolicyGate policyGate,
        IExistingArchitectureConformanceValidator conformanceValidator,
        IExistingArchitectureResultAuthorizer resultAuthorizer,
        IExistingArchitectureEvidenceRecorder evidenceRecorder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(systemsReader);
        ArgumentNullException.ThrowIfNull(policyGate);
        ArgumentNullException.ThrowIfNull(conformanceValidator);
        ArgumentNullException.ThrowIfNull(resultAuthorizer);
        ArgumentNullException.ThrowIfNull(evidenceRecorder);
        request.Validate();

        var systems = await systemsReader.LoadAsync(
            request.SystemsDiscoveryId,
            request.Identity.TenantId,
            cancellationToken) ?? throw new KeyNotFoundException("Authorized Existing Systems snapshot was not found.");
        ValidateSystemsSnapshot(request, systems);

        var prerequisiteEvidence = systems.EvidenceReferences
            .Append(systems.DiscoveryEvidenceReference!)
            .Append(request.AuthorizationEvidenceReference)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var policyInput = new ExistingArchitecturePolicyInput(
            Guid.NewGuid(), request.DiscoveryId, systems.DiscoveryId, systems.ContextDiscoveryId,
            systems.RegistrationId, systems.RegistrationVersion, systems.TenantId,
            request.Identity.SubjectId, request.Purpose, request.MaximumClassification,
            request.Environment, "internal-service.existing-architecture.discover",
            systems.IntentSha256Digest, systems.ContextSha256Digest, systems.InventorySha256Digest!,
            request.PolicyBundle, prerequisiteEvidence, request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(policyInput, cancellationToken);
        ValidateDecision(policyInput, request.Identity, decision);

        var decisionEvidence = prerequisiteEvidence
            .Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return Denied(request, systems, decision, decisionEvidence);

        var availableSystemIds = systems.Systems
            .Select(system => new EnterpriseObjectId(system.SystemId))
            .ToImmutableHashSet();
        if (!decision.AllowedSystemIds.IsSubsetOf(availableSystemIds))
            throw new UnauthorizedAccessException("OPA authorized a system outside the Existing Systems snapshot.");

        var scopeConformanceRequest = new ExistingArchitectureConformanceScopeRequest(
            request.DiscoveryId, systems.TenantId, ArchitectureAuthority,
            decision.AllowedSystemIds, decision.AllowedItemKinds,
            decision.AllowedRelationshipTypes, decisionEvidence, decision.DecidedAt);
        var scopeConformance = await conformanceValidator.ValidateScopeAsync(
            scopeConformanceRequest, cancellationToken);
        ValidateConformance(scopeConformanceRequest.DiscoveryId, null, systems.TenantId, scopeConformance);

        var selectedSources = SelectAuthorizedSources(decision);
        var scope = new ExistingArchitectureSourceScope(
            systems.TenantId, request.Purpose, request.Environment,
            decision.MaximumClassification, decision.AllowedSystemIds,
            decision.AllowedItemKinds, decision.AllowedRelationshipTypes,
            decision.AllowedSourceKinds, decision.MaximumResults);
        var sourceResults = await Task.WhenAll(selectedSources.Select(async source =>
            new ArchitectureSourceResult(
                source.SourceKind,
                await source.DiscoverAsync(scope, cancellationToken)
                    ?? throw new InvalidOperationException("Existing Architecture source returned no result collection."))));
        var candidates = sourceResults
            .SelectMany(result => result.Candidates.Select(candidate => (result.SourceKind, Candidate: candidate)))
            .ToArray();
        if (candidates.Length > decision.MaximumResults)
            throw new InvalidOperationException("Existing Architecture sources exceeded the policy-authorized result count.");

        var items = ImmutableArray.CreateBuilder<AuthorizedExistingArchitectureItem>(candidates.Length);
        var seenItemIds = new HashSet<Guid>();
        foreach (var (sourceKind, candidate) in candidates.OrderBy(value => value.Candidate.ArchitectureItemId))
        {
            ValidateCandidate(request, decision, availableSystemIds, sourceKind, candidate);
            if (!seenItemIds.Add(candidate.ArchitectureItemId))
                throw new InvalidOperationException("Existing Architecture sources returned a duplicate item.");

            var itemConformanceRequest = new ExistingArchitectureItemConformanceRequest(
                request.DiscoveryId, systems.TenantId, candidate, ArchitectureAuthority,
                decisionEvidence, decision.DecidedAt);
            var itemConformance = await conformanceValidator.ValidateItemAsync(
                itemConformanceRequest, cancellationToken);
            ValidateConformance(
                request.DiscoveryId, candidate.ArchitectureItemId, systems.TenantId, itemConformance);
            items.Add(await AuthorizeAndMapAsync(
                request, decision, candidate, itemConformance, resultAuthorizer, cancellationToken));
        }

        var orderedItems = items.OrderBy(item => item.ArchitectureItemId).ToImmutableArray();
        var architectureDigest = Digest(systems, decision, orderedItems);
        var architectureEvidence = decisionEvidence
            .Concat(scopeConformance.EvidenceReferences)
            .Concat(orderedItems.SelectMany(item => item.EvidenceReferences))
            .Concat(orderedItems.SelectMany(item => item.ConformanceEvidenceReferences))
            .Concat(orderedItems.SelectMany(item => item.AuthorizationEvidenceReferences))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var evidenceRecord = new ExistingArchitectureEvidenceRecord(
            request.DiscoveryId, systems.DiscoveryId, systems.ContextDiscoveryId,
            systems.RegistrationId, systems.RegistrationVersion, systems.TenantId,
            request.Identity.SubjectId, request.Purpose, systems.IntentSha256Digest,
            systems.ContextSha256Digest, systems.InventorySha256Digest!, decision.DecisionRequestId,
            decision.BundleId, decision.BundleVersion, decision.BundleSha256Digest,
            architectureDigest, orderedItems, architectureEvidence, decision.DecidedAt);
        var evidenceReceipt = await evidenceRecorder.RecordAsync(evidenceRecord, cancellationToken);
        ValidateEvidenceReceipt(evidenceRecord, evidenceReceipt);

        return new AuthorizedExistingArchitectureDiscoveryReceipt(
            request.DiscoveryId, systems.DiscoveryId, systems.ContextDiscoveryId,
            systems.RegistrationId, systems.RegistrationVersion, systems.TenantId,
            systems.IntentSha256Digest, systems.ContextSha256Digest, systems.InventorySha256Digest!,
            decision.Outcome, IsArchitectureReleased: true, CanAdvance: false,
            architectureDigest, orderedItems, evidenceReceipt.EvidenceReference,
            architectureEvidence.Append(evidenceReceipt.EvidenceReference)
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
            "Separately approved Approved Packages selection", evidenceReceipt.RecordedAt);
    }

    private static AuthorizedExistingArchitectureDiscoveryReceipt Denied(
        AuthorizedExistingArchitectureDiscoveryRequest request,
        AuthorizedExistingSystemsDiscoveryReceipt systems,
        ExistingArchitecturePolicyDecision decision,
        ImmutableArray<string> evidence) => new(
            request.DiscoveryId, systems.DiscoveryId, systems.ContextDiscoveryId,
            systems.RegistrationId, systems.RegistrationVersion, systems.TenantId,
            systems.IntentSha256Digest, systems.ContextSha256Digest, systems.InventorySha256Digest!,
            decision.Outcome, IsArchitectureReleased: false, CanAdvance: false,
            ArchitectureSha256Digest: null, Items: [], DiscoveryEvidenceReference: null,
            evidence, "Policy denial requires a new authorized Existing Architecture request", decision.DecidedAt);

    private IReadOnlyCollection<IExistingArchitectureSource> SelectAuthorizedSources(
        ExistingArchitecturePolicyDecision decision)
    {
        if (_architectureSources.Any(source => string.IsNullOrWhiteSpace(source.SourceKind)) ||
            _architectureSources.GroupBy(source => source.SourceKind, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new ExistingArchitectureDependencyUnavailableException(
                "Existing Architecture source registration is invalid.");
        var availableKinds = _architectureSources.Select(source => source.SourceKind)
            .ToImmutableHashSet(StringComparer.Ordinal);
        if (!decision.AllowedSourceKinds.IsSubsetOf(availableKinds))
            throw new ExistingArchitectureDependencyUnavailableException(
                "One or more OPA-authorized Existing Architecture source kinds are unavailable.");
        return _architectureSources.Where(source => decision.AllowedSourceKinds.Contains(source.SourceKind)).ToArray();
    }

    private static void ValidateSystemsSnapshot(
        AuthorizedExistingArchitectureDiscoveryRequest request,
        AuthorizedExistingSystemsDiscoveryReceipt systems)
    {
        if (systems.DiscoveryId != request.SystemsDiscoveryId ||
            systems.ContextDiscoveryId != request.ContextDiscoveryId ||
            systems.RegistrationId != request.RegistrationId ||
            systems.RegistrationVersion != request.ExpectedRegistrationVersion ||
            !StringComparer.Ordinal.Equals(systems.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(systems.IntentSha256Digest, request.ExpectedIntentSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(systems.ContextSha256Digest, request.ExpectedContextSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(systems.InventorySha256Digest, request.ExpectedInventorySha256Digest))
            throw new InvalidOperationException("Authorized Existing Systems snapshot does not match the Existing Architecture request.");
        if (systems.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !systems.IsInventoryReleased ||
            systems.CanAdvance || systems.Systems.IsDefaultOrEmpty || string.IsNullOrWhiteSpace(systems.InventorySha256Digest) ||
            string.IsNullOrWhiteSpace(systems.DiscoveryEvidenceReference))
            throw new UnauthorizedAccessException("Existing Systems snapshot is not eligible for architecture discovery.");
        AuthorizedExistingArchitectureDiscoveryRequest.ValidateSha256(systems.IntentSha256Digest, "Existing Systems intent");
        AuthorizedExistingArchitectureDiscoveryRequest.ValidateSha256(systems.ContextSha256Digest, "Existing Systems context");
        AuthorizedExistingArchitectureDiscoveryRequest.ValidateSha256(systems.InventorySha256Digest, "Existing Systems inventory");
        ValidateEvidence(systems.EvidenceReferences, "Existing Systems snapshot");
        if (systems.Systems.Any(system => system.SystemId == Guid.Empty) ||
            systems.Systems.Select(system => system.SystemId).Distinct().Count() != systems.Systems.Length)
            throw new InvalidOperationException("Existing Systems snapshot contains invalid or duplicate system identities.");
    }

    private static void ValidateDecision(
        ExistingArchitecturePolicyInput input,
        GovernedIdentity identity,
        ExistingArchitecturePolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.DecisionRequestId != input.DecisionRequestId || decision.DiscoveryId != input.DiscoveryId ||
            decision.SystemsDiscoveryId != input.SystemsDiscoveryId || decision.ContextDiscoveryId != input.ContextDiscoveryId ||
            decision.RegistrationId != input.RegistrationId || decision.RegistrationVersion != input.RegistrationVersion ||
            !StringComparer.Ordinal.Equals(decision.TenantId, input.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.IntentSha256Digest, input.IntentSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.ContextSha256Digest, input.ContextSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.InventorySha256Digest, input.InventorySha256Digest) ||
            !StringComparer.Ordinal.Equals(decision.Environment, input.Environment) ||
            !StringComparer.Ordinal.Equals(decision.BundleId, input.PolicyBundle.BundleId) ||
            !StringComparer.Ordinal.Equals(decision.BundleVersion, input.PolicyBundle.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(decision.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Existing Architecture decision; discovery denied fail closed.");
        if (!decision.PolicySignatureValid || string.IsNullOrWhiteSpace(decision.PolicyVerificationEvidenceReference))
            throw new UnauthorizedAccessException("Existing Architecture policy signature verification is required.");
        if (decision.MaximumClassification > input.MaximumClassification || decision.MaximumClassification > identity.Clearance)
            throw new UnauthorizedAccessException("OPA exceeded the requested Existing Architecture classification scope.");
        if (decision.Reasons.IsDefaultOrEmpty || decision.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("OPA Existing Architecture decisions require reasons and evidence.");
        ValidateEvidence(decision.EvidenceReferences, "OPA Existing Architecture decision");
        if (decision.DecidedAt < input.EvaluatedAt)
            throw new InvalidOperationException("OPA Existing Architecture decision predates evaluation.");
        if (decision.Outcome == GovernedIntentPolicyOutcome.Permit &&
            (decision.AllowedSystemIds.IsEmpty || decision.AllowedItemKinds.IsEmpty ||
             decision.AllowedRelationshipTypes.IsEmpty || decision.AllowedSourceKinds.IsEmpty ||
             decision.MaximumResults <= 0))
            throw new UnauthorizedAccessException("OPA permit did not establish an explicit Existing Architecture scope.");
        if (decision.AllowedSystemIds.Any(id => id.Value == Guid.Empty) ||
            decision.AllowedItemKinds.Any(kind => !Enum.IsDefined(kind)) ||
            decision.AllowedRelationshipTypes.Any(string.IsNullOrWhiteSpace) ||
            decision.AllowedSourceKinds.Any(string.IsNullOrWhiteSpace))
            throw new UnauthorizedAccessException("OPA returned an invalid Existing Architecture scope.");
    }

    private static void ValidateCandidate(
        AuthorizedExistingArchitectureDiscoveryRequest request,
        ExistingArchitecturePolicyDecision decision,
        ImmutableHashSet<EnterpriseObjectId> availableSystemIds,
        string registeredSourceKind,
        ExistingArchitectureCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (!StringComparer.Ordinal.Equals(candidate.SourceKind, registeredSourceKind) ||
            !decision.AllowedSourceKinds.Contains(candidate.SourceKind))
            throw new UnauthorizedAccessException("Architecture source returned an unauthorized source kind.");
        if (candidate.ArchitectureItemId == Guid.Empty || candidate.SystemId.Value == Guid.Empty ||
            !availableSystemIds.Contains(candidate.SystemId) || !decision.AllowedSystemIds.Contains(candidate.SystemId))
            throw new UnauthorizedAccessException("Architecture item is not bound to an authorized Existing Systems identity.");
        if (!Enum.IsDefined(candidate.Kind) || !decision.AllowedItemKinds.Contains(candidate.Kind) ||
            candidate.ApprovalState != ExistingArchitectureApprovalState.Approved)
            throw new UnauthorizedAccessException("Architecture source returned an unauthorized or unapproved item.");
        if (!Enum.IsDefined(candidate.Classification) || candidate.Classification > decision.MaximumClassification ||
            !Enum.IsDefined(candidate.Lifecycle))
            throw new UnauthorizedAccessException("Architecture item exceeds its authorized state or classification.");
        if (!StringComparer.Ordinal.Equals(candidate.Environment, request.Environment))
            throw new UnauthorizedAccessException("Architecture item environment is outside the authorized scope.");
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Description);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Version);
        if (candidate.RelatedSystemId is { } related &&
            (related.Value == Guid.Empty || !availableSystemIds.Contains(related) || !decision.AllowedSystemIds.Contains(related)))
            throw new UnauthorizedAccessException("Architecture relationship targets an unauthorized system.");
        if (candidate.RelatedSystemId is null != string.IsNullOrWhiteSpace(candidate.RelationshipType) ||
            candidate.RelationshipType is not null && !decision.AllowedRelationshipTypes.Contains(candidate.RelationshipType))
            throw new UnauthorizedAccessException("Architecture relationship is structurally invalid or unauthorized.");
        if (candidate.ApprovedAt == default || candidate.UpdatedAt < candidate.ApprovedAt)
            throw new InvalidOperationException("Architecture approval and update timestamps are invalid.");
        ValidateEvidence(candidate.EvidenceReferences, "Approved architecture item");
        if (candidate.CredentialsIncluded || candidate.LiveSessionIncluded || candidate.ExecutableCommandIncluded ||
            candidate.GeneratedContentIncluded || candidate.ExternalEffectOccurred)
            throw new UnauthorizedAccessException("Architecture source returned forbidden active or generated content.");
    }

    private static async Task<AuthorizedExistingArchitectureItem> AuthorizeAndMapAsync(
        AuthorizedExistingArchitectureDiscoveryRequest request,
        ExistingArchitecturePolicyDecision decision,
        ExistingArchitectureCandidate candidate,
        ExistingArchitectureConformanceDecision conformance,
        IExistingArchitectureResultAuthorizer resultAuthorizer,
        CancellationToken cancellationToken)
    {
        var evidence = candidate.EvidenceReferences.Concat(conformance.EvidenceReferences)
            .Concat(decision.EvidenceReferences).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        var authorizationRequest = new ExistingArchitectureResultAuthorizationRequest(
            Guid.NewGuid(), request.DiscoveryId, request.Identity.TenantId,
            request.Identity.SubjectId, request.Purpose, "existing-architecture.item.read",
            candidate.ArchitectureItemId, candidate.SystemId, candidate.RelatedSystemId,
            candidate.Kind, candidate.RelationshipType, candidate.Classification,
            candidate.SourceKind, evidence, decision.DecidedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken);
        if (authorization.AuthorizationRequestId != authorizationRequest.AuthorizationRequestId ||
            authorization.DiscoveryId != request.DiscoveryId ||
            authorization.ArchitectureItemId != candidate.ArchitectureItemId ||
            !StringComparer.Ordinal.Equals(authorization.TenantId, request.Identity.TenantId) ||
            !StringComparer.Ordinal.Equals(authorization.Action, authorizationRequest.Action) ||
            !authorization.IsAllowed || string.IsNullOrWhiteSpace(authorization.Code) ||
            authorization.DecidedAt < authorizationRequest.RequestedAt)
            throw new UnauthorizedAccessException("Existing Architecture result authorization denied or mismatched.");
        ValidateEvidence(authorization.EvidenceReferences, "Existing Architecture result authorization");
        return new AuthorizedExistingArchitectureItem(
            candidate.ArchitectureItemId, candidate.SystemId.Value, candidate.RelatedSystemId?.Value,
            candidate.Kind, candidate.Name, candidate.Description, candidate.RelationshipType,
            candidate.Version, candidate.Classification, candidate.Environment, candidate.Lifecycle,
            candidate.SourceKind, candidate.EvidenceReferences.Order(StringComparer.Ordinal).ToImmutableArray(),
            conformance.EvidenceReferences.Order(StringComparer.Ordinal).ToImmutableArray(),
            authorization.EvidenceReferences.Order(StringComparer.Ordinal).ToImmutableArray(),
            candidate.ApprovedAt, candidate.UpdatedAt);
    }

    private static void ValidateConformance(
        Guid discoveryId,
        Guid? itemId,
        string tenantId,
        ExistingArchitectureConformanceDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.DiscoveryId != discoveryId || decision.ArchitectureItemId != itemId ||
            !StringComparer.Ordinal.Equals(decision.TenantId, tenantId) || !decision.IsConformant ||
            string.IsNullOrWhiteSpace(decision.Code))
            throw new UnauthorizedAccessException("Existing Architecture constitutional conformance denied or mismatched.");
        ValidateEvidence(decision.EvidenceReferences, "Existing Architecture conformance");
    }

    private static string Digest(
        AuthorizedExistingSystemsDiscoveryReceipt systems,
        ExistingArchitecturePolicyDecision decision,
        ImmutableArray<AuthorizedExistingArchitectureItem> items)
    {
        var canonical = new StringBuilder()
            .Append(systems.DiscoveryId.ToString("D")).Append('|')
            .Append(systems.InventorySha256Digest!.ToLowerInvariant()).Append('|')
            .Append(decision.DecisionRequestId.ToString("D")).Append('|')
            .Append(decision.BundleSha256Digest.ToLowerInvariant());
        foreach (var item in items)
        {
            canonical.Append('|').Append(item.ArchitectureItemId.ToString("D"))
                .Append(':').Append(item.SystemId.ToString("D"))
                .Append(':').Append(item.RelatedSystemId?.ToString("D") ?? string.Empty)
                .Append(':').Append(item.Kind)
                .Append(':').Append(item.Name)
                .Append(':').Append(item.Description)
                .Append(':').Append(item.RelationshipType ?? string.Empty)
                .Append(':').Append(item.Version)
                .Append(':').Append((int)item.Classification)
                .Append(':').Append(item.Environment)
                .Append(':').Append(item.Lifecycle)
                .Append(':').Append(item.SourceKind)
                .Append(':').Append(item.ApprovedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))
                .Append(':').Append(item.UpdatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))
                .Append(':').AppendJoin(',', item.EvidenceReferences)
                .Append(':').AppendJoin(',', item.ConformanceEvidenceReferences)
                .Append(':').AppendJoin(',', item.AuthorizationEvidenceReferences);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static void ValidateEvidenceReceipt(
        ExistingArchitectureEvidenceRecord record,
        ExistingArchitectureEvidenceReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (receipt.DiscoveryId != record.DiscoveryId || receipt.SystemsDiscoveryId != record.SystemsDiscoveryId ||
            receipt.RegistrationId != record.RegistrationId ||
            !StringComparer.Ordinal.Equals(receipt.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.ArchitectureSha256Digest, record.ArchitectureSha256Digest) ||
            string.IsNullOrWhiteSpace(receipt.EvidenceReference) || receipt.RecordedAt < record.DiscoveredAt)
            throw new InvalidOperationException("Existing Architecture evidence recorder returned a mismatched receipt.");
    }

    private static void ValidateEvidence(ImmutableArray<string> references, string owner)
    {
        if (references.IsDefaultOrEmpty || references.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException($"{owner} requires non-placeholder evidence references.");
    }

    private sealed record ArchitectureSourceResult(
        string SourceKind,
        IReadOnlyCollection<ExistingArchitectureCandidate> Candidates);
}
