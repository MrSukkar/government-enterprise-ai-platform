using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Identity.Access;
using Platform.Integrations.ExistingSystems;

namespace Platform.SoftwareFactory.InternalService;

public sealed record AuthorizedExistingSystemsDiscoveryRequest(
    Guid DiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long ExpectedRegistrationVersion,
    string ExpectedIntentSha256Digest,
    string ExpectedContextSha256Digest,
    GovernedIdentity Identity,
    string Purpose,
    DataClassification MaximumClassification,
    string AuthorizationEvidenceReference,
    string Environment,
    IntentPolicyBundleReference PolicyBundle,
    DateTimeOffset RequestedAt)
{
    public AuthorizedExistingSystemsDiscoveryRequest Validate()
    {
        if (DiscoveryId == Guid.Empty)
            throw new InvalidOperationException("Existing Systems discovery identity is required.");
        if (ContextDiscoveryId == Guid.Empty)
            throw new InvalidOperationException("Authorized Enterprise Context discovery identity is required.");
        if (RegistrationId == Guid.Empty)
            throw new InvalidOperationException("Governed intent registration identity is required.");
        if (ExpectedRegistrationVersion < 0)
            throw new InvalidOperationException("A persisted intent registration version is required.");
        ValidateSha256(ExpectedIntentSha256Digest, "Expected intent");
        ValidateSha256(ExpectedContextSha256Digest, "Expected Enterprise Context");
        ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated)
            throw new UnauthorizedAccessException("Existing Systems discovery requires an authenticated identity.");
        if (!Identity.Permissions.Contains("developer.internal-service.systems.discover"))
            throw new UnauthorizedAccessException("The developer.internal-service.systems.discover permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Identity.SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Identity.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification)
            throw new UnauthorizedAccessException("Identity clearance is insufficient for Existing Systems discovery.");
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentNullException.ThrowIfNull(PolicyBundle);
        PolicyBundle.Validate();
        if (!StringComparer.Ordinal.Equals(Environment, PolicyBundle.Environment))
            throw new InvalidOperationException("Existing Systems policy environment does not match the request.");
        if (RequestedAt == default || RequestedAt < PolicyBundle.ActivatedAt)
            throw new InvalidOperationException("Existing Systems discovery time is invalid for the active policy bundle.");
        return this;
    }

    private static void ValidateSha256(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException($"{field} requires a SHA-256 digest.");
    }
}

public interface IAuthorizedEnterpriseContextSnapshotReader
{
    Task<AuthorizedEnterpriseContextDiscoveryReceipt?> LoadAsync(
        Guid contextDiscoveryId,
        string tenantId,
        CancellationToken cancellationToken);
}

public sealed record ExistingSystemsPolicyInput(
    Guid DecisionRequestId,
    Guid DiscoveryId,
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
    IntentPolicyBundleReference PolicyBundle,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset EvaluatedAt);

