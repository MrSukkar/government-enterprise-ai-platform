using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Domain.Security;
using Platform.Evidence.Chain;
using Platform.Governance.Policies;
using Platform.Identity.Access;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.Sandbox;

namespace Platform.SoftwareFactory.InternalService;

public enum TestsRuntimeConfigurationState { Unconfigured, Invalid, Configured }

public sealed class TestsRuntimeOptions
{
    public const string SectionName = "Platform:SoftwareFactory:TestsRuntime";
    public string Endpoint { get; init; } = string.Empty;
    public string RuntimeProfile { get; init; } = string.Empty;
    public string OperatorId { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public Dictionary<string,string> TrustedPublicKeysPem { get; init; } = new(StringComparer.Ordinal);
    public int RequestTimeoutSeconds { get; init; }
    public int MaximumRequestBytes { get; init; }
    public int MaximumResponseBytes { get; init; }
    public TestsRuntimeConfigurationState ConfigurationState {
        get {
            if (new[]{Endpoint,RuntimeProfile,OperatorId,SignatureAlgorithm}.All(string.IsNullOrWhiteSpace) &&
                TrustedPublicKeysPem.Count==0 && RequestTimeoutSeconds==0 && MaximumRequestBytes==0 && MaximumResponseBytes==0)
                return TestsRuntimeConfigurationState.Unconfigured;
            if (!Uri.TryCreate(Endpoint,UriKind.Absolute,out var uri) || uri.Scheme!=Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
                string.IsNullOrWhiteSpace(RuntimeProfile) || string.IsNullOrWhiteSpace(OperatorId) ||
                SignatureAlgorithm is not ("RS256" or "ES256") || RequestTimeoutSeconds<=0 || MaximumRequestBytes<=0 ||
                MaximumResponseBytes<=0 || TrustedPublicKeysPem.Count==0 || TrustedPublicKeysPem.Any(x=>string.IsNullOrWhiteSpace(x.Key)||!x.Value.Contains("PUBLIC KEY",StringComparison.Ordinal)))
                return TestsRuntimeConfigurationState.Invalid;
            return TestsRuntimeConfigurationState.Configured;
        }
    }
    public bool IsOperationallyConfigured => ConfigurationState==TestsRuntimeConfigurationState.Configured;
}
public sealed record TestsRuntimeReadiness(TestsRuntimeConfigurationState State);

public sealed class SovereignTestsPolicyGate(IPolicyBundleVerifier bundleVerifier, ISovereignPolicyEvaluationClient client) : ITestsPolicyGate
{
    public async Task<TestsPolicyDecision> EvaluateAsync(TestsPolicyInput input,CancellationToken ct)
    {
        var verification=await bundleVerifier.VerifyAsync(new(input.PolicyBundle.BundleId,input.PolicyBundle.Version,input.PolicyBundle.Sha256Digest,input.PolicyBundle.SignatureReference,input.PolicyBundle.Environment,input.PolicyBundle.ActivatedAt),ct);
        var attrs=new Dictionary<string,string>(StringComparer.Ordinal){
            ["executionId"]=input.ExecutionId.ToString("D"),["sandboxExecutionId"]=input.SandboxExecutionId.ToString("D"),
            ["candidateSha256Digest"]=input.CandidateSha256Digest,["securityReportSha256Digest"]=input.SecurityReportSha256Digest,
            ["sandboxResultSha256Digest"]=input.SandboxResultSha256Digest,["testManifestReference"]=input.TestManifestReference,
            ["testManifestSha256Digest"]=input.TestManifestSha256Digest,["testImage"]=Key(input.TestImage),
            ["requiredTestIds"]=string.Join(',',input.RequiredTestIds.Order(StringComparer.Ordinal)),
            ["allowedTestCategories"]=string.Join(',',input.AllowedTestCategories.Order(StringComparer.Ordinal)),
            ["isolation"]=Isolation(input.IsolationPolicy),["environmentReferences"]=string.Join(',',input.NonSecretEnvironmentReferences.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>$"{x.Key}={x.Value}"))
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);
        var ev=input.EvidenceReferences.Append(verification.VerificationEvidenceReference).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var d=await client.EvaluateAsync(new(input.DecisionRequestId,"internal-service.tests.execute",input.SandboxExecutionId.ToString("D"),input.TenantId,input.SubjectId,input.Purpose,input.MaximumClassification.ToString(),input.Environment,verification,attrs,ev,verification.VerifiedAt>input.EvaluatedAt?verification.VerifiedAt:input.EvaluatedAt),ct);
        if(d.Outcome==OpaDecisionOutcome.Deny){if(d.Scope is not null)throw new UnauthorizedAccessException("Denied Tests decision returned scope.");return Make(input,verification,d,GovernedIntentPolicyOutcome.Deny,input.MaximumClassification,[],[],[],[],string.Empty);}
        var envelope=d.Scope??throw new UnauthorizedAccessException("OPA permit omitted Tests scope.");
        var s=envelope.Tests??throw new UnauthorizedAccessException("OPA permit omitted action-specific Tests scope.");
        if(envelope.Sandbox is not null||envelope.SecurityValidation is not null||envelope.StaticValidation is not null||envelope.CodeGeneration is not null||envelope.AiPlanning is not null||envelope.ApprovedPackages is not null||envelope.ExistingArchitecture is not null||envelope.ExistingSystems is not null)throw new UnauthorizedAccessException("Tests decision mixed scopes from another action.");
        if(!Enum.TryParse<DataClassification>(s.MaximumClassification,false,out var classification)||s.AllowedTestImage is null||s.AllowedEnvironmentReferences is null||s.RequiredRoles.IsDefaultOrEmpty||s.OutputKind!="tests-result")throw new UnauthorizedAccessException("Invalid Tests scope.");
        var image=Image(s.AllowedTestImage); var net=Set(s.AllowedNetworkDestinations); var required=Set(s.RequiredTestIds);var categories=Set(s.AllowedTestCategories);var roles=Set(s.RequiredRoles);
        var isolation=new SandboxIsolationPolicy(s.IsolationClass,s.Ephemeral,s.MicroVmIsolation,s.ProductionCredentialsAllowed,s.HostFilesystemAccessAllowed,s.NetworkDefaultDeny,net,s.CpuLimit,s.MemoryLimitBytes,TimeSpan.FromTicks(s.ExecutionTimeoutTicks));isolation.Validate();
        if(image!=input.TestImage||Isolation(isolation)!=Isolation(input.IsolationPolicy)||s.TestManifestReference!=input.TestManifestReference||!StringComparer.OrdinalIgnoreCase.Equals(s.TestManifestSha256Digest,input.TestManifestSha256Digest)||!required.SetEquals(input.RequiredTestIds)||!categories.SetEquals(input.AllowedTestCategories)||s.AllowedEnvironmentReferences.Count!=input.NonSecretEnvironmentReferences.Count||s.AllowedEnvironmentReferences.Any(x=>!input.NonSecretEnvironmentReferences.TryGetValue(x.Key,out var v)||v!=x.Value))throw new UnauthorizedAccessException("Tests scope mismatch.");
        return Make(input,verification,d,GovernedIntentPolicyOutcome.Permit,classification,required,categories,s.AllowedEnvironmentReferences.ToImmutableDictionary(StringComparer.Ordinal),net,roles,s.OutputKind);
    }
    private static TestsPolicyDecision Make(TestsPolicyInput i,PolicyBundleVerification v,SovereignPolicyEvaluationDecision d,GovernedIntentPolicyOutcome o,DataClassification c,ImmutableHashSet<string> tests,ImmutableHashSet<string> cats,ImmutableDictionary<string,string> env,ImmutableHashSet<string> net,ImmutableHashSet<string> roles,string kind)=>new(d.DecisionRequestId,i.ExecutionId,i.SandboxExecutionId,i.SecurityValidationId,i.GenerationId,i.DeliveryRunId,i.TenantId,i.Environment,i.CandidateSha256Digest,i.SecurityReportSha256Digest,i.SandboxResultSha256Digest,i.TestManifestReference,i.TestManifestSha256Digest,tests,cats,i.TestImage,i.IsolationPolicy,env,net,d.BundleId,d.BundleVersion,d.BundleSha256Digest,true,v.VerificationEvidenceReference,o,c,roles,kind,d.Reasons,d.EvidenceReferences,d.DecidedAt);
    private static TestsPolicyDecision Make(TestsPolicyInput i,PolicyBundleVerification v,SovereignPolicyEvaluationDecision d,GovernedIntentPolicyOutcome o,DataClassification c,ImmutableHashSet<string> tests,ImmutableHashSet<string> cats,ImmutableDictionary<string,string> env,ImmutableHashSet<string> net,string kind)=>Make(i,v,d,o,c,tests,cats,env,net,[],kind);
    private static ImmutableHashSet<string> Set(ImmutableArray<string> a){if(a.IsDefault||a.Any(string.IsNullOrWhiteSpace))throw new UnauthorizedAccessException("Invalid Tests set.");var s=a.ToImmutableHashSet(StringComparer.Ordinal);if(s.Count!=a.Length)throw new UnauthorizedAccessException("Duplicate Tests set.");return s;}
    private static PackageCoordinate Image(SovereignApprovedPackageCoordinate p){if(!Enum.TryParse<PackageKind>(p.Kind,false,out var k))throw new UnauthorizedAccessException();var x=new PackageCoordinate(k,p.Name,p.Version,p.ContentDigest);x.Validate();if(k!=PackageKind.SandboxImage)throw new UnauthorizedAccessException();return x;}
    private static string Key(PackageCoordinate p)=>$"{p.Kind}|{p.Name}|{p.Version}|{p.ContentDigest}";
    private static string Isolation(SandboxIsolationPolicy p)=>$"{p.IsolationClass}|{p.Ephemeral}|{p.MicroVmIsolation}|{p.ProductionCredentialsAllowed}|{p.HostFilesystemAccessAllowed}|{p.NetworkDefaultDeny}|{p.CpuLimit}|{p.MemoryLimitBytes}|{p.ExecutionTimeout.Ticks}|{string.Join(',',p.AllowedNetworkDestinations.Order(StringComparer.Ordinal))}";
}

