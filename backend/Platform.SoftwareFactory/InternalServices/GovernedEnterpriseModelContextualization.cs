using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedEnterpriseModelContextualizationRequest(
    Guid ContextualizationId, Guid RegistrationId, Guid DeliveryRunId,
    EnterpriseObjectId ExpectedEnterpriseObjectId, string ExpectedRequestFingerprint,
    string ExpectedRegistrationEvidenceReference, GovernedIdentity Identity, string Purpose,
    DataClassification MaximumClassification, string AuthorizationEvidenceReference,
    string Environment, IntentPolicyBundleReference PolicyBundle, DateTimeOffset RequestedAt)
{
    public GovernedEnterpriseModelContextualizationRequest Validate()
    {
        if (ContextualizationId == Guid.Empty || RegistrationId == Guid.Empty || DeliveryRunId == Guid.Empty || ExpectedEnterpriseObjectId.Value == Guid.Empty)
            throw new InvalidOperationException("Enterprise Model contextualization identities are required.");
        if (!ExpectedRequestFingerprint.StartsWith("sha256:", StringComparison.Ordinal)) throw new InvalidOperationException("Qualified registration fingerprint is required.");
        GovernedAiPlanningRequest.ValidateDigest(ExpectedRequestFingerprint["sha256:".Length..], "registration fingerprint");
        ArgumentException.ThrowIfNullOrWhiteSpace(ExpectedRegistrationEvidenceReference); ArgumentNullException.ThrowIfNull(Identity);
        if (!Identity.IsAuthenticated || !Identity.Permissions.Contains("operator.internal-service.enterprise-model.contextualize"))
            throw new UnauthorizedAccessException("Enterprise Model contextualization permission is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose); ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment); ArgumentNullException.ThrowIfNull(PolicyBundle); PolicyBundle.Validate();
        if (!Enum.IsDefined(MaximumClassification) || Identity.Clearance < MaximumClassification || !Eq(Environment, PolicyBundle.Environment) || RequestedAt < PolicyBundle.ActivatedAt)
            throw new UnauthorizedAccessException("Enterprise Model contextualization scope is invalid.");
        return this;
    }
    private static bool Eq(string left, string right) => StringComparer.Ordinal.Equals(left, right);
}

public interface IAuthorizedAutomaticRegistrationReceiptReader
{ Task<GovernedAutomaticRegistrationReceipt?> LoadAsync(Guid registrationId, string tenantId, CancellationToken cancellationToken); }
public interface IEnterpriseModelDeliveryRunReader
{ Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken); }
public interface IAuthorizedRegisteredEnterpriseObjectReader
{ Task<EnterpriseObject?> LoadAsync(EnterpriseObjectId objectId, string tenantId, CancellationToken cancellationToken); }

public sealed record EnterpriseModelContextPolicyInput(
    Guid DecisionRequestId, Guid ContextualizationId, Guid RegistrationId, Guid DeliveryRunId,
    EnterpriseObjectId EnterpriseObjectId, string RequestFingerprint, string RegistrationEvidenceReference,
    string TenantId, string SubjectId, string Purpose, string Environment, DataClassification MaximumClassification,
    IntentPolicyBundleReference PolicyBundle, ImmutableArray<string> EvidenceReferences, DateTimeOffset EvaluatedAt);
public sealed record EnterpriseModelContextPolicyDecision(
    Guid DecisionRequestId, Guid ContextualizationId, Guid RegistrationId, Guid DeliveryRunId,
    EnterpriseObjectId EnterpriseObjectId, string RequestFingerprint, string TenantId, string Environment,
    bool ObjectReadAllowed, bool MutationAllowed, bool WorkflowAdvancementAllowed,
    string BundleId, string BundleVersion, string BundleSha256Digest, bool PolicySignatureValid,
    string PolicyVerificationEvidenceReference, GovernedIntentPolicyOutcome Outcome,
    DataClassification MaximumClassification, ImmutableArray<string> Reasons,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);
public interface IEnterpriseModelContextPolicyGate
{ Task<EnterpriseModelContextPolicyDecision> EvaluateAsync(EnterpriseModelContextPolicyInput input, CancellationToken cancellationToken); }

