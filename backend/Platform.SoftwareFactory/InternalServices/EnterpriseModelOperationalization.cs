using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Governance.Policies;

namespace Platform.SoftwareFactory.InternalService;

public enum EnterpriseModelRuntimeConfigurationState { Unconfigured, Invalid, Configured }
public sealed record EnterpriseModelRuntimeReadiness(EnterpriseModelRuntimeConfigurationState State);
public sealed class EnterpriseModelDependencyUnavailableException(string message) : Exception(message);

public sealed class SovereignEnterpriseModelContextPolicyGate(
    IPolicyBundleVerifier bundleVerifier,
    ISovereignPolicyEvaluationClient policyClient) : IEnterpriseModelContextPolicyGate
{
    public async Task<EnterpriseModelContextPolicyDecision> EvaluateAsync(EnterpriseModelContextPolicyInput input, CancellationToken cancellationToken)
    {
        var verified = await bundleVerifier.VerifyAsync(new(input.PolicyBundle.BundleId, input.PolicyBundle.Version,
            input.PolicyBundle.Sha256Digest, input.PolicyBundle.SignatureReference, input.PolicyBundle.Environment,
            input.PolicyBundle.ActivatedAt), cancellationToken);
        var attributes = new Dictionary<string,string>(StringComparer.Ordinal)
        {
            ["contextualizationId"]=input.ContextualizationId.ToString("D"), ["registrationId"]=input.RegistrationId.ToString("D"),
            ["deliveryRunId"]=input.DeliveryRunId.ToString("D"), ["enterpriseObjectId"]=input.EnterpriseObjectId.ToString(),
            ["requestFingerprint"]=input.RequestFingerprint, ["registrationEvidenceReference"]=input.RegistrationEvidenceReference
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence=input.EvidenceReferences.Append(verified.VerificationEvidenceReference).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var decision=await policyClient.EvaluateAsync(new(input.DecisionRequestId,"internal-service.enterprise-model.contextualize",
            input.EnterpriseObjectId.ToString(),input.TenantId,input.SubjectId,input.Purpose,input.MaximumClassification.ToString(),
            input.Environment,verified,attributes,evidence,verified.VerifiedAt>input.EvaluatedAt?verified.VerifiedAt:input.EvaluatedAt),cancellationToken);
        if(decision.Outcome==OpaDecisionOutcome.Deny){if(decision.Scope is not null)throw new UnauthorizedAccessException("Denied Enterprise Model decision returned scope.");return Create(input,verified,decision,GovernedIntentPolicyOutcome.Deny,input.MaximumClassification,false,false,false);}
        var envelope=decision.Scope??throw new UnauthorizedAccessException("Enterprise Model scope missing.");
        var scope=envelope.EnterpriseModel??throw new UnauthorizedAccessException("Enterprise Model action scope missing.");
        if(envelope.AutomaticRegistration is not null||envelope.OpenTelemetry is not null||envelope.Deployment is not null||envelope.Artifact is not null||envelope.CiCd is not null||envelope.Git is not null||envelope.HumanReview is not null||envelope.Tests is not null||envelope.Sandbox is not null||envelope.SecurityValidation is not null||envelope.StaticValidation is not null||envelope.CodeGeneration is not null||envelope.AiPlanning is not null||envelope.ApprovedPackages is not null||envelope.ExistingArchitecture is not null||envelope.ExistingSystems is not null)throw new UnauthorizedAccessException("Enterprise Model decision mixed scopes.");
        if(!Enum.TryParse<DataClassification>(scope.MaximumClassification,true,out var classification)||scope.RegistrationId!=input.RegistrationId||scope.DeliveryRunId!=input.DeliveryRunId||scope.EnterpriseObjectId!=input.EnterpriseObjectId.Value||!StringComparer.OrdinalIgnoreCase.Equals(scope.RequestFingerprint,input.RequestFingerprint)||scope.RegistrationEvidenceReference!=input.RegistrationEvidenceReference||!scope.ObjectReadAllowed||scope.MutationAllowed||scope.WorkflowAdvancementAllowed||scope.RequiredRoles.IsDefaultOrEmpty||scope.OutputKind!="enterprise-model-context-result")throw new UnauthorizedAccessException("Enterprise Model scope mismatch.");
        return Create(input,verified,decision,GovernedIntentPolicyOutcome.Permit,classification,scope.ObjectReadAllowed,scope.MutationAllowed,scope.WorkflowAdvancementAllowed);
    }
    private static EnterpriseModelContextPolicyDecision Create(EnterpriseModelContextPolicyInput input,PolicyBundleVerification verified,SovereignPolicyEvaluationDecision decision,GovernedIntentPolicyOutcome outcome,DataClassification classification,bool read,bool mutate,bool advance)=>new(input.DecisionRequestId,input.ContextualizationId,input.RegistrationId,input.DeliveryRunId,input.EnterpriseObjectId,input.RequestFingerprint,input.TenantId,input.Environment,read,mutate,advance,verified.BundleId,verified.Version,verified.Sha256Digest,true,verified.VerificationEvidenceReference,outcome,classification,decision.Reasons,decision.EvidenceReferences,decision.DecidedAt);
}

public sealed class DeterministicEnterpriseModelContextResultAuthorizer:IEnterpriseModelContextResultAuthorizer
{
    public Task<EnterpriseModelContextResultAuthorizationDecision> AuthorizeAsync(EnterpriseModelContextResultAuthorizationRequest request,CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();request.EnterpriseObject.Validate();
        if(request.EnterpriseObject.TenantId!=request.TenantId||request.EnterpriseObject.Classification>request.MaximumClassification||request.EnterpriseObject.Source!="automatic-registration"||request.EnterpriseObject.State!="registered"||request.EnterpriseObject.Confidence!=1m||request.EnterpriseObject.EvidenceReferences.IsDefaultOrEmpty)throw new UnauthorizedAccessException("Unsafe Enterprise Model contextualization result.");
        var digest=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{request.AuthorizationRequestId:D}|{request.EnterpriseObject.Id}|{request.SubjectId}")));
        return Task.FromResult(new EnterpriseModelContextResultAuthorizationDecision(request.AuthorizationRequestId,request.ContextualizationId,request.EnterpriseObject.Id,request.TenantId,true,"enterprise-model-context-result-authorized",request.EvidenceReferences.Append($"evidence://enterprise-model/result-authorization/{request.AuthorizationRequestId:D}/sha256/{digest}").ToImmutableArray(),request.RequestedAt));
    }
}