public sealed class SovereignHttpGovernedTestRuntime(IHttpClientFactory factory,IOptions<TestsRuntimeOptions> configured) : IGovernedTestRuntime
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public async Task<GovernedTestRuntimeResult> ExecuteAsync(GovernedTestRuntimeRequest request,CancellationToken ct){
        request.Run.Validate();request.Candidate.Validate();request.Manifest.Validate();request.IsolationPolicy.Validate();var o=configured.Value;
        if(!o.IsOperationallyConfigured||request.Run.CurrentStage!=Delivery.DeliveryStage.Sandbox||!request.TestImageDecision.IsAllowed)throw new TestsDependencyUnavailableException("Tests runtime is not configured.");
        var id=Guid.NewGuid();var candidate=SovereignHttpCodeGenerationRuntime.CandidateDigest(request.Candidate);var input=Hash($"{id:D}|{o.OperatorId}|{o.RuntimeProfile}|{request.Run.Id:D}|{candidate}|{request.Manifest.Sha256Digest}|{request.TestImage.ContentDigest}|{request.IsolationPolicy.ExecutionTimeout.Ticks}");
        var bytes=JsonSerializer.SerializeToUtf8Bytes(new Request(id,o.OperatorId,o.RuntimeProfile,input,candidate,request),Json);if(bytes.Length>o.MaximumRequestBytes)throw new UnauthorizedAccessException("Tests request exceeds bound.");
        using var msg=new HttpRequestMessage(HttpMethod.Post,o.Endpoint){Content=new ByteArrayContent(bytes)};msg.Content.Headers.ContentType=new("application/json");using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(o.RequestTimeoutSeconds));
        try{using var response=await factory.CreateClient("sovereign-governed-tests").SendAsync(msg,HttpCompletionOption.ResponseHeadersRead,timeout.Token);if(!response.IsSuccessStatusCode)throw new TestsDependencyUnavailableException("Tests runtime rejected request.");var body=await SovereignAiHttp.ReadBoundedAsync(response.Content,o.MaximumResponseBytes,timeout.Token);var r=JsonSerializer.Deserialize<Response>(body,Json)??throw new InvalidOperationException("Empty Tests response.");Validate(r,id,input,candidate,request,o);return new(r.TimedOut,r.IsolationViolationDetected,r.TestResults,r.EvidenceReference);}catch(OperationCanceledException)when(!ct.IsCancellationRequested){throw new TestsDependencyUnavailableException("Tests runtime timed out.");}catch(HttpRequestException){throw new TestsDependencyUnavailableException("Tests runtime unavailable.");}}
    private static void Validate(Response r,Guid id,string input,string candidate,GovernedTestRuntimeRequest q,TestsRuntimeOptions o){if(r.InvocationId!=id||r.OperatorId!=o.OperatorId||r.RuntimeProfile!=o.RuntimeProfile||!StringComparer.OrdinalIgnoreCase.Equals(r.InputSha256Digest,input)||!StringComparer.OrdinalIgnoreCase.Equals(r.CandidateSha256Digest,candidate)||!StringComparer.OrdinalIgnoreCase.Equals(r.ManifestSha256Digest,q.Manifest.Sha256Digest)||r.TestImage!=q.TestImage||r.IsolationClass!="Firecracker-class"||!r.Ephemeral||!r.MicroVmIsolation||r.ProductionCredentialsMounted||r.HostFilesystemMounted||!r.NetworkDefaultDenyEnforced||!r.AllowedNetworkDestinations.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(q.IsolationPolicy.AllowedNetworkDestinations)||r.TimedOut||r.IsolationViolationDetected||string.IsNullOrWhiteSpace(r.EvidenceReference)||r.CompletedAt==default||r.Signature.SignedAt<r.CompletedAt)throw new UnauthorizedAccessException("Tests response binding invalid.");var payload=$"{r.InvocationId:D}|{r.OperatorId}|{r.RuntimeProfile}|{r.InputSha256Digest}|{r.CandidateSha256Digest}|{r.ManifestSha256Digest}|{r.TestImage.ContentDigest}|{string.Join(',',r.TestResults.OrderBy(x=>x.TestId,StringComparer.Ordinal).Select(x=>$"{x.TestId}:{x.Passed}:{x.EvidenceReference}"))}|{r.EvidenceReference}|{r.CompletedAt:O}|{r.Signature.Algorithm}|{r.Signature.KeyId}|{r.Signature.CertificateChainReference}|{r.Signature.SignedAt:O}";if(!AiPlanningSignatureVerifier.Verify(o.SignatureAlgorithm,o.TrustedPublicKeysPem,r.Signature,SHA256.HashData(Encoding.UTF8.GetBytes(payload))))throw new UnauthorizedAccessException("Tests response signature invalid.");}
    private static string Hash(string s)=>Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
    private sealed record Request(Guid InvocationId,string OperatorId,string RuntimeProfile,string InputSha256Digest,string CandidateSha256Digest,GovernedTestRuntimeRequest Tests);
    private sealed record Response(Guid InvocationId,string OperatorId,string RuntimeProfile,string InputSha256Digest,string CandidateSha256Digest,string ManifestSha256Digest,PackageCoordinate TestImage,string IsolationClass,bool Ephemeral,bool MicroVmIsolation,bool ProductionCredentialsMounted,bool HostFilesystemMounted,bool NetworkDefaultDenyEnforced,ImmutableArray<string> AllowedNetworkDestinations,bool TimedOut,bool IsolationViolationDetected,ImmutableArray<GovernedTestCaseResult> TestResults,string EvidenceReference,DateTimeOffset CompletedAt,SignatureEnvelope Signature);
}

