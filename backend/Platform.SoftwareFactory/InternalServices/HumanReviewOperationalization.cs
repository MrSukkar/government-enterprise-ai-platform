using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Domain.Security;
using Platform.Evidence.Chain;
using Platform.Governance.Policies;

namespace Platform.SoftwareFactory.InternalService;

public enum HumanReviewRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class HumanReviewRuntimeOptions
{
    public const string SectionName = "Platform:SoftwareFactory:HumanReviewRuntime";
    public string AttestationEndpoint { get; init; } = string.Empty;
    public string VerifierId { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public Dictionary<string,string> TrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public int RequestTimeoutSeconds { get; init; }
    public int MaximumRequestBytes { get; init; }
    public int MaximumResponseBytes { get; init; }
    public HumanReviewRuntimeConfigurationState ConfigurationState {
        get {
            if (new[]{AttestationEndpoint,VerifierId,SignatureAlgorithm}.All(string.IsNullOrWhiteSpace) && TrustedPublicKeysPem.Count==0 && RequestTimeoutSeconds==0 && MaximumRequestBytes==0 && MaximumResponseBytes==0) return HumanReviewRuntimeConfigurationState.Unconfigured;
            if (!Uri.TryCreate(AttestationEndpoint,UriKind.Absolute,out var uri)||uri.Scheme!=Uri.UriSchemeHttps||!string.IsNullOrEmpty(uri.UserInfo)||!string.IsNullOrEmpty(uri.Query)||!string.IsNullOrEmpty(uri.Fragment)||string.IsNullOrWhiteSpace(VerifierId)||SignatureAlgorithm is not ("RS256" or "ES256")||TrustedPublicKeysPem.Count==0||RequestTimeoutSeconds<=0||MaximumRequestBytes<=0||MaximumResponseBytes<=0) return HumanReviewRuntimeConfigurationState.Invalid;
            return HumanReviewRuntimeConfigurationState.Configured;
        }
    }
    public bool IsOperationallyConfigured=>ConfigurationState==HumanReviewRuntimeConfigurationState.Configured;
}
public sealed record HumanReviewRuntimeReadiness(HumanReviewRuntimeConfigurationState State);

public sealed class SovereignHumanReviewPolicyGate(IPolicyBundleVerifier bundleVerifier,ISovereignPolicyEvaluationClient client):IHumanReviewPolicyGate
{
    public async Task<HumanReviewPolicyDecision> EvaluateAsync(HumanReviewPolicyInput i,CancellationToken ct){
        var v=await bundleVerifier.VerifyAsync(new(i.PolicyBundle.BundleId,i.PolicyBundle.Version,i.PolicyBundle.Sha256Digest,i.PolicyBundle.SignatureReference,i.PolicyBundle.Environment,i.PolicyBundle.ActivatedAt),ct);
        var attrs=new Dictionary<string,string>(StringComparer.Ordinal){["reviewId"]=i.ReviewId.ToString("D"),["testsExecutionId"]=i.TestsExecutionId.ToString("D"),["sandboxExecutionId"]=i.SandboxExecutionId.ToString("D"),["securityValidationId"]=i.SecurityValidationId.ToString("D"),["generationId"]=i.GenerationId.ToString("D"),["deliveryRunId"]=i.DeliveryRunId.ToString("D"),["reviewerSubjectId"]=i.ReviewerSubjectId,["initiatorSubjectId"]=i.InitiatorSubjectId,["candidateSha256Digest"]=i.CandidateSha256Digest,["securityReportSha256Digest"]=i.SecurityReportSha256Digest,["sandboxResultSha256Digest"]=i.SandboxResultSha256Digest,["testsResultSha256Digest"]=i.TestsResultSha256Digest,["testsEvidenceReference"]=i.TestsEvidenceReference,["humanDecision"]=i.HumanDecision.ToString(),["rationaleSha256Digest"]=Hash(i.Rationale),["humanAttestationReference"]=i.HumanAttestationReference,["conflictingSubjectIds"]=string.Join(',',i.DeclaredConflictingSubjectIds.Order(StringComparer.Ordinal)),["expectedVersion"]=i.ExpectedVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var evidence=i.EvidenceReferences.Append(v.VerificationEvidenceReference).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var d=await client.EvaluateAsync(new(i.DecisionRequestId,"internal-service.human-review.decide",i.ReviewId.ToString("D"),i.TenantId,i.ReviewerSubjectId,i.Purpose,i.MaximumClassification.ToString(),i.Environment,v,attrs,evidence,v.VerifiedAt>i.EvaluatedAt?v.VerifiedAt:i.EvaluatedAt),ct);
        if(d.Outcome==OpaDecisionOutcome.Deny){if(d.Scope is not null)throw new UnauthorizedAccessException("Denied Human Review decision returned scope.");return Make(i,v,d,GovernedIntentPolicyOutcome.Deny,i.MaximumClassification,i.DeclaredConflictingSubjectIds,false);}
        var e=d.Scope??throw new UnauthorizedAccessException("Human Review scope missing.");var s=e.HumanReview??throw new UnauthorizedAccessException("Human Review action scope missing.");
        if(e.Git is not null||e.Tests is not null||e.Sandbox is not null||e.SecurityValidation is not null||e.StaticValidation is not null||e.CodeGeneration is not null||e.AiPlanning is not null||e.ApprovedPackages is not null||e.ExistingArchitecture is not null||e.ExistingSystems is not null||!e.AllowedResourceIds.IsDefaultOrEmpty||!e.AllowedModalities.IsDefaultOrEmpty||!e.RequiredRoles.IsDefaultOrEmpty||e.MaximumResults!=0)throw new UnauthorizedAccessException("Human Review decision mixed scopes.");
        if(!Enum.TryParse<DataClassification>(s.MaximumClassification,true,out var c)||!StringComparer.Ordinal.Equals(s.ReviewerSubjectId,i.ReviewerSubjectId)||!StringComparer.Ordinal.Equals(s.InitiatorSubjectId,i.InitiatorSubjectId)||s.HumanDecision!=i.HumanDecision.ToString()||!StringComparer.OrdinalIgnoreCase.Equals(s.RationaleSha256Digest,Hash(i.Rationale))||s.HumanAttestationReference!=i.HumanAttestationReference||!s.ConflictingSubjectIds.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(i.DeclaredConflictingSubjectIds)||!s.ReviewerIsHuman||s.ExpectedVersion!=i.ExpectedVersion||s.RequiredRoles.IsDefaultOrEmpty||s.OutputKind!="human-review-decision")throw new UnauthorizedAccessException("Human Review scope mismatch.");
        return Make(i,v,d,GovernedIntentPolicyOutcome.Permit,c,s.ConflictingSubjectIds.ToImmutableHashSet(StringComparer.Ordinal),true);
    }
    private static HumanReviewPolicyDecision Make(HumanReviewPolicyInput i,PolicyBundleVerification v,SovereignPolicyEvaluationDecision d,GovernedIntentPolicyOutcome o,DataClassification c,ImmutableHashSet<string> conflicts,bool human)=>new(i.DecisionRequestId,i.ReviewId,i.TestsExecutionId,i.SandboxExecutionId,i.SecurityValidationId,i.GenerationId,i.DeliveryRunId,i.TenantId,i.ReviewerSubjectId,i.InitiatorSubjectId,i.Environment,i.CandidateSha256Digest,i.SecurityReportSha256Digest,i.SandboxResultSha256Digest,i.TestsResultSha256Digest,i.HumanDecision,i.Rationale,i.HumanAttestationReference,conflicts,human,v.BundleId,v.Version,v.Sha256Digest,true,v.VerificationEvidenceReference,o,c,d.Reasons,d.EvidenceReferences,d.DecidedAt);
    private static string Hash(string value)=>Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed class SovereignHttpHumanReviewAttestationVerifier(IHttpClientFactory factory,IOptions<HumanReviewRuntimeOptions> configured):IHumanReviewAttestationVerifier
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public async Task<HumanReviewAttestationDecision> VerifyAsync(HumanReviewAttestationRequest request,CancellationToken ct){var o=configured.Value;if(!o.IsOperationallyConfigured)throw new HumanReviewDependencyUnavailableException("Human attestation verifier is not configured.");var nonce=Guid.NewGuid();var input=Hash(JsonSerializer.Serialize(request,Json));var bytes=JsonSerializer.SerializeToUtf8Bytes(new AttestationRequest(nonce,o.VerifierId,input,request),Json);if(bytes.Length>o.MaximumRequestBytes)throw new UnauthorizedAccessException("Human attestation request exceeds bound.");using var message=new HttpRequestMessage(HttpMethod.Post,o.AttestationEndpoint){Content=new ByteArrayContent(bytes)};message.Content.Headers.ContentType=new("application/json");using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(o.RequestTimeoutSeconds));try{using var response=await factory.CreateClient("sovereign-human-review-attestation").SendAsync(message,HttpCompletionOption.ResponseHeadersRead,timeout.Token);if(!response.IsSuccessStatusCode)throw new HumanReviewDependencyUnavailableException("Human attestation rejected.");var body=await SovereignAiHttp.ReadBoundedAsync(response.Content,o.MaximumResponseBytes,timeout.Token);var r=JsonSerializer.Deserialize<AttestationResponse>(body,Json)??throw new UnauthorizedAccessException("Human attestation response missing.");if(r.Nonce!=nonce||r.VerifierId!=o.VerifierId||!StringComparer.OrdinalIgnoreCase.Equals(r.InputSha256Digest,input)||r.Decision.ReviewId!=request.ReviewId||r.Decision.ReviewerSubjectId!=request.ReviewerSubjectId||r.Decision.Decision!=request.Decision||!StringComparer.OrdinalIgnoreCase.Equals(r.Decision.ReviewPackageSha256Digest,request.ReviewPackageSha256Digest)||r.Decision.HumanAttestationReference!=request.HumanAttestationReference)throw new UnauthorizedAccessException("Human attestation binding invalid.");var payload=$"{r.Nonce:D}|{r.VerifierId}|{r.InputSha256Digest}|{JsonSerializer.Serialize(r.Decision,Json)}|{r.Signature.Algorithm}|{r.Signature.KeyId}|{r.Signature.CertificateChainReference}|{r.Signature.SignedAt:O}";if(!AiPlanningSignatureVerifier.Verify(o.SignatureAlgorithm,o.TrustedPublicKeysPem,r.Signature,SHA256.HashData(Encoding.UTF8.GetBytes(payload))))throw new UnauthorizedAccessException("Human attestation signature invalid.");return r.Decision;}catch(OperationCanceledException)when(!ct.IsCancellationRequested){throw new HumanReviewDependencyUnavailableException("Human attestation timed out.");}catch(HttpRequestException){throw new HumanReviewDependencyUnavailableException("Human attestation unavailable.");}}
    private static string Hash(string value)=>Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private sealed record AttestationRequest(Guid Nonce,string VerifierId,string InputSha256Digest,HumanReviewAttestationRequest Review);
    private sealed record AttestationResponse(Guid Nonce,string VerifierId,string InputSha256Digest,HumanReviewAttestationDecision Decision,SignatureEnvelope Signature);
}
