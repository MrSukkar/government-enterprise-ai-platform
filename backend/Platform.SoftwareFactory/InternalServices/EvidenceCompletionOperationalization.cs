using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;
using Platform.Evidence.Chain;
using Platform.Governance.Policies;

namespace Platform.SoftwareFactory.InternalService;

public enum EvidenceCompletionRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class EvidenceCompletionRuntimeOptions
{
    public const string SectionName="Platform:SoftwareFactory:EvidenceCompletionRuntime";
    public string SignatureAlgorithm{get;init;}=string.Empty;
    public string SigningKeyId{get;init;}=string.Empty;
    public string SigningPrivateKeyPem{get;init;}=string.Empty;
    public Dictionary<string,string> TrustedPublicKeysPem{get;init;}=new(StringComparer.Ordinal);
    public string CertificateChainReference{get;init;}=string.Empty;
    public EvidenceCompletionRuntimeConfigurationState ConfigurationState
    {
        get{var values=new[]{SignatureAlgorithm,SigningKeyId,SigningPrivateKeyPem,CertificateChainReference};if(values.All(string.IsNullOrWhiteSpace)&&TrustedPublicKeysPem.Count==0)return EvidenceCompletionRuntimeConfigurationState.Unconfigured;if(SignatureAlgorithm is not("RS256" or "ES256")||values.Any(string.IsNullOrWhiteSpace)||TrustedPublicKeysPem.Count==0||!TrustedPublicKeysPem.ContainsKey(SigningKeyId))return EvidenceCompletionRuntimeConfigurationState.Invalid;return EvidenceCompletionRuntimeConfigurationState.Configured;}
    }
    public bool IsOperationallyConfigured=>ConfigurationState==EvidenceCompletionRuntimeConfigurationState.Configured;
}
public sealed record EvidenceCompletionRuntimeReadiness(EvidenceCompletionRuntimeConfigurationState State);
public sealed class EvidenceCompletionDependencyUnavailableException(string message):Exception(message);