public sealed class DeterministicTestsResultAuthorizer(IAccessPolicyEvaluator access) : ITestsResultAuthorizer
{
    public Task<TestsResultAuthorizationDecision> AuthorizeAsync(TestsResultAuthorizationRequest r,CancellationToken ct){ct.ThrowIfCancellationRequested();if(!r.Identity.IsAuthenticated||r.RequiredRoles.IsEmpty||!r.Result.TestResults.All(x=>x.Passed||x.Skipped))throw new UnauthorizedAccessException("Invalid Tests result authorization.");var d=access.Evaluate(new(r.Identity,r.Purpose,"tests-result.read",$"{r.ExecutionId:D}/{r.ResultSha256Digest}",r.TenantId,r.MaximumClassification,r.RequiredRoles,ImmutableHashSet.Create(StringComparer.Ordinal,"developer.internal-service.tests.execute"),r.SubjectId,false));var hash=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{r.AuthorizationRequestId:D}|{r.ResultSha256Digest}|{d.Code}")));return Task.FromResult(new TestsResultAuthorizationDecision(r.AuthorizationRequestId,r.ExecutionId,r.TenantId,r.ResultSha256Digest,d.IsAllowed,d.Code,r.EvidenceReferences.Append($"evidence://tests/result-authorization/{r.AuthorizationRequestId:D}/sha256/{hash}").ToImmutableArray(),r.RequestedAt));}
}