public sealed record ExistingSystemsPolicyDecision(
    Guid DecisionRequestId,
    Guid DiscoveryId,
    Guid ContextDiscoveryId,
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
    ImmutableHashSet<EnterpriseObjectId> AllowedSystemIds,
    ImmutableHashSet<string> AllowedRelationshipTypes,
    ImmutableHashSet<string> AllowedSourceKinds,
    int MaximumResults,
    ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IExistingSystemsPolicyGate
{
    Task<ExistingSystemsPolicyDecision> EvaluateAsync(
        ExistingSystemsPolicyInput input,
        CancellationToken cancellationToken);
}

public sealed record ExistingSystemResultAuthorizationRequest(
    Guid AuthorizationRequestId,
    Guid DiscoveryId,
    string TenantId,
    string SubjectId,
    string Purpose,
    string Action,
    EnterpriseObjectId SystemId,
    EnterpriseObjectId? RelatedSystemId,
    string? RelationshipType,
    DataClassification Classification,
    string SourceKind,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset RequestedAt);

public sealed record ExistingSystemResultAuthorizationDecision(
    Guid AuthorizationRequestId,
    Guid DiscoveryId,
    string TenantId,
    string Action,
    EnterpriseObjectId SystemId,
    EnterpriseObjectId? RelatedSystemId,
    string? RelationshipType,
    bool IsAllowed,
    string Code,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DecidedAt);

public interface IExistingSystemResultAuthorizer
{
    Task<ExistingSystemResultAuthorizationDecision> AuthorizeAsync(
        ExistingSystemResultAuthorizationRequest request,
        CancellationToken cancellationToken);
}

public sealed record AuthorizedExistingSystemRelationship(
    Guid TargetSystemId,
    string RelationshipType,
    RelationshipKnowledgeState KnowledgeState,
    decimal Confidence,
    string Source,
    ImmutableArray<string> EvidenceReferences,
    ImmutableArray<string> AuthorizationEvidenceReferences,
    DateTimeOffset ObservedAt);

public sealed record AuthorizedExistingSystem(
    Guid SystemId,
    string Type,
    string State,
    string OwnerId,
    DataClassification Classification,
    ImmutableArray<AuthorizedExistingSystemRelationship> Relationships,
    ImmutableArray<string> PolicyReferences,
    ImmutableArray<string> PermittedActions,
    string Source,
    string SourceKind,
    decimal Confidence,
    ImmutableArray<string> EvidenceReferences,
    ImmutableArray<string> AuthorizationEvidenceReferences,
    LifecycleState Lifecycle,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ExistingSystemsEvidenceRecord(
    Guid DiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string SubjectId,
    string Purpose,
    string IntentSha256Digest,
    string ContextSha256Digest,
    Guid PolicyDecisionRequestId,
    string PolicyBundleId,
    string PolicyBundleVersion,
    string PolicyBundleSha256Digest,
    string InventorySha256Digest,
    ImmutableArray<AuthorizedExistingSystem> Systems,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset DiscoveredAt);

public sealed record ExistingSystemsEvidenceReceipt(
    Guid DiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    string TenantId,
    string InventorySha256Digest,
    string EvidenceReference,
    DateTimeOffset RecordedAt);

public interface IExistingSystemsEvidenceRecorder
{
    Task<ExistingSystemsEvidenceReceipt> RecordAsync(
        ExistingSystemsEvidenceRecord record,
        CancellationToken cancellationToken);
}

public sealed class ExistingSystemsDependencyUnavailableException(string message) : Exception(message);

public sealed record AuthorizedExistingSystemsDiscoveryReceipt(
    Guid DiscoveryId,
    Guid ContextDiscoveryId,
    Guid RegistrationId,
    long RegistrationVersion,
    string TenantId,
    string IntentSha256Digest,
    string ContextSha256Digest,
    GovernedIntentPolicyOutcome PolicyOutcome,
    bool IsInventoryReleased,
    bool CanAdvance,
    string? InventorySha256Digest,
    ImmutableArray<AuthorizedExistingSystem> Systems,
    string? DiscoveryEvidenceReference,
    ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate,
    DateTimeOffset CompletedAt);

public sealed class AuthorizedExistingSystemsDiscoveryEngine(
    IEnumerable<IExistingSystemInventorySource> inventorySources)
{
    private readonly IReadOnlyCollection<IExistingSystemInventorySource> _inventorySources = inventorySources.ToArray();

    public async Task<AuthorizedExistingSystemsDiscoveryReceipt> DiscoverAsync(
        AuthorizedExistingSystemsDiscoveryRequest request,
        IAuthorizedEnterpriseContextSnapshotReader contextReader,
        IExistingSystemsPolicyGate policyGate,
        IExistingSystemResultAuthorizer resultAuthorizer,
        IExistingSystemsEvidenceRecorder evidenceRecorder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(contextReader);
        ArgumentNullException.ThrowIfNull(policyGate);
        ArgumentNullException.ThrowIfNull(resultAuthorizer);
        ArgumentNullException.ThrowIfNull(evidenceRecorder);
        request.Validate();

        var context = await contextReader.LoadAsync(
            request.ContextDiscoveryId,
            request.Identity.TenantId,
            cancellationToken) ?? throw new KeyNotFoundException("Authorized Enterprise Context snapshot was not found.");
        ValidateContext(request, context);

        var decisionRequestId = Guid.NewGuid();
        var contextEvidence = context.EvidenceReferences
            .Append(context.DiscoveryEvidenceReference!)
            .Append(request.AuthorizationEvidenceReference)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var policyInput = new ExistingSystemsPolicyInput(
            decisionRequestId,
            request.DiscoveryId,
            context.DiscoveryId,
            context.RegistrationId,
            context.RegistrationVersion,
            context.TenantId,
            request.Identity.SubjectId,
            request.Purpose,
            request.MaximumClassification,
            request.Environment,
            "internal-service.existing-systems.discover",
            context.IntentSha256Digest,
            context.ContextSha256Digest!,
            request.PolicyBundle,
            contextEvidence,
            request.RequestedAt);
        var decision = await policyGate.EvaluateAsync(policyInput, cancellationToken);
        ValidateDecision(policyInput, request.Identity, decision);

        var decisionEvidence = contextEvidence
            .Append(decision.PolicyVerificationEvidenceReference)
            .Concat(decision.EvidenceReferences)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (decision.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new AuthorizedExistingSystemsDiscoveryReceipt(
                request.DiscoveryId,
                context.DiscoveryId,
                context.RegistrationId,
                context.RegistrationVersion,
                context.TenantId,
                context.IntentSha256Digest,
                context.ContextSha256Digest!,
                decision.Outcome,
                IsInventoryReleased: false,
                CanAdvance: false,
                InventorySha256Digest: null,
                Systems: [],
                DiscoveryEvidenceReference: null,
                decisionEvidence,
                "Policy denial requires a new authorized Existing Systems request",
                decision.DecidedAt);

        var selectedSources = SelectAuthorizedSources(decision);
        var scope = new ExistingSystemInventoryScope(
            context.TenantId,
            request.Purpose,
            decision.MaximumClassification,
            decision.AllowedSystemIds,
            decision.AllowedRelationshipTypes,
            decision.AllowedSourceKinds,
            decision.MaximumResults);
        var sourceResults = await Task.WhenAll(selectedSources.Select(async source =>
            new InventorySourceResult(
                source.SourceKind,
                await source.DiscoverAsync(scope, cancellationToken)
                    ?? throw new InvalidOperationException("Existing Systems source returned no result collection."))));
        var candidates = sourceResults
            .SelectMany(result => result.Candidates.Select(candidate => (result.SourceKind, Candidate: candidate)))
            .ToArray();
        if (candidates.Length > decision.MaximumResults)
            throw new InvalidOperationException("Existing Systems sources exceeded the policy-authorized result count.");

        var systems = ImmutableArray.CreateBuilder<AuthorizedExistingSystem>(candidates.Length);
        var seenSystemIds = new HashSet<EnterpriseObjectId>();
        foreach (var (sourceKind, candidate) in candidates.OrderBy(
                     item => item.Candidate.System.Id.Value))
        {
            ValidateCandidate(request, decision, sourceKind, candidate);
            if (!seenSystemIds.Add(candidate.System.Id))
                throw new InvalidOperationException("Existing Systems sources returned a duplicate enterprise system.");
            systems.Add(await AuthorizeAndMapAsync(
                request,
                decision,
                sourceKind,
                candidate.System,
                resultAuthorizer,
                cancellationToken));
        }

        var orderedSystems = systems
            .OrderBy(system => system.SystemId)
            .ToImmutableArray();
        var inventoryDigest = Digest(context, decision, orderedSystems);
        var inventoryEvidence = decisionEvidence
            .Concat(orderedSystems.SelectMany(system => system.EvidenceReferences))
            .Concat(orderedSystems.SelectMany(system => system.AuthorizationEvidenceReferences))
            .Concat(orderedSystems.SelectMany(system => system.Relationships)
                .SelectMany(relationship => relationship.EvidenceReferences))
            .Concat(orderedSystems.SelectMany(system => system.Relationships)
                .SelectMany(relationship => relationship.AuthorizationEvidenceReferences))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var evidenceRecord = new ExistingSystemsEvidenceRecord(
            request.DiscoveryId,
            context.DiscoveryId,
            context.RegistrationId,
            context.RegistrationVersion,
            context.TenantId,
            request.Identity.SubjectId,
            request.Purpose,
            context.IntentSha256Digest,
            context.ContextSha256Digest!,
            decision.DecisionRequestId,
            decision.BundleId,
            decision.BundleVersion,
            decision.BundleSha256Digest,
            inventoryDigest,
            orderedSystems,
            inventoryEvidence,
            decision.DecidedAt);
        var evidenceReceipt = await evidenceRecorder.RecordAsync(evidenceRecord, cancellationToken);
        ValidateEvidenceReceipt(evidenceRecord, evidenceReceipt);

        return new AuthorizedExistingSystemsDiscoveryReceipt(
            request.DiscoveryId,
            context.DiscoveryId,
            context.RegistrationId,
            context.RegistrationVersion,
            context.TenantId,
            context.IntentSha256Digest,
            context.ContextSha256Digest!,
            decision.Outcome,
            IsInventoryReleased: true,
            CanAdvance: false,
            inventoryDigest,
            orderedSystems,
            evidenceReceipt.EvidenceReference,
            inventoryEvidence
                .Append(evidenceReceipt.EvidenceReference)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToImmutableArray(),
            "Separately approved Existing Architecture discovery",
            evidenceReceipt.RecordedAt);
    }

    private IReadOnlyCollection<IExistingSystemInventorySource> SelectAuthorizedSources(
        ExistingSystemsPolicyDecision decision)
    {
        if (_inventorySources.Any(source => string.IsNullOrWhiteSpace(source.SourceKind)) ||
            _inventorySources.GroupBy(source => source.SourceKind, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new ExistingSystemsDependencyUnavailableException(
                "Existing Systems inventory source registration is invalid.");
        var availableKinds = _inventorySources
            .Select(source => source.SourceKind)
            .ToImmutableHashSet(StringComparer.Ordinal);
        if (!decision.AllowedSourceKinds.IsSubsetOf(availableKinds))
            throw new ExistingSystemsDependencyUnavailableException(
                "One or more OPA-authorized Existing Systems source kinds are unavailable.");
        return _inventorySources
            .Where(source => decision.AllowedSourceKinds.Contains(source.SourceKind))
            .ToArray();
    }

    private static void ValidateContext(
        AuthorizedExistingSystemsDiscoveryRequest request,
        AuthorizedEnterpriseContextDiscoveryReceipt context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.DiscoveryId != request.ContextDiscoveryId ||
            context.RegistrationId != request.RegistrationId ||
            context.RegistrationVersion != request.ExpectedRegistrationVersion ||
            !StringComparer.Ordinal.Equals(context.TenantId, request.Identity.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(context.IntentSha256Digest, request.ExpectedIntentSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(context.ContextSha256Digest, request.ExpectedContextSha256Digest) ||
            context.PolicyOutcome != GovernedIntentPolicyOutcome.Permit ||
            !context.IsContextReleased || context.CanAdvance ||
            string.IsNullOrWhiteSpace(context.ContextSha256Digest) ||
            string.IsNullOrWhiteSpace(context.DiscoveryEvidenceReference) ||
            context.DiscoveryEvidenceReference.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
            context.DiscoveryEvidenceReference.Contains("pending", StringComparison.OrdinalIgnoreCase) ||
            context.EvidenceReferences.IsDefaultOrEmpty ||
            context.Items.IsDefault ||
            context.CompletedAt == default || context.CompletedAt > request.RequestedAt)
            throw new UnauthorizedAccessException(
                "Authorized Enterprise Context snapshot does not match the Existing Systems request.");
        foreach (var evidenceReference in context.EvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        foreach (var item in context.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ResourceId) || string.IsNullOrWhiteSpace(item.Content) ||
                string.IsNullOrWhiteSpace(item.Source) || item.Classification > request.MaximumClassification ||
                !Enum.IsDefined(item.Modality) || item.Relevance is < 0 or > 1 ||
                item.EvidenceReferences.IsDefaultOrEmpty)
                throw new UnauthorizedAccessException("Enterprise Context item is invalid for Existing Systems discovery.");
            foreach (var evidenceReference in item.EvidenceReferences)
                ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        }
    }

    private static void ValidateDecision(
        ExistingSystemsPolicyInput input,
        GovernedIdentity identity,
        ExistingSystemsPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(decision.AllowedSystemIds);
        ArgumentNullException.ThrowIfNull(decision.AllowedRelationshipTypes);
        ArgumentNullException.ThrowIfNull(decision.AllowedSourceKinds);
        if (decision.DecisionRequestId != input.DecisionRequestId ||
            decision.DiscoveryId != input.DiscoveryId ||
            decision.ContextDiscoveryId != input.ContextDiscoveryId ||
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
            decision.MaximumClassification > identity.Clearance ||
            decision.MaximumClassification > input.MaximumClassification)
            throw new UnauthorizedAccessException(
                "OPA returned a mismatched Existing Systems decision; discovery denied fail closed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(decision.PolicyVerificationEvidenceReference);
        if (decision.Reasons.IsDefaultOrEmpty || decision.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("OPA Existing Systems decisions require reasons and evidence.");
        foreach (var reason in decision.Reasons) ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ValidateEvidenceReferences(decision.EvidenceReferences, "OPA Existing Systems decision");
        if (decision.DecidedAt < input.EvaluatedAt)
            throw new InvalidOperationException("OPA Existing Systems decision predates evaluation.");
        if (decision.Outcome == GovernedIntentPolicyOutcome.Permit &&
            (decision.AllowedSystemIds.IsEmpty || decision.AllowedRelationshipTypes.IsEmpty ||
             decision.AllowedSourceKinds.IsEmpty || decision.MaximumResults <= 0))
            throw new UnauthorizedAccessException("OPA permit did not establish an explicit Existing Systems scope.");
        if (decision.AllowedSystemIds.Any(id => id.Value == Guid.Empty) ||
            decision.AllowedRelationshipTypes.Any(string.IsNullOrWhiteSpace) ||
            decision.AllowedSourceKinds.Any(string.IsNullOrWhiteSpace))
            throw new UnauthorizedAccessException("OPA returned an invalid Existing Systems scope.");
    }

    private static void ValidateCandidate(
        AuthorizedExistingSystemsDiscoveryRequest request,
        ExistingSystemsPolicyDecision decision,
        string invokedSourceKind,
        ExistingSystemInventoryCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(candidate.System);
        var system = candidate.System;
        if (!StringComparer.Ordinal.Equals(candidate.SourceKind, invokedSourceKind) ||
            !decision.AllowedSourceKinds.Contains(candidate.SourceKind) ||
            candidate.CredentialsIncluded || candidate.LiveSessionIncluded ||
            candidate.ExecutableCommandIncluded || candidate.ExternalEffectOccurred ||
            system.Id.Value == Guid.Empty || !decision.AllowedSystemIds.Contains(system.Id) ||
            !StringComparer.Ordinal.Equals(system.TenantId, request.Identity.TenantId) ||
            !Enum.IsDefined(system.Classification) || system.Classification > decision.MaximumClassification ||
            !Enum.IsDefined(system.Lifecycle) ||
            system.Relationships.IsDefault || system.PolicyReferences.IsDefault ||
            system.PermittedActions.IsDefault || system.EvidenceReferences.IsDefaultOrEmpty ||
            system.CreatedAt == default || system.UpdatedAt > request.RequestedAt)
            throw new InvalidOperationException("Inventory source returned an out-of-scope existing system.");
        system.Validate();
        foreach (var value in system.PolicyReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in system.PermittedActions) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ValidateEvidenceReferences(system.EvidenceReferences, "Existing system");
        if (system.Relationships
            .GroupBy(relationship => $"{relationship.TargetId.Value:D}\u001f{relationship.RelationshipType}\u001f{relationship.Source}", StringComparer.Ordinal)
            .Any(group => group.Count() != 1))
            throw new InvalidOperationException("Inventory source returned duplicate system relationships.");
        foreach (var relationship in system.Relationships)
        {
            relationship.Validate();
            if (relationship.TargetId.Value == Guid.Empty ||
                !decision.AllowedSystemIds.Contains(relationship.TargetId) ||
                !decision.AllowedRelationshipTypes.Contains(relationship.RelationshipType) ||
                !Enum.IsDefined(relationship.KnowledgeState) ||
                relationship.ObservedAt == default || relationship.ObservedAt > request.RequestedAt)
                throw new InvalidOperationException("Inventory source returned an out-of-scope system relationship.");
            ValidateEvidenceReferences(relationship.EvidenceReferences, "Existing system relationship");
        }
    }

    private static async Task<AuthorizedExistingSystem> AuthorizeAndMapAsync(
        AuthorizedExistingSystemsDiscoveryRequest request,
        ExistingSystemsPolicyDecision decision,
        string sourceKind,
        EnterpriseObject system,
        IExistingSystemResultAuthorizer authorizer,
        CancellationToken cancellationToken)
    {
        var systemAuthorization = await AuthorizeResultAsync(
            new ExistingSystemResultAuthorizationRequest(
                Guid.NewGuid(), request.DiscoveryId, system.TenantId, request.Identity.SubjectId,
                request.Purpose, "existing-system.read", system.Id, null, null,
                system.Classification, sourceKind, system.EvidenceReferences, decision.DecidedAt),
            authorizer,
            cancellationToken);
        var relationships = ImmutableArray.CreateBuilder<AuthorizedExistingSystemRelationship>(system.Relationships.Length);
        foreach (var relationship in system.Relationships
                     .OrderBy(item => item.TargetId.Value)
                     .ThenBy(item => item.RelationshipType, StringComparer.Ordinal)
                     .ThenBy(item => item.Source, StringComparer.Ordinal))
        {
            var relationshipAuthorization = await AuthorizeResultAsync(
                new ExistingSystemResultAuthorizationRequest(
                    Guid.NewGuid(), request.DiscoveryId, system.TenantId, request.Identity.SubjectId,
                    request.Purpose, "existing-system.relationship.read", system.Id,
                    relationship.TargetId, relationship.RelationshipType, system.Classification,
                    sourceKind, relationship.EvidenceReferences, decision.DecidedAt),
                authorizer,
                cancellationToken);
            relationships.Add(new AuthorizedExistingSystemRelationship(
                relationship.TargetId.Value,
                relationship.RelationshipType,
                relationship.KnowledgeState,
                relationship.Confidence,
                relationship.Source,
                relationship.EvidenceReferences
                    .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
                relationshipAuthorization.EvidenceReferences
                    .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
                relationship.ObservedAt));
        }

        return new AuthorizedExistingSystem(
            system.Id.Value,
            system.Type,
            system.State,
            system.OwnerId,
            system.Classification,
            relationships.ToImmutable(),
            system.PolicyReferences.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
            system.PermittedActions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
            system.Source,
            sourceKind,
            system.Confidence,
            system.EvidenceReferences.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
            systemAuthorization.EvidenceReferences
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
            system.Lifecycle,
            system.CreatedAt,
            system.UpdatedAt);
    }

    private static async Task<ExistingSystemResultAuthorizationDecision> AuthorizeResultAsync(
        ExistingSystemResultAuthorizationRequest request,
        IExistingSystemResultAuthorizer authorizer,
        CancellationToken cancellationToken)
    {
        var decision = await authorizer.AuthorizeAsync(request, cancellationToken);
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.AuthorizationRequestId != request.AuthorizationRequestId ||
            decision.DiscoveryId != request.DiscoveryId ||
            !StringComparer.Ordinal.Equals(decision.TenantId, request.TenantId) ||
            !StringComparer.Ordinal.Equals(decision.Action, request.Action) ||
            decision.SystemId != request.SystemId ||
            decision.RelatedSystemId != request.RelatedSystemId ||
            !StringComparer.Ordinal.Equals(decision.RelationshipType, request.RelationshipType) ||
            !decision.IsAllowed || string.IsNullOrWhiteSpace(decision.Code) ||
            decision.EvidenceReferences.IsDefaultOrEmpty || decision.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Existing Systems result authorization denied or mismatched.");
        ValidateEvidenceReferences(decision.EvidenceReferences, "Existing Systems result authorization");
        return decision;
    }

    private static void ValidateEvidenceReferences(
        IEnumerable<string> evidenceReferences,
        string boundary)
    {
        foreach (var evidenceReference in evidenceReferences)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
            if (evidenceReference.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
                evidenceReference.Contains("pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{boundary} returned placeholder evidence.");
        }
    }

    private static void ValidateEvidenceReceipt(
        ExistingSystemsEvidenceRecord record,
        ExistingSystemsEvidenceReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (receipt.DiscoveryId != record.DiscoveryId ||
            receipt.ContextDiscoveryId != record.ContextDiscoveryId ||
            receipt.RegistrationId != record.RegistrationId ||
            !StringComparer.Ordinal.Equals(receipt.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.InventorySha256Digest, record.InventorySha256Digest) ||
            string.IsNullOrWhiteSpace(receipt.EvidenceReference) ||
            receipt.EvidenceReference.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
            receipt.EvidenceReference.Contains("pending", StringComparison.OrdinalIgnoreCase) ||
            receipt.RecordedAt < record.DiscoveredAt)
            throw new InvalidOperationException("Existing Systems evidence recorder returned a mismatched receipt.");
    }

    private static string Digest(
        AuthorizedEnterpriseContextDiscoveryReceipt context,
        ExistingSystemsPolicyDecision decision,
        ImmutableArray<AuthorizedExistingSystem> systems)
    {
        var canonicalSystems = systems.Select(system => string.Join('\u001e',
            system.SystemId.ToString("D"), system.Type, system.State, system.OwnerId,
            system.Classification, system.Source, system.SourceKind,
            system.Confidence.ToString(CultureInfo.InvariantCulture), system.Lifecycle,
            system.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            system.UpdatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            string.Join('\u001d', system.PolicyReferences),
            string.Join('\u001d', system.PermittedActions),
            string.Join('\u001d', system.EvidenceReferences),
            string.Join('\u001d', system.AuthorizationEvidenceReferences),
            string.Join('\u001c', system.Relationships.Select(relationship => string.Join('\u001b',
                relationship.TargetSystemId.ToString("D"), relationship.RelationshipType,
                relationship.KnowledgeState, relationship.Confidence.ToString(CultureInfo.InvariantCulture),
                relationship.Source, relationship.ObservedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                string.Join('\u001a', relationship.EvidenceReferences),
                string.Join('\u001a', relationship.AuthorizationEvidenceReferences))))));
        var canonical = string.Join('\u001f',
            context.RegistrationId.ToString("D"),
            context.RegistrationVersion.ToString(CultureInfo.InvariantCulture),
            context.DiscoveryId.ToString("D"),
            context.IntentSha256Digest.ToLowerInvariant(),
            context.ContextSha256Digest!.ToLowerInvariant(),
            decision.DecisionRequestId.ToString("D"),
            decision.BundleId,
            decision.BundleVersion,
            decision.BundleSha256Digest.ToLowerInvariant(),
            string.Join('\u0019', canonicalSystems));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private sealed record InventorySourceResult(
        string SourceKind,
        IReadOnlyCollection<ExistingSystemInventoryCandidate> Candidates);
}