public sealed class SovereignEvidenceCompletionPolicyGate(IPolicyBundleVerifier verifier,ISovereignPolicyEvaluationClient client):IEvidenceCompletionPolicyGate
{
 public async Task<EvidenceCompletionPolicyDecision> EvaluateAsync(EvidenceCompletionPolicyInput i,CancellationToken ct){var v=await verifier.VerifyAsync(new(i.PolicyBundle.BundleId,i.PolicyBundle.Version,i.PolicyBundle.Sha256Digest,i.PolicyBundle.SignatureReference,i.PolicyBundle.Environment,i.PolicyBundle.ActivatedAt),ct);var a=new Dictionary<string,string>(StringComparer.Ordinal){{"completionId",i.CompletionId.ToString("D")},{"contextualizationId",i.ContextualizationId.ToString("D")},{"deliveryRunId",i.DeliveryRunId.ToString("D")},{"chainId",i.ChainId.ToString("D")},{"correlationId",i.CorrelationId},{"contextualizationEvidenceReference",i.ContextualizationEvidenceReference},{"payloadSha256Digest",i.PayloadSha256Digest},{"traceReferences",string.Join(',',i.TraceReferences.Order(StringComparer.Ordinal))}}.ToImmutableSortedDictionary(StringComparer.Ordinal);var e=i.EvidenceReferences.Append(v.VerificationEvidenceReference).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();var d=await client.EvaluateAsync(new(i.DecisionRequestId,"internal-service.evidence.complete",i.ChainId.ToString("D"),i.TenantId,i.SubjectId,i.Purpose,i.MaximumClassification.ToString(),i.Environment,v,a,e,v.VerifiedAt>i.EvaluatedAt?v.VerifiedAt:i.EvaluatedAt),ct);if(d.Outcome==OpaDecisionOutcome.Deny){if(d.Scope is not null)throw new UnauthorizedAccessException("Denied Evidence completion decision returned scope.");return Create(i,v,d,GovernedIntentPolicyOutcome.Deny,i.MaximumClassification,false,false,false);}var envelope=d.Scope??throw new UnauthorizedAccessException("Evidence completion scope missing.");var s=envelope.EvidenceCompletion??throw new UnauthorizedAccessException("Evidence completion action scope missing.");if(envelope.EnterpriseModel is not null||envelope.AutomaticRegistration is not null||envelope.OpenTelemetry is not null||envelope.Deployment is not null||envelope.Artifact is not null||envelope.CiCd is not null||envelope.Git is not null||envelope.HumanReview is not null||envelope.Tests is not null||envelope.Sandbox is not null||envelope.SecurityValidation is not null||envelope.StaticValidation is not null||envelope.CodeGeneration is not null||envelope.AiPlanning is not null||envelope.ApprovedPackages is not null||envelope.ExistingArchitecture is not null||envelope.ExistingSystems is not null)throw new UnauthorizedAccessException("Evidence completion decision mixed scopes.");if(!Enum.TryParse<DataClassification>(s.MaximumClassification,true,out var c)||s.ContextualizationId!=i.ContextualizationId||s.DeliveryRunId!=i.DeliveryRunId||s.ChainId!=i.ChainId||s.CorrelationId!=i.CorrelationId||s.ContextualizationEvidenceReference!=i.ContextualizationEvidenceReference||!StringComparer.OrdinalIgnoreCase.Equals(s.PayloadSha256Digest,i.PayloadSha256Digest)||!s.TraceReferences.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(i.TraceReferences)||!s.AppendFinalEvidenceAllowed||!s.VerificationAllowed||s.WorkflowAdvancementAllowed||s.RequiredRoles.IsDefaultOrEmpty||s.OutputKind!="evidence-completion-result")throw new UnauthorizedAccessException("Evidence completion scope mismatch.");return Create(i,v,d,GovernedIntentPolicyOutcome.Permit,c,s.AppendFinalEvidenceAllowed,s.VerificationAllowed,s.WorkflowAdvancementAllowed);}
 private static EvidenceCompletionPolicyDecision Create(EvidenceCompletionPolicyInput i,PolicyBundleVerification v,SovereignPolicyEvaluationDecision d,GovernedIntentPolicyOutcome o,DataClassification c,bool append,bool verify,bool advance)=>new(i.DecisionRequestId,i.CompletionId,i.ContextualizationId,i.DeliveryRunId,i.ChainId,i.CorrelationId,i.PayloadSha256Digest,i.TenantId,i.Environment,append,verify,advance,v.BundleId,v.Version,v.Sha256Digest,true,v.VerificationEvidenceReference,o,c,d.Reasons,d.EvidenceReferences,d.DecidedAt);
}

public sealed class DeterministicEvidenceAccessAuthorizer:IEvidenceAccessAuthorizer
{
 public Task<EvidenceAuthorizationDecision> AuthorizeAppendAsync(EvidenceAppendRequest r,CancellationToken ct)=>Authorize(r.Permissions.Contains("evidence.append")&&r.Stage==EvidenceStage.Evidence,$"append|{r.ChainId:D}|{r.TenantId}|{r.ActorSubjectId}|{r.Purpose}",ct);
 public Task<EvidenceAuthorizationDecision> AuthorizeVerificationAsync(EvidenceVerificationRequest r,CancellationToken ct)=>Authorize(r.Permissions.Contains("evidence.verify"),$"verify|{r.ChainId:D}|{r.TenantId}|{r.RequestingSubjectId}|{r.Purpose}",ct);
 public Task<EvidenceAuthorizationDecision> AuthorizeClassificationAsync(EvidenceVerificationRequest r,string classification,CancellationToken ct){var allowed=Enum.TryParse<DataClassification>(classification,true,out var actual)&&Enum.TryParse<DataClassification>(r.MaximumAuthorizedClassification,true,out var maximum)&&actual<=maximum;return Authorize(allowed,$"classification|{r.ChainId:D}|{r.TenantId}|{classification}|{r.MaximumAuthorizedClassification}",ct);}
 private static Task<EvidenceAuthorizationDecision> Authorize(bool allowed,string canonical,CancellationToken ct){ct.ThrowIfCancellationRequested();var d=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));return Task.FromResult(new EvidenceAuthorizationDecision(allowed,$"evidence://authorization/sha256/{d}"));}
}