public sealed record EnterpriseModelContextResultAuthorizationRequest(
    Guid AuthorizationRequestId, Guid ContextualizationId, string TenantId, string SubjectId,
    string Purpose, string Environment, DataClassification MaximumClassification,
    EnterpriseObject EnterpriseObject, ImmutableArray<string> EvidenceReferences, DateTimeOffset RequestedAt);
public sealed record EnterpriseModelContextResultAuthorizationDecision(
    Guid AuthorizationRequestId, Guid ContextualizationId, EnterpriseObjectId EnterpriseObjectId,
    string TenantId, bool IsAllowed, string Code, ImmutableArray<string> EvidenceReferences, DateTimeOffset DecidedAt);
public interface IEnterpriseModelContextResultAuthorizer
{ Task<EnterpriseModelContextResultAuthorizationDecision> AuthorizeAsync(EnterpriseModelContextResultAuthorizationRequest request, CancellationToken cancellationToken); }

public sealed record EnterpriseModelContextEvidenceRecord(
    Guid ContextualizationId, Guid RegistrationId, Guid DeliveryRunId, string TenantId,
    Guid PolicyDecisionRequestId, EnterpriseObject EnterpriseObject,
    ImmutableArray<string> EvidenceReferences, DateTimeOffset AuthorizedAt);
public sealed record EnterpriseModelContextEvidenceReceipt(
    Guid ContextualizationId, Guid DeliveryRunId, EnterpriseObjectId EnterpriseObjectId,
    string TenantId, string EvidenceReference, DateTimeOffset RecordedAt);
public interface IEnterpriseModelContextEvidenceRecorder
{ Task<EnterpriseModelContextEvidenceReceipt> RecordAsync(EnterpriseModelContextEvidenceRecord record, CancellationToken cancellationToken); }

public sealed record GovernedEnterpriseModelContextualizationReceipt(
    Guid ContextualizationId, Guid RegistrationId, Guid DeliveryRunId, string TenantId,
    GovernedIntentPolicyOutcome PolicyOutcome, bool IsAccepted, bool EnterpriseModelContextualized,
    bool EnterpriseModelMutated, bool WorkflowAdvanced, bool EvidenceCompleted, bool CanAdvance,
    EnterpriseObjectId EnterpriseObjectId, string RequestFingerprint,
    string? ContextualizationEvidenceReference, ImmutableArray<string> EvidenceReferences,
    string NextRequiredGate, DateTimeOffset CompletedAt);

public sealed class GovernedEnterpriseModelContextualizationEngine
{
    public async Task<GovernedEnterpriseModelContextualizationReceipt> ContextualizeAsync(
        GovernedEnterpriseModelContextualizationRequest request, IEnterpriseModelContextPolicyGate policyGate,
        IAuthorizedAutomaticRegistrationReceiptReader registrationReader, IEnterpriseModelDeliveryRunReader runReader,
        IAuthorizedRegisteredEnterpriseObjectReader objectReader, IEnterpriseModelContextResultAuthorizer resultAuthorizer,
        IEnterpriseModelContextEvidenceRecorder evidenceRecorder, CancellationToken cancellationToken)
    {
        request.Validate();
        var input = new EnterpriseModelContextPolicyInput(Guid.NewGuid(), request.ContextualizationId, request.RegistrationId,
            request.DeliveryRunId, request.ExpectedEnterpriseObjectId, request.ExpectedRequestFingerprint,
            request.ExpectedRegistrationEvidenceReference, request.Identity.TenantId, request.Identity.SubjectId,
            request.Purpose, request.Environment, request.MaximumClassification, request.PolicyBundle,
            [request.AuthorizationEvidenceReference, request.ExpectedRegistrationEvidenceReference], request.RequestedAt);
        var policy = await policyGate.EvaluateAsync(input, cancellationToken); ValidatePolicy(input, request.Identity, policy);
        var policyEvidence = Normalize(input.EvidenceReferences.Append(policy.PolicyVerificationEvidenceReference).Concat(policy.EvidenceReferences));
        if (policy.Outcome != GovernedIntentPolicyOutcome.Permit)
            return new(request.ContextualizationId, request.RegistrationId, request.DeliveryRunId, request.Identity.TenantId,
                policy.Outcome, false, false, false, false, false, false, request.ExpectedEnterpriseObjectId,
                request.ExpectedRequestFingerprint, null, policyEvidence, "Policy denial requires a new governed Enterprise Model request", policy.DecidedAt);
        var registration = await registrationReader.LoadAsync(request.RegistrationId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Governed Automatic Registration receipt was not found.");
        ValidateRegistration(request, registration);
        var run = await runReader.LoadAsync(request.DeliveryRunId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Software Delivery Run was not found.");
        ValidateRun(request, run);
        var enterpriseObject = await objectReader.LoadAsync(request.ExpectedEnterpriseObjectId, request.Identity.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Authorized registered Enterprise Object was not found.");
        ValidateObject(request, registration, enterpriseObject);
        var evidence = Normalize(policyEvidence.Concat(registration.EvidenceReferences).Append(registration.RegistrationEvidenceReference!)
            .Append(run.History[^1].EvidenceReference).Concat(enterpriseObject.EvidenceReferences)
            .Concat(enterpriseObject.Relationships.SelectMany(item => item.EvidenceReferences)));
        var authorizationRequest = new EnterpriseModelContextResultAuthorizationRequest(Guid.NewGuid(), request.ContextualizationId,
            request.Identity.TenantId, request.Identity.SubjectId, request.Purpose, request.Environment,
            request.MaximumClassification, enterpriseObject, evidence,
            registration.CompletedAt >= run.History[^1].CompletedAt ? registration.CompletedAt : run.History[^1].CompletedAt);
        var authorization = await resultAuthorizer.AuthorizeAsync(authorizationRequest, cancellationToken); ValidateAuthorization(authorizationRequest, authorization);
        var allEvidence = Normalize(evidence.Concat(authorization.EvidenceReferences));
        var record = new EnterpriseModelContextEvidenceRecord(request.ContextualizationId, request.RegistrationId,
            request.DeliveryRunId, request.Identity.TenantId, policy.DecisionRequestId, enterpriseObject, allEvidence, authorization.DecidedAt);
        var receipt = await evidenceRecorder.RecordAsync(record, cancellationToken); ValidateEvidence(record, receipt);
        return new(request.ContextualizationId, request.RegistrationId, request.DeliveryRunId, request.Identity.TenantId,
            policy.Outcome, true, true, false, false, false, false, enterpriseObject.Id, request.ExpectedRequestFingerprint,
            receipt.EvidenceReference, Normalize(allEvidence.Append(receipt.EvidenceReference)),
            "Separately approved Evidence completion", receipt.RecordedAt);
    }

    private static void ValidatePolicy(EnterpriseModelContextPolicyInput input, GovernedIdentity identity, EnterpriseModelContextPolicyDecision value)
    {
        if (value.DecisionRequestId != input.DecisionRequestId || value.ContextualizationId != input.ContextualizationId || value.RegistrationId != input.RegistrationId ||
            value.DeliveryRunId != input.DeliveryRunId || value.EnterpriseObjectId != input.EnterpriseObjectId || !EqI(value.RequestFingerprint, input.RequestFingerprint) ||
            !Eq(value.TenantId, input.TenantId) || !Eq(value.Environment, input.Environment) || !Eq(value.BundleId, input.PolicyBundle.BundleId) ||
            !Eq(value.BundleVersion, input.PolicyBundle.Version) || !EqI(value.BundleSha256Digest, input.PolicyBundle.Sha256Digest))
            throw new InvalidOperationException("OPA returned a mismatched Enterprise Model decision.");
        if (!value.PolicySignatureValid || value.MutationAllowed || value.WorkflowAdvancementAllowed || string.IsNullOrWhiteSpace(value.PolicyVerificationEvidenceReference) ||
            value.MaximumClassification > input.MaximumClassification || value.MaximumClassification > identity.Clearance || value.Reasons.IsDefaultOrEmpty ||
            value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < input.EvaluatedAt || value.Outcome == GovernedIntentPolicyOutcome.Permit && !value.ObjectReadAllowed)
            throw new UnauthorizedAccessException("Enterprise Model OPA decision is invalid.");
    }
    private static void ValidateRegistration(GovernedEnterpriseModelContextualizationRequest request, GovernedAutomaticRegistrationReceipt value)
    {
        if (value.RegistrationId != request.RegistrationId || value.DeliveryRunId != request.DeliveryRunId || !Eq(value.TenantId, request.Identity.TenantId) ||
            value.PolicyOutcome != GovernedIntentPolicyOutcome.Permit || !value.IsAccepted || !value.AutomaticRegistrationOccurred || !value.EnterpriseModelObjectPersisted ||
            value.WorkflowAdvanced || value.CanAdvance || value.EnterpriseObjectId != request.ExpectedEnterpriseObjectId || !EqI(value.RequestFingerprint, request.ExpectedRequestFingerprint) ||
            !Eq(value.RegistrationEvidenceReference, request.ExpectedRegistrationEvidenceReference) || value.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Governed Automatic Registration prerequisite is invalid or mismatched.");
    }
    private static void ValidateRun(GovernedEnterpriseModelContextualizationRequest request, SoftwareDeliveryRun run)
    {
        run.Validate(); var expected = Enum.GetValues<DeliveryStage>().TakeWhile(stage => stage <= DeliveryStage.AutomaticRegistration).ToArray();
        if (run.Id != request.DeliveryRunId || !Eq(run.TenantId, request.Identity.TenantId) || run.CurrentStage != DeliveryStage.AutomaticRegistration || run.History.Length != expected.Length ||
            run.History.Where((item, index) => item.Stage != expected[index] || item.Result != StageResult.Passed || string.IsNullOrWhiteSpace(item.EvidenceReference) || index > 0 && item.CompletedAt < run.History[index - 1].CompletedAt).Any())
            throw new UnauthorizedAccessException("Delivery run is not stopped at a valid Automatic Registration boundary.");
    }
    private static void ValidateObject(GovernedEnterpriseModelContextualizationRequest request, GovernedAutomaticRegistrationReceipt registration, EnterpriseObject value)
    {
        value.Validate();
        if (value.Id != request.ExpectedEnterpriseObjectId || !Eq(value.TenantId, request.Identity.TenantId) || value.Classification > request.MaximumClassification ||
            !Eq(value.Source, "automatic-registration") || !Eq(value.State, "registered") || value.Lifecycle != LifecycleState.Active ||
            value.PolicyReferences.IsDefaultOrEmpty || value.PermittedActions.IsDefaultOrEmpty || value.EvidenceReferences.IsDefaultOrEmpty ||
            !value.EvidenceReferences.Contains(request.ExpectedRegistrationEvidenceReference) || value.UpdatedAt < registration.CompletedAt ||
            value.Relationships.Any(item => item.EvidenceReferences.IsDefaultOrEmpty))
            throw new UnauthorizedAccessException("Registered Enterprise Object is invalid, unauthorized, or mismatched.");
    }
    private static void ValidateAuthorization(EnterpriseModelContextResultAuthorizationRequest request, EnterpriseModelContextResultAuthorizationDecision value)
    {
        if (value.AuthorizationRequestId != request.AuthorizationRequestId || value.ContextualizationId != request.ContextualizationId || value.EnterpriseObjectId != request.EnterpriseObject.Id ||
            !Eq(value.TenantId, request.TenantId) || !value.IsAllowed || string.IsNullOrWhiteSpace(value.Code) || value.EvidenceReferences.IsDefaultOrEmpty || value.DecidedAt < request.RequestedAt)
            throw new UnauthorizedAccessException("Enterprise Model result authorization is invalid.");
    }
    private static void ValidateEvidence(EnterpriseModelContextEvidenceRecord record, EnterpriseModelContextEvidenceReceipt value)
    {
        if (value.ContextualizationId != record.ContextualizationId || value.DeliveryRunId != record.DeliveryRunId || value.EnterpriseObjectId != record.EnterpriseObject.Id ||
            !Eq(value.TenantId, record.TenantId) || string.IsNullOrWhiteSpace(value.EvidenceReference) || value.RecordedAt < record.AuthorizedAt)
            throw new InvalidOperationException("Enterprise Model contextualization evidence receipt is invalid.");
    }
    private static ImmutableArray<string> Normalize(IEnumerable<string> values) => values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
    private static bool Eq(string? left, string? right) => StringComparer.Ordinal.Equals(left, right);
    private static bool EqI(string? left, string? right) => StringComparer.OrdinalIgnoreCase.Equals(left, right);
}