public sealed class SovereignEvidenceSigner(EvidenceCompletionRuntimeOptionsAccessor accessor):IEvidenceSigner
{
 public Task<SignatureEnvelope> SignAsync(string tenant,string digest,CancellationToken ct){ct.ThrowIfCancellationRequested();var o=accessor.Options;if(!o.IsOperationallyConfigured)throw new EvidenceCompletionDependencyUnavailableException("Evidence signer is not configured.");var data=Convert.FromHexString(digest);byte[] sig;if(o.SignatureAlgorithm=="RS256"){using var key=RSA.Create();key.ImportFromPem(o.SigningPrivateKeyPem);sig=key.SignData(data,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);}else{using var key=ECDsa.Create();key.ImportFromPem(o.SigningPrivateKeyPem);sig=key.SignData(data,HashAlgorithmName.SHA256);}return Task.FromResult(new SignatureEnvelope(o.SignatureAlgorithm,o.SigningKeyId,Convert.ToBase64String(sig),o.CertificateChainReference,DateTimeOffset.UtcNow));}
}
public sealed class SovereignEvidenceSignatureVerifier(EvidenceCompletionRuntimeOptionsAccessor accessor):IEvidenceSignatureVerifier
{
 public Task<bool> VerifyAsync(string tenant,string digest,SignatureEnvelope s,CancellationToken ct){ct.ThrowIfCancellationRequested();var o=accessor.Options;if(!o.IsOperationallyConfigured||s.Algorithm!=o.SignatureAlgorithm||!o.TrustedPublicKeysPem.TryGetValue(s.KeyId,out var pem))return Task.FromResult(false);var data=Convert.FromHexString(digest);var sig=Convert.FromBase64String(s.SignatureBase64);if(s.Algorithm=="RS256"){using var key=RSA.Create();key.ImportFromPem(pem);return Task.FromResult(key.VerifyData(data,sig,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1));}using var ec=ECDsa.Create();ec.ImportFromPem(pem);return Task.FromResult(ec.VerifyData(data,sig,HashAlgorithmName.SHA256));}
}
public sealed class EvidenceCompletionRuntimeOptionsAccessor(Microsoft.Extensions.Options.IOptions<EvidenceCompletionRuntimeOptions> configured){public EvidenceCompletionRuntimeOptions Options=>configured.Value;}

public sealed class DeterministicEvidenceCompletionResultAuthorizer:IEvidenceCompletionResultAuthorizer
{
 public Task<EvidenceCompletionResultAuthorizationDecision> AuthorizeAsync(EvidenceCompletionResultAuthorizationRequest r,CancellationToken ct){ct.ThrowIfCancellationRequested();if(!r.Proof.IsValid||!r.Proof.IsComplete||r.FinalEntry.Stage!=EvidenceStage.Evidence||r.Proof.EntryProofs.Length!=Enum.GetValues<EvidenceStage>().Length||!StringComparer.OrdinalIgnoreCase.Equals(r.Proof.HeadSha256Digest,r.FinalEntry.EntrySha256Digest))throw new UnauthorizedAccessException("Unsafe Evidence completion result.");var d=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{r.AuthorizationRequestId:D}|{r.Proof.HeadSha256Digest}|{r.SubjectId}")));return Task.FromResult(new EvidenceCompletionResultAuthorizationDecision(r.AuthorizationRequestId,r.CompletionId,r.Proof.ChainId,r.TenantId,r.Proof.HeadSha256Digest!,true,"evidence-completion-result-authorized",r.EvidenceReferences.Append($"evidence://completion/result-authorization/{r.AuthorizationRequestId:D}/sha256/{d}").ToImmutableArray(),r.RequestedAt));}
}
