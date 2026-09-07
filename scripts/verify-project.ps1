[CmdletBinding()]
param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$statePath = Join-Path $repositoryRoot 'project-os\project-state.json'
$solutionPath = Join-Path $repositoryRoot 'GovernmentEnterpriseAIPlatform.sln'

if (-not (Test-Path -LiteralPath $statePath)) {
    throw 'Missing project-os/project-state.json.'
}

$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
$phaseNumber = [int]$state.currentPhase.number
$phaseDirectory = Join-Path $repositoryRoot ('docs\phase-{0:D2}' -f $phaseNumber)
$acceptancePath = Join-Path $phaseDirectory ('PHASE_{0:D2}_ACCEPTANCE.md' -f $phaseNumber)

if (-not (Test-Path -LiteralPath $acceptancePath)) {
    throw "Missing acceptance artifact: $acceptancePath"
}

$acceptance = Get-Content -LiteralPath $acceptancePath -Raw
if ($acceptance -notmatch 'Status:\s*\*\*Satisfied\*\*') {
    throw "Acceptance gate is not satisfied: $acceptancePath"
}

$projectCount = (Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'backend') -Recurse -Filter '*.csproj').Count +
    (Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'frontend') -Recurse -Filter '*.csproj').Count

if ($projectCount -ne 15) {
    throw "Expected 15 projects, found $projectCount."
}

if ($phaseNumber -ge 6) {
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    if (-not (Test-Path -LiteralPath $openApiPath)) {
        throw "Missing approved OpenAPI contract: $openApiPath"
    }

    $openApi = Get-Content -LiteralPath $openApiPath -Raw | ConvertFrom-Json
    if ($openApi.openapi -ne '3.1.0') {
        throw "Expected OpenAPI 3.1.0, found '$($openApi.openapi)'."
    }

    $operationIds = @($openApi.paths.PSObject.Properties.Value.PSObject.Properties.Value.operationId | Where-Object { $_ })
    if (($operationIds | Sort-Object -Unique).Count -ne $operationIds.Count) {
        throw 'OpenAPI operationId values must be unique.'
    }
}

if ($phaseNumber -ge 7) {
    $enterpriseObjectPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Model\EnterpriseObject.cs'
    $relationshipStatePath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Model\RelationshipKnowledgeState.cs'
    if (-not (Test-Path -LiteralPath $enterpriseObjectPath) -or -not (Test-Path -LiteralPath $relationshipStatePath)) {
        throw 'Enterprise Model base artifacts are missing.'
    }

    $enterpriseObject = Get-Content -LiteralPath $enterpriseObjectPath -Raw
    @('Id', 'Type', 'State', 'OwnerId', 'Classification', 'Relationships', 'PolicyReferences', 'PermittedActions', 'Source', 'Confidence', 'EvidenceReferences', 'Lifecycle', 'CreatedAt', 'UpdatedAt') | ForEach-Object {
        if ($enterpriseObject -notmatch "\b$($_)\b") { throw "Enterprise Object is missing required field '$($_)'." }
    }

    $relationshipState = Get-Content -LiteralPath $relationshipStatePath -Raw
    @('Confirmed', 'Discovered', 'Inferred', 'Unknown') | ForEach-Object {
        if ($relationshipState -notmatch "\b$($_)\b") { throw "Missing relationship state '$($_)'." }
    }
}

if ($phaseNumber -ge 8) {
    $retrieverPath = Join-Path $repositoryRoot 'backend\Platform.Knowledge\Retrieval\AuthorizedKnowledgeRetriever.cs'
    $queryPath = Join-Path $repositoryRoot 'backend\Platform.Knowledge\Retrieval\KnowledgeQuery.cs'
    if (-not (Test-Path -LiteralPath $retrieverPath) -or -not (Test-Path -LiteralPath $queryPath)) {
        throw 'Authorized knowledge retrieval artifacts are missing.'
    }

    $retriever = Get-Content -LiteralPath $retrieverPath -Raw
    @('AuthorizedRetrievalScope', 'AuthorizeOrThrow', 'ValidateSourceScope', 'knowledge.context.read') | ForEach-Object {
        if ($retriever -notmatch [regex]::Escape($_)) { throw "Knowledge retrieval invariant '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 9) {
    $packagePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Packages\InstitutionalPackage.cs'
    $eligibilityPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Packages\PackageEligibilityEvaluator.cs'
    if (-not (Test-Path -LiteralPath $packagePath) -or -not (Test-Path -LiteralPath $eligibilityPath)) {
        throw 'Institutional package registry artifacts are missing.'
    }

    $eligibility = Get-Content -LiteralPath $eligibilityPath -Raw
    @('coordinate_mismatch', 'tenant_denied', 'environment_denied', 'sovereign_copy_required', 'approval_required', 'approval_expired') | ForEach-Object {
        if ($eligibility -notmatch [regex]::Escape($_)) { throw "Package eligibility guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 10) {
    $stagePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Delivery\DeliveryStage.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Delivery\DeterministicSoftwareFactoryEngine.cs'
    if (-not (Test-Path -LiteralPath $stagePath) -or -not (Test-Path -LiteralPath $enginePath)) {
        throw 'Software Factory delivery engine artifacts are missing.'
    }

    $stages = Get-Content -LiteralPath $stagePath -Raw
    @('Intent', 'EnterpriseContext', 'ExistingSystems', 'ExistingArchitecture', 'ApprovedPackages', 'AiPlanning', 'CodeGeneration', 'StaticValidation', 'SecurityValidation', 'Sandbox', 'Tests', 'HumanReview', 'Git', 'CiCd', 'Artifact', 'Deployment', 'OpenTelemetry', 'AutomaticRegistration', 'EnterpriseModel', 'Evidence') | ForEach-Object {
        if ($stages -notmatch "\b$($_)\b") { throw "Software Factory stage '$($_)' is missing." }
    }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('stage_order_denied', 'package_denied', 'independent_review_required', 'time_order_denied') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Software Factory guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 11) {
    $runtimePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\AiDevelopment\IAiDevelopmentRuntime.cs'
    $evaluationPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\AiDevelopment\AiEvaluationReport.cs'
    $candidatePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\AiDevelopment\EvaluatedAiCandidate.cs'
    if (-not (Test-Path -LiteralPath $runtimePath) -or -not (Test-Path -LiteralPath $evaluationPath) -or -not (Test-Path -LiteralPath $candidatePath)) {
        throw 'AI Development Engine artifacts are missing.'
    }

    $evaluation = Get-Content -LiteralPath $evaluationPath -Raw
    @('IsIndependentFromGenerationRuntime', 'RequiredCriteria', 'EvidenceReference') | ForEach-Object {
        if ($evaluation -notmatch [regex]::Escape($_)) { throw "AI evaluation invariant '$($_)' is missing." }
    }

    $candidate = Get-Content -LiteralPath $candidatePath -Raw
    if ($candidate -notmatch 'IsExecutable\s*=>\s*false') { throw 'AI candidate must remain non-executable.' }
}

if ($phaseNumber -ge 12) {
    $validationPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Validation\CodeValidationPipeline.cs'
    $sandboxPolicyPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Sandbox\SandboxIsolationPolicy.cs'
    $sandboxServicePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Sandbox\GovernedSandboxService.cs'
    if (-not (Test-Path -LiteralPath $validationPath) -or -not (Test-Path -LiteralPath $sandboxPolicyPath) -or -not (Test-Path -LiteralPath $sandboxServicePath)) {
        throw 'Validation or security sandbox artifacts are missing.'
    }

    $policy = Get-Content -LiteralPath $sandboxPolicyPath -Raw
    @('Firecracker-class', 'Ephemeral', 'MicroVmIsolation', 'ProductionCredentialsAllowed', 'HostFilesystemAccessAllowed', 'NetworkDefaultDeny', 'CpuLimit', 'MemoryLimitBytes', 'ExecutionTimeout') | ForEach-Object {
        if ($policy -notmatch [regex]::Escape($_)) { throw "Sandbox isolation invariant '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 13) {
    $workflowPath = Join-Path $repositoryRoot '.github\workflows\ci.yml'
    $sbomScriptPath = Join-Path $repositoryRoot 'scripts\generate-sbom.ps1'
    $supplyChainPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SupplyChain\SupplyChainVerificationPipeline.cs'
    if (-not (Test-Path -LiteralPath $workflowPath) -or -not (Test-Path -LiteralPath $sbomScriptPath) -or -not (Test-Path -LiteralPath $supplyChainPath)) {
        throw 'CI or supply-chain artifacts are missing.'
    }

    $lockFileCount = (Get-ChildItem -LiteralPath $repositoryRoot -Recurse -Filter 'packages.lock.json').Count
    if ($lockFileCount -ne 15) { throw "Expected 15 dependency lock files, found $lockFileCount." }

    $workflow = Get-Content -LiteralPath $workflowPath -Raw
    @('--force-evaluate', '--use-lock-file', 'Frontend dependency lock changed beyond the verified WebAssembly SDK content hash.', 'generate-sbom.ps1', 'actions/attest@f7c74d28b9d84cb8768d0b8ca14a4bac6ef463e6', 'checksums.txt', 'provenance.json') | ForEach-Object {
        if ($workflow -notmatch [regex]::Escape($_)) { throw "CI supply-chain step '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 14) {
    $profilePath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Sovereignty\SovereignDeploymentProfile.cs'
    $artifactPath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Sovereignty\VerifiedDeploymentArtifact.cs'
    $runtimePath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Sovereignty\ISovereignDeploymentRuntime.cs'
    $servicePath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Sovereignty\GovernedSovereignDeploymentService.cs'
    $dependencyPath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Sovereignty\SovereignDependencyKind.cs'
    $requestPath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Sovereignty\SovereignDeploymentRequest.cs'
    if (-not (Test-Path -LiteralPath $profilePath) -or -not (Test-Path -LiteralPath $artifactPath) -or
        -not (Test-Path -LiteralPath $runtimePath) -or -not (Test-Path -LiteralPath $servicePath) -or
        -not (Test-Path -LiteralPath $dependencyPath) -or -not (Test-Path -LiteralPath $requestPath)) {
        throw 'Sovereign deployment platform artifacts are missing.'
    }

    $profile = Get-Content -LiteralPath $profilePath -Raw
    @('AirGapped', 'ExternalControlPlaneAllowed', 'ExternalApiAllowed', 'ExternalAiServiceAllowed',
      'ExternalSaasAllowed', 'OutboundNetworkDefaultDeny', 'IsLocallyOperated') | ForEach-Object {
        if ($profile -notmatch [regex]::Escape($_)) { throw "Sovereign deployment invariant '$($_)' is missing." }
    }

    $dependencies = Get-Content -LiteralPath $dependencyPath -Raw
    @('ModelRuntime', 'ArtifactRegistry', 'PackageRegistry', 'PolicyAuthority', 'IdentityProvider',
      'EvidenceStore', 'ObservabilityBackend', 'SecretsManager', 'KeyManagement') | ForEach-Object {
        if ($dependencies -notmatch "\b$($_)\b") { throw "Sovereign dependency '$($_)' is missing." }
    }

    $artifact = Get-Content -LiteralPath $artifactPath -Raw
    @('SbomReference', 'BuildAttestationReference', 'SignatureReference', 'SupplyChainVerificationEvidenceReference') | ForEach-Object {
        if ($artifact -notmatch [regex]::Escape($_)) { throw "Verified deployment artifact control '$($_)' is missing." }
    }

    $request = Get-Content -LiteralPath $requestPath -Raw
    if ($request -notmatch 'HumanApprovalReference') { throw 'Sovereign deployment requires human approval evidence.' }

    $runtime = Get-Content -LiteralPath $runtimePath -Raw
    if ($runtime -notmatch 'ISovereignDeploymentRuntime') { throw 'Vendor-neutral sovereign deployment runtime abstraction is missing.' }

    $service = Get-Content -LiteralPath $servicePath -Raw
    if ($service -notmatch 'EvidenceReference') { throw 'Sovereign deployment evidence receipt validation is missing.' }
}

if ($phaseNumber -ge 15) {
    $observabilityProjectPath = Join-Path $repositoryRoot 'backend\Platform.Observability\Platform.Observability.csproj'
    $registrationPath = Join-Path $repositoryRoot 'backend\Platform.Observability\ObservabilityServiceCollectionExtensions.cs'
    $telemetryPath = Join-Path $repositoryRoot 'backend\Platform.Observability\OpenTelemetry\PlatformTelemetry.cs'
    $processorPath = Join-Path $repositoryRoot 'backend\Platform.Observability\OpenTelemetry\RedactingActivityProcessor.cs'
    $policyPath = Join-Path $repositoryRoot 'backend\Platform.Observability\Redaction\TelemetryRedactionPolicy.cs'
    $lockPath = Join-Path $repositoryRoot 'backend\Platform.Observability\packages.lock.json'
    if (-not (Test-Path -LiteralPath $registrationPath) -or -not (Test-Path -LiteralPath $telemetryPath) -or
        -not (Test-Path -LiteralPath $processorPath) -or -not (Test-Path -LiteralPath $policyPath)) {
        throw 'OpenTelemetry core or redaction artifacts are missing.'
    }

    $observabilityProject = Get-Content -LiteralPath $observabilityProjectPath -Raw
    @('OpenTelemetry.Extensions.Hosting', 'OpenTelemetry.Instrumentation.AspNetCore', 'Version="1.17.0"') | ForEach-Object {
        if ($observabilityProject -notmatch [regex]::Escape($_)) { throw "OpenTelemetry dependency control '$($_)' is missing." }
    }

    $registration = Get-Content -LiteralPath $registrationPath -Raw
    @('AddOpenTelemetry', 'ConfigureResource', 'WithTracing', 'AddAspNetCoreInstrumentation',
      'AddProcessor<RedactingActivityProcessor>', 'WithMetrics', 'AddMeter') | ForEach-Object {
        if ($registration -notmatch [regex]::Escape($_)) { throw "OpenTelemetry registration '$($_)' is missing." }
    }

    $policy = Get-Content -LiteralPath $policyPath -Raw
    @('authorization', 'cookie', 'credential', 'password', 'secret', 'token', 'api_key',
      'url.query', 'exception.message', 'exception.stacktrace', '[REDACTED]') | ForEach-Object {
        if ($policy -notmatch [regex]::Escape($_)) { throw "Telemetry redaction control '$($_)' is missing." }
    }

    $processor = Get-Content -LiteralPath $processorPath -Raw
    @('OnStart', 'OnEnd', 'ClearBaggage', 'TelemetryAttributeDisposition.Drop') | ForEach-Object {
        if ($processor -notmatch [regex]::Escape($_)) { throw "Redacting processor invariant '$($_)' is missing." }
    }

    $telemetry = Get-Content -LiteralPath $telemetryPath -Raw
    @('ActivitySource', 'Meter', 'Counter<long>', 'Histogram<double>', 'low-cardinality lowercase tokens') | ForEach-Object {
        if ($telemetry -notmatch [regex]::Escape($_)) { throw "Telemetry core invariant '$($_)' is missing." }
    }

    $lock = Get-Content -LiteralPath $lockPath -Raw
    @('OpenTelemetry.Extensions.Hosting', 'OpenTelemetry.Instrumentation.AspNetCore', '"resolved": "1.17.0"') | ForEach-Object {
        if ($lock -notmatch [regex]::Escape($_)) { throw "OpenTelemetry dependency lock '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 16) {
    $observabilityProjectPath = Join-Path $repositoryRoot 'backend\Platform.Observability\Platform.Observability.csproj'
    $registrationPath = Join-Path $repositoryRoot 'backend\Platform.Observability\ObservabilityServiceCollectionExtensions.cs'
    $exportProfilePath = Join-Path $repositoryRoot 'backend\Platform.Observability\Collection\CollectorAgentExportProfile.cs'
    $pipelinePath = Join-Path $repositoryRoot 'backend\Platform.Observability\Central\CollectorPipelineProfile.cs'
    $storagePath = Join-Path $repositoryRoot 'backend\Platform.Observability\Central\TelemetryStorageBinding.cs'
    $queryPath = Join-Path $repositoryRoot 'backend\Platform.Observability\Central\CentralObservabilityQuery.cs'
    $servicePath = Join-Path $repositoryRoot 'backend\Platform.Observability\Central\CentralObservabilityService.cs'
    if (-not (Test-Path -LiteralPath $exportProfilePath) -or -not (Test-Path -LiteralPath $pipelinePath) -or
        -not (Test-Path -LiteralPath $storagePath) -or -not (Test-Path -LiteralPath $queryPath) -or
        -not (Test-Path -LiteralPath $servicePath)) {
        throw 'Central observability artifacts are missing.'
    }

    $observabilityProject = Get-Content -LiteralPath $observabilityProjectPath -Raw
    @('OpenTelemetry.Exporter.OpenTelemetryProtocol', 'Version="1.17.0"') | ForEach-Object {
        if ($observabilityProject -notmatch [regex]::Escape($_)) { throw "Central telemetry dependency '$($_)' is missing." }
    }

    $registration = Get-Content -LiteralPath $registrationPath -Raw
    @('AddOtlpExporter', 'OtlpExportProtocol.HttpProtobuf', 'CollectorAgentExportProfile') | ForEach-Object {
        if ($registration -notmatch [regex]::Escape($_)) { throw "Collector agent registration '$($_)' is missing." }
    }

    $exportProfile = Get-Content -LiteralPath $exportProfilePath -Raw
    @('Observability:CollectorAgent', 'TrustAnchorReference', 'Uri.UriSchemeHttps') | ForEach-Object {
        if ($exportProfile -notmatch [regex]::Escape($_)) { throw "Collector export control '$($_)' is missing." }
    }

    $pipeline = Get-Content -LiteralPath $pipelinePath -Raw
    @('AgentEndpoint', 'GatewayEndpoint', 'TraceAwareRoutingEnabled', 'redaction', 'tenant_isolation',
      'classification_enforcement', 'batch', 'IsLocallyOperated') | ForEach-Object {
        if ($pipeline -notmatch [regex]::Escape($_)) { throw "Collector pipeline invariant '$($_)' is missing." }
    }

    $storage = Get-Content -LiteralPath $storagePath -Raw
    @('OpenSearch', 'Prometheus', 'TelemetrySignalKind.Metrics') | ForEach-Object {
        if ($storage -notmatch [regex]::Escape($_)) { throw "Observability storage invariant '$($_)' is missing." }
    }

    $query = Get-Content -LiteralPath $queryPath -Raw
    @('observability.read', 'TenantId', 'EnvironmentName', 'MaximumClassification', 'Purpose') | ForEach-Object {
        if ($query -notmatch [regex]::Escape($_)) { throw "Central observability authorization '$($_)' is missing." }
    }

    $service = Get-Content -LiteralPath $servicePath -Raw
    @('outside the authorized scope', 'RedactedAttributes', 'CorrelationTraceId', 'EnterpriseObjectReferences') | ForEach-Object {
        if ($service -notmatch [regex]::Escape($_)) { throw "Central observability query invariant '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 17) {
    $requestPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Registration\AutomaticRegistrationRequest.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Registration\AutomaticRegistrationEngine.cs'
    $repositoryPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Registration\IAutomaticRegistrationRepository.cs'
    $proposalPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Registration\AutomaticRegistrationProposal.cs'
    $commitPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Registration\AutomaticRegistrationCommit.cs'
    if (-not (Test-Path -LiteralPath $requestPath) -or -not (Test-Path -LiteralPath $enginePath) -or
        -not (Test-Path -LiteralPath $repositoryPath) -or -not (Test-Path -LiteralPath $proposalPath) -or
        -not (Test-Path -LiteralPath $commitPath)) {
        throw 'Automatic registration artifacts are missing.'
    }

    $request = Get-Content -LiteralPath $requestPath -Raw
    @('ArtifactDigest', 'RegistryReference', 'DeploymentEvidenceReference', 'SupplyChainEvidenceReference',
      'ObservabilityEvidenceReference', 'HumanApprovalReference', 'PolicyReferences', 'PermittedActions') | ForEach-Object {
        if ($request -notmatch [regex]::Escape($_)) { throw "Automatic registration input '$($_)' is missing." }
    }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('RegisterAtomicallyAsync', 'RequestFingerprint', 'SHA256.HashData', 'RelationshipKnowledgeState.Confirmed',
      'automatic-registration', 'LifecycleState.Active', 'RegistrationDisposition') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Automatic registration invariant '$($_)' is missing." }
    }

    $repository = Get-Content -LiteralPath $repositoryPath -Raw
    if ($repository -notmatch 'RegisterAtomicallyAsync') { throw 'Atomic registration repository boundary is missing.' }

    $commit = Get-Content -LiteralPath $commitPath -Raw
    @('RegistrationDisposition', 'EvidenceReference', 'CommittedAt') | ForEach-Object {
        if ($commit -notmatch [regex]::Escape($_)) { throw "Registration commit evidence '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 18) {
    $requestPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Understanding\UnderstandingRequest.cs'
    $factPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Understanding\UnderstandingFact.cs'
    $candidatePath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Understanding\UnderstandingCandidate.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Understanding\GovernedUnderstandingEngine.cs'
    $contextPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Understanding\IUnderstandingContextProvider.cs'
    $analyzerPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Understanding\IUnderstandingAnalyzer.cs'
    if (-not (Test-Path -LiteralPath $requestPath) -or -not (Test-Path -LiteralPath $factPath) -or
        -not (Test-Path -LiteralPath $candidatePath) -or -not (Test-Path -LiteralPath $enginePath) -or
        -not (Test-Path -LiteralPath $contextPath) -or -not (Test-Path -LiteralPath $analyzerPath)) {
        throw 'Understanding Engine artifacts are missing.'
    }

    $request = Get-Content -LiteralPath $requestPath -Raw
    @('enterprise.understanding.read', 'ObjectScope', 'MaximumClassification', 'Purpose') | ForEach-Object {
        if ($request -notmatch [regex]::Escape($_)) { throw "Understanding authorization '$($_)' is missing." }
    }

    $fact = Get-Content -LiteralPath $factPath -Raw
    @('RelationshipKnowledgeState', 'EvidenceReferences', 'EnterpriseObjectReferences', 'Unknown facts cannot assert confidence') | ForEach-Object {
        if ($fact -notmatch [regex]::Escape($_)) { throw "Understanding fact invariant '$($_)' is missing." }
    }

    $candidate = Get-Content -LiteralPath $candidatePath -Raw
    if ($candidate -notmatch 'IsExecutable\s*=>\s*false') { throw 'Understanding candidates must remain non-executable.' }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('LoadAuthorizedSnapshotAsync', 'AnalyzeAsync', 'outside the authorized request scope',
      'Confirmed and discovered claims must match grounded facts exactly',
      'Inferred claims require grounded supporting facts', 'cannot downgrade source classification',
      'summary cannot downgrade its claims or exceed authorization') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Understanding Engine guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 19) {
    $definitionPath = Join-Path $repositoryRoot 'backend\Platform.AgenticWork\Execution\AgenticWorkDefinition.cs'
    $statePath = Join-Path $repositoryRoot 'backend\Platform.AgenticWork\Execution\AgenticWorkState.cs'
    $storePath = Join-Path $repositoryRoot 'backend\Platform.AgenticWork\Execution\IDurableAgenticWorkStore.cs'
    $runtimePath = Join-Path $repositoryRoot 'backend\Platform.AgenticWork\Execution\IAgentRuntime.cs'
    $resultPath = Join-Path $repositoryRoot 'backend\Platform.AgenticWork\Execution\AgentStepResult.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.AgenticWork\Execution\DurableAgenticWorkEngine.cs'
    $approvalPath = Join-Path $repositoryRoot 'backend\Platform.AgenticWork\Execution\AgenticWorkApproval.cs'
    $resumePath = Join-Path $repositoryRoot 'backend\Platform.AgenticWork\Execution\AgenticWorkResume.cs'
    if (-not (Test-Path -LiteralPath $definitionPath) -or -not (Test-Path -LiteralPath $statePath) -or
        -not (Test-Path -LiteralPath $storePath) -or -not (Test-Path -LiteralPath $runtimePath) -or
        -not (Test-Path -LiteralPath $resultPath) -or -not (Test-Path -LiteralPath $enginePath) -or
        -not (Test-Path -LiteralPath $approvalPath) -or -not (Test-Path -LiteralPath $resumePath)) {
        throw 'Agentic Work System artifacts are missing.'
    }

    $definition = Get-Content -LiteralPath $definitionPath -Raw
    @('TenantId', 'InitiatorSubjectId', 'PolicyReferences', 'EvidenceReferences', 'Steps',
      'contiguous and zero-based') | ForEach-Object {
        if ($definition -notmatch [regex]::Escape($_)) { throw "Agentic work definition '$($_)' is missing." }
    }

    $state = Get-Content -LiteralPath $statePath -Raw
    @('AwaitingApproval', 'Ready', 'Running', 'Suspended', 'Completed', 'Failed', 'Cancelled') | ForEach-Object {
        if ($state -notmatch "\b$($_)\b") { throw "Agentic work state '$($_)' is missing." }
    }

    $store = Get-Content -LiteralPath $storePath -Raw
    @('CreateAtomicallyAsync', 'LoadAsync', 'AppendAtomicallyAsync') | ForEach-Object {
        if ($store -notmatch [regex]::Escape($_)) { throw "Durable agentic store operation '$($_)' is missing." }
    }

    $result = Get-Content -LiteralPath $resultPath -Raw
    if ($result -notmatch 'IsExternallyEffecting\s*=>\s*false') {
        throw 'Phase 19 agent results must remain non-effecting.'
    }

    $resume = Get-Content -LiteralPath $resumePath -Raw
    if ($resume -notmatch 'agentic\.work\.resume') { throw 'Agentic work resume permission is missing.' }

    $approval = Get-Content -LiteralPath $approvalPath -Raw
    if ($approval -notmatch 'agentic\.work\.approve') { throw 'Agentic work approval permission is missing.' }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('AgenticWorkState.AwaitingApproval', 'separation of duties', 'idempotencyKey',
      'AgenticWorkState.Running', 'DurableCheckpointReference', 'ValidatePersisted',
      'SequenceEqual(expected.Definition.Steps)', 'Agentic work is not ready or resumable',
      'externally effecting result') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Durable agentic guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 20) {
    $actionRequestPath = Join-Path $repositoryRoot 'backend\Platform.Governance\GovernedActions\GovernedActionRequest.cs'
    $gatewayPath = Join-Path $repositoryRoot 'backend\Platform.Governance\GovernedActions\GovernedActionGateway.cs'
    $policyBundlePath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\SignedPolicyBundleReference.cs'
    $policyVerifierPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\IPolicyBundleVerifier.cs'
    $opaPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\IOpaPolicyDecisionPoint.cs'
    $evidencePath = Join-Path $repositoryRoot 'backend\Platform.Governance\Evidence\IGovernanceEvidenceJournal.cs'
    $mcpBindingPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Mcp\McpToolBinding.cs'
    $mcpExecutorPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Mcp\McpGovernedActionExecutor.cs'
    @($actionRequestPath, $gatewayPath, $policyBundlePath, $policyVerifierPath, $opaPath,
      $evidencePath, $mcpBindingPath, $mcpExecutorPath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 20 artifact is missing: $_" }
    }

    $actionRequest = Get-Content -LiteralPath $actionRequestPath -Raw
    @('governance.action.execute', 'separation of duties', 'ApprovalEvidenceReference',
      'Policy bundle environment does not match') | ForEach-Object {
        if ($actionRequest -notmatch [regex]::Escape($_)) { throw "Governed action guard '$($_)' is missing." }
    }

    $policyBundle = Get-Content -LiteralPath $policyBundlePath -Raw
    @('Version', 'Sha256Digest', 'SignatureReference', 'Environment', 'ActivatedAt') | ForEach-Object {
        if ($policyBundle -notmatch [regex]::Escape($_)) { throw "Signed policy field '$($_)' is missing." }
    }

    $gateway = Get-Content -LiteralPath $gatewayPath -Raw
    @('VerifyAsync', 'EvaluateAsync', 'SignatureValid', 'OpaDecisionOutcome.Permit',
      'GovernanceEvidenceStage.ActionIntent', 'action_denied_fail_closed', 'idempotencyKey',
      'ValidateResult') | ForEach-Object {
        if ($gateway -notmatch [regex]::Escape($_)) { throw "Governance gateway boundary '$($_)' is missing." }
    }

    $mcpBinding = Get-Content -LiteralPath $mcpBindingPath -Raw
    @('TenantId', 'Environment', 'InputSchemaSha256Digest', 'MaximumClassification', 'Enabled') | ForEach-Object {
        if ($mcpBinding -notmatch [regex]::Escape($_)) { throw "MCP binding guard '$($_)' is missing." }
    }

    $mcpExecutor = Get-Content -LiteralPath $mcpExecutorPath -Raw
    @('AuthorizedActionCommand', 'No governed MCP tool binding exists', 'command.Classification',
      'ValidateMcpResult', 'InputSchemaSha256Digest', 'IdempotencyKey') | ForEach-Object {
        if ($mcpExecutor -notmatch [regex]::Escape($_)) { throw "MCP action guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 21) {
    $modelingRequestPath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Impact\EnterpriseModelingRequest.cs'
    $snapshotProviderPath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Impact\IEnterpriseModelSnapshotProvider.cs'
    $impactPath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Impact\EnterpriseImpact.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Impact\EnterpriseImpactAnalysisEngine.cs'
    @($modelingRequestPath, $snapshotProviderPath, $impactPath, $enginePath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 21 artifact is missing: $_" }
    }

    $request = Get-Content -LiteralPath $modelingRequestPath -Raw
    @('enterprise.modeling.analyze', 'AuthorizedObjectScope', 'MaximumClassification',
      'MaximumTraversalDepth', 'Change.TargetObjectId') | ForEach-Object {
        if ($request -notmatch [regex]::Escape($_)) { throw "Modeling request guard '$($_)' is missing." }
    }

    $impact = Get-Content -LiteralPath $impactPath -Raw
    @('ConfirmedRelationship', 'DiscoveredRelationship', 'InferredRelationship',
      'UnknownRelationship', 'Confidence', 'EvidenceReferences') | ForEach-Object {
        if ($impact -notmatch [regex]::Escape($_)) { throw "Impact knowledge field '$($_)' is missing." }
    }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('LoadAuthorizedSnapshotAsync', 'ValidateSnapshot', 'exceeded authorized scope',
      'excludedRelationshipCount', 'MaximumTraversalDepth', 'does not simulate outcomes',
      'RelationshipKnowledgeState.Confirmed') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Enterprise impact guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 22) {
    $scenarioPath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Simulation\SimulationScenario.cs'
    $requestPath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Simulation\EnterpriseSimulationRequest.cs'
    $twinPath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Simulation\DigitalTwinSnapshot.cs'
    $isolationPath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Simulation\SimulationIsolationProfile.cs'
    $resultPath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Simulation\SimulationResult.cs'
    $storePath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Simulation\ISimulationRunStore.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.Modeling\Simulation\EnterpriseSimulationEngine.cs'
    @($scenarioPath, $requestPath, $twinPath, $isolationPath, $resultPath, $storePath, $enginePath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 22 artifact is missing: $_" }
    }

    $scenario = Get-Content -LiteralPath $scenarioPath -Raw
    @('RecoveryPlanReference', 'RecoveryPlanVersion', 'RecoveryPlanSha256Digest',
      'RecoveryEvidenceReferences', 'Perturbations') | ForEach-Object {
        if ($scenario -notmatch [regex]::Escape($_)) { throw "Resilience scenario field '$($_)' is missing." }
    }

    $request = Get-Content -LiteralPath $requestPath -Raw
    @('enterprise.simulation.run', 'AuthorizedObjectScope', 'MaximumClassification',
      'Every perturbation target must be inside authorized scope') | ForEach-Object {
        if ($request -notmatch [regex]::Escape($_)) { throw "Simulation request guard '$($_)' is missing." }
    }

    $isolation = Get-Content -LiteralPath $isolationPath -Raw
    @('HasProductionCredentials => false', 'AllowsExternalEffects => false',
      'SimulationNetworkAccess.None') | ForEach-Object {
        if ($isolation -notmatch [regex]::Escape($_)) { throw "Simulation isolation '$($_)' is missing." }
    }

    $result = Get-Content -LiteralPath $resultPath -Raw
    @('IsExternallyEffecting => false', 'IsAuthoritativeDecision => false',
      'RecoveryAssessmentReference') | ForEach-Object {
        if ($result -notmatch [regex]::Escape($_)) { throw "Simulation result boundary '$($_)' is missing." }
    }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('LoadAuthorizedIsolatedSnapshotAsync', 'ValidateDigitalTwin', 'IsProductionConnected',
      'CreateAtomicallyAsync', 'CompleteAtomicallyAsync', 'idempotencyKey',
      'changed the governed assumptions', 'exceeded authorized scope', 'ValidatePersisted') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Simulation engine guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 23) {
    $requestPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Intelligence\ProactiveIntelligenceRequest.cs'
    $signalPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Intelligence\EnterpriseOperationalSignal.cs'
    $snapshotPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Intelligence\ProactiveIntelligenceSnapshot.cs'
    $reportPath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Intelligence\ProactiveIntelligenceReport.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.EnterpriseModel\Intelligence\ProactiveIntelligenceEngine.cs'
    @($requestPath, $signalPath, $snapshotPath, $reportPath, $enginePath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 23 artifact is missing: $_" }
    }

    $request = Get-Content -LiteralPath $requestPath -Raw
    @('enterprise.intelligence.evaluate', 'AuthorizedObjectScope', 'MaximumClassification',
      'DetectionPolicy.Environment', 'WindowEnd <= WindowStart', 'RequestedAt < WindowEnd') | ForEach-Object {
        if ($request -notmatch [regex]::Escape($_)) { throw "Proactive request guard '$($_)' is missing." }
    }

    $signal = Get-Content -LiteralPath $signalPath -Raw
    @('TraceId', 'Classification', 'EvidenceReferences', 'ObservedAt') | ForEach-Object {
        if ($signal -notmatch [regex]::Escape($_)) { throw "Operational signal field '$($_)' is missing." }
    }

    $report = Get-Content -LiteralPath $reportPath -Raw
    @('IsExternallyEffecting => false', 'RequiresHumanReview => true',
      'RequiresGovernanceForAction') | ForEach-Object {
        if ($report -notmatch [regex]::Escape($_)) { throw "Proactive finding boundary '$($_)' is missing." }
    }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('LoadAuthorizedContextAsync', 'PolicySignatureValid', 'exceeded authorized object scope',
      'unauthorized signal', 'outside authorized context', 'duplicate findings',
      'SHA256.HashData', 'RecommendGovernedAction') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Proactive intelligence guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 24) {
    $jurisdictionPath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Productization\JurisdictionProfile.cs'
    $controlPath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Productization\ComplianceControlMapping.cs'
    $manifestPath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Productization\GovernmentProductManifest.cs'
    $requestPath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Productization\GovernmentProductizationRequest.cs'
    $servicePath = Join-Path $repositoryRoot 'backend\Platform.Infrastructure\Productization\GovernmentProductizationService.cs'
    @($jurisdictionPath, $controlPath, $manifestPath, $requestPath, $servicePath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 24 artifact is missing: $_" }
    }

    $jurisdiction = Get-Content -LiteralPath $jurisdictionPath -Raw
    @('DataResidencyReference', 'SupportedLanguages', 'MaximumClassification', 'AllowedTopologies',
      'IdentityAuthorityReference', 'PolicyAuthorityReference', 'TrustBundleReference',
      'RequiredComplianceControls') | ForEach-Object {
        if ($jurisdiction -notmatch [regex]::Escape($_)) { throw "Jurisdiction field '$($_)' is missing." }
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw
    @('ManifestSha256Digest', 'SignatureReference', 'ExternalLicenseCheckRequired',
      'ExternalTelemetryRequired', 'SupportsOfflineInstallation', 'Artifacts', 'ComplianceControls') | ForEach-Object {
        if ($manifest -notmatch [regex]::Escape($_)) { throw "Government manifest field '$($_)' is missing." }
    }

    $request = Get-Content -LiteralPath $requestPath -Raw
    @('government.product.publish', 'separation of duties', 'AllowedTopologies', 'DeploymentProfile.TenantId') | ForEach-Object {
        if ($request -notmatch [regex]::Escape($_)) { throw "Productization request guard '$($_)' is missing." }
    }

    $service = Get-Content -LiteralPath $servicePath -Raw
    @('ValidateComplianceCoverage', 'RequiredComplianceControls.IsSubsetOf', 'VerifyAsync',
      'SignatureValid', 'TrustBundleReference', 'RegisterAtomicallyAsync', 'ValidateRegistered') | ForEach-Object {
        if ($service -notmatch [regex]::Escape($_)) { throw "Government product guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 25) {
    $contextPath = Join-Path $repositoryRoot 'frontend\Platform.Web\Foundation\ExperienceContext.cs'
    $authorizedContextPath = Join-Path $repositoryRoot 'frontend\Platform.Web\FrontDoor\GovernedExperienceContext.cs'
    $destinationPath = Join-Path $repositoryRoot 'frontend\Platform.Web\FrontDoor\FrontDoorDestination.cs'
    $catalogPath = Join-Path $repositoryRoot 'frontend\Platform.Web\FrontDoor\FrontDoorCatalog.cs'
    $homePath = Join-Path $repositoryRoot 'frontend\Platform.Web\Pages\Home.razor'
    $navPath = Join-Path $repositoryRoot 'frontend\Platform.Web\Layout\NavMenu.razor'
    $cssPath = Join-Path $repositoryRoot 'frontend\Platform.Web\wwwroot\css\app.css'
    @($contextPath, $authorizedContextPath, $destinationPath, $catalogPath, $homePath, $navPath, $cssPath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 25 artifact is missing: $_" }
    }

    $context = Get-Content -LiteralPath $contextPath -Raw
    @('IsGovernedIdentityEstablished { get; private set; }', 'ApplyServerAuthorizedContext',
      'AuthorizationEvidenceReference', 'Permissions.Contains', 'public void Clear') | ForEach-Object {
        if ($context -notmatch [regex]::Escape($_)) { throw "Front Door context guard '$($_)' is missing." }
    }

    $authorizedContext = Get-Content -LiteralPath $authorizedContextPath -Raw
    @('TenantId', 'Purpose', 'Permissions.IsEmpty', 'AuthorizationEvidenceReference',
      'ExpiresAt <= IssuedAt', 'now >= ExpiresAt') | ForEach-Object {
        if ($authorizedContext -notmatch [regex]::Escape($_)) { throw "Authorized experience guard '$($_)' is missing." }
    }

    $catalog = (Get-Content -LiteralPath $destinationPath -Raw) + (Get-Content -LiteralPath $catalogPath -Raw)
    @('BUILD', 'UNDERSTAND', 'OPERATE', 'ACT', 'PROVE', 'RequiredPermission',
      'frontdoor.act.request', 'frontdoor.evidence.read') | ForEach-Object {
        if ($catalog -notmatch [regex]::Escape($_)) { throw "Front Door destination '$($_)' is missing." }
    }

    $homeContent = Get-Content -LiteralPath $homePath -Raw
    @('Governed identity required', 'ExperienceContext.CanAccess', 'disabled="@(!permitted)"',
      'Server-side authorization remains authoritative', 'aria-labelledby') | ForEach-Object {
        if ($homeContent -notmatch [regex]::Escape($_)) { throw "Front Door UI guard '$($_)' is missing." }
    }

    $nav = Get-Content -LiteralPath $navPath -Raw
    if ($nav -notmatch 'API re-authorizes every operation') { throw 'Front Door navigation assurance is missing.' }
    $css = Get-Content -LiteralPath $cssPath -Raw
    @('focus-visible', 'prefers-reduced-motion', '@media (max-width: 800px)', 'button:disabled') | ForEach-Object {
        if ($css -notmatch [regex]::Escape($_)) { throw "Front Door accessibility style '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 26) {
    $templatePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\DeveloperExperience\ApprovedDeveloperTemplate.cs'
    $requestPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\DeveloperExperience\DeveloperWorkspaceRequest.cs'
    $environmentPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\DeveloperExperience\DeveloperEnvironmentSnapshot.cs'
    $planPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\DeveloperExperience\DeveloperWorkspacePlan.cs'
    $servicePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\DeveloperExperience\GovernedDeveloperExperienceService.cs'
    @($templatePath, $requestPath, $environmentPath, $planPath, $servicePath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 26 artifact is missing: $_" }
    }

    $template = Get-Content -LiteralPath $templatePath -Raw
    @('Sha256Digest', 'SignatureReference', 'ArchitectureReference', 'RequiredDotNetSdkVersion',
      'ApprovedPackageReferences', 'EvidenceReferences') | ForEach-Object {
        if ($template -notmatch [regex]::Escape($_)) { throw "Developer template field '$($_)' is missing." }
    }

    $request = Get-Content -LiteralPath $requestPath -Raw
    @('developer.workspace.bootstrap', 'Path.IsPathRooted', 'Contains("..")',
      'LocalPackageSourceReference', 'GitRepositoryReference') | ForEach-Object {
        if ($request -notmatch [regex]::Escape($_)) { throw "Developer workspace guard '$($_)' is missing." }
    }

    $plan = Get-Content -LiteralPath $planPath -Raw
    @('RestoreLockedDependencies', 'VerifyProject', 'ReviewChanges', 'SubmitToGitAndCi',
      'IsProductionDeploymentCapable => false', 'RequiresHumanReview => true') | ForEach-Object {
        if ($plan -notmatch [regex]::Escape($_)) { throw "Developer plan boundary '$($_)' is missing." }
    }

    $service = Get-Content -LiteralPath $servicePath -Raw
    @('VerifyAsync', 'InspectAsync', 'HasProductionCredentials', 'OutboundNetworkRequired',
      '--locked-mode', 'scripts/verify-project.ps1', 'RegisterAtomicallyAsync', 'ValidateRegistered') | ForEach-Object {
        if ($service -notmatch [regex]::Escape($_)) { throw "Developer experience guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 27) {
    $requestPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\ClosedLoop\ClosedLoopEvaluationRequest.cs'
    $contextPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\ClosedLoop\ClosedLoopContext.cs'
    $proposalPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\ClosedLoop\ImprovementProposal.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\ClosedLoop\ClosedLoopEngine.cs'
    @($requestPath, $contextPath, $proposalPath, $enginePath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 27 artifact is missing: $_" }
    }

    $request = Get-Content -LiteralPath $requestPath -Raw
    @('software.closedloop.evaluate', 'EnterpriseObjectReference', 'ReleaseArtifactSha256Digest',
      'ReleaseProvenanceReference', 'ObservationWindowEnd <= ObservationWindowStart', 'RequestedAt < ObservationWindowEnd') | ForEach-Object {
        if ($request -notmatch [regex]::Escape($_)) { throw "Closed-loop request guard '$($_)' is missing." }
    }

    $context = Get-Content -LiteralPath $contextPath -Raw
    @('DeliveryEvidenceReferences', 'RegistrationEvidenceReferences', 'TelemetryEvidenceReferences',
      'PolicyVerificationEvidenceReference', 'PolicySignatureValid') | ForEach-Object {
        if ($context -notmatch [regex]::Escape($_)) { throw "Closed-loop context field '$($_)' is missing." }
    }

    $proposal = Get-Content -LiteralPath $proposalPath -Raw
    @('IsExternallyEffecting => false', 'RequiresHumanReview => true',
      'RequiresNewSoftwareDeliveryRun => true') | ForEach-Object {
        if ($proposal -notmatch [regex]::Escape($_)) { throw "Improvement proposal boundary '$($_)' is missing." }
    }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('LoadAuthorizedContextAsync', 'PolicySignatureValid', 'outside the closed-loop context',
      'SHA256.HashData', 'duplicate improvement intents', 'CreateAtomicallyAsync', 'ValidatePersisted') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Closed-loop engine guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 28) {
    $architecturePath = Join-Path $repositoryRoot 'architecture\system-architecture.v2.json'
    $architectureDocumentPath = Join-Path $repositoryRoot 'docs\phase-28\FINAL_SYSTEM_ARCHITECTURE.md'
    $conformancePath = Join-Path $repositoryRoot 'docs\phase-28\ARCHITECTURE_CONFORMANCE_MATRIX.md'
    @($architecturePath, $architectureDocumentPath, $conformancePath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 28 artifact is missing: $_" }
    }

    $architecture = Get-Content -LiteralPath $architecturePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $approvedArchitectureVersion = 'PROJECT MASTER SPECIFICATION v2 ' + [char]0x2014 + ' APPROVED'
    if ($architecture.architectureVersion -ne $approvedArchitectureVersion -or
        $architecture.style -ne 'ASP.NET Core modular monolith' -or
        $architecture.backendTarget -ne 'net10.0' -or
        $architecture.frontend -ne 'Blazor WebAssembly net10.0') {
        throw 'Final architecture baseline does not match the approved specification.'
    }
    if ($architecture.modules.Count -ne 15 -or
        ($architecture.modules.name | Sort-Object -Unique).Count -ne 15) {
        throw 'Final architecture must record exactly 15 unique project boundaries.'
    }
    @('Platform.Api', 'Platform.AgenticWork', 'Platform.Application', 'Platform.Domain',
      'Platform.EnterpriseModel', 'Platform.Evidence', 'Platform.Governance', 'Platform.Identity',
      'Platform.Infrastructure', 'Platform.Integrations', 'Platform.Knowledge', 'Platform.Modeling',
      'Platform.Observability', 'Platform.SoftwareFactory', 'Platform.Web') | ForEach-Object {
        if ($architecture.modules.name -notcontains $_) { throw "Final architecture module '$($_)' is missing." }
    }
    @('No direct AI to production path', 'AI runtime is not policy authority',
      'Retrieval is authorized before access and re-authorized before AI context',
      'No numerical SLO before workload benchmarking') | ForEach-Object {
        if ($architecture.invariants -notcontains $_) { throw "Final architecture invariant '$($_)' is missing." }
    }
    @('operatingModel', 'retrievalAuthorization', 'agenticGovernance', 'softwareFactory',
      'observability', 'evidence') | ForEach-Object {
        if ($architecture.flows.PSObject.Properties.Name -notcontains $_) { throw "Final architecture flow '$($_)' is missing." }
    }
    if ($architecture.technologyBaseline.vectorStores -ne 'pgvector or Qdrant conditional only') {
        throw 'Conditional vector stores were promoted without Change Control.'
    }
}

if ($phaseNumber -ge 29) {
    $stagePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Delivery\DeliveryStage.cs'
    $requestPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\VerticalSlice\InternalServiceVerticalSliceRequest.cs'
    $storePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\VerticalSlice\IVerticalSliceRunStore.cs'
    $runPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\VerticalSlice\VerticalSliceRun.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\VerticalSlice\InternalServiceVerticalSliceEngine.cs'
    @($stagePath, $requestPath, $storePath, $runPath, $enginePath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 29 artifact is missing: $_" }
    }

    $expectedStages = @('Intent', 'EnterpriseContext', 'ExistingSystems', 'ExistingArchitecture',
        'ApprovedPackages', 'AiPlanning', 'CodeGeneration', 'StaticValidation', 'SecurityValidation',
        'Sandbox', 'Tests', 'HumanReview', 'Git', 'CiCd', 'Artifact', 'Deployment', 'OpenTelemetry',
        'AutomaticRegistration', 'EnterpriseModel', 'Evidence')
    $stageContent = Get-Content -LiteralPath $stagePath -Raw
    $actualStages = [regex]::Matches($stageContent, '(?m)^\s{4}([A-Za-z][A-Za-z0-9]*),?\s*$') |
        ForEach-Object { $_.Groups[1].Value }
    if (($actualStages -join '|') -ne ($expectedStages -join '|')) {
        throw 'Vertical slice stages do not match the exact approved sequence.'
    }

    $request = Get-Content -LiteralPath $requestPath -Raw
    @('developer.internal-service.create', 'EnterpriseContextReferences', 'ExistingSystemReferences',
      'ExistingArchitectureReference', 'ApprovedPackageReferences', 'IntentEvidenceReferences') | ForEach-Object {
        if ($request -notmatch [regex]::Escape($_)) { throw "Vertical slice request guard '$($_)' is missing." }
    }

    $store = Get-Content -LiteralPath $storePath -Raw
    @('CreateAtomicallyAsync', 'LoadAsync', 'AppendAtomicallyAsync', 'expectedVersion') | ForEach-Object {
        if ($store -notmatch [regex]::Escape($_)) { throw "Vertical slice store contract '$($_)' is missing." }
    }

    $run = Get-Content -LiteralPath $runPath -Raw
    @('Version != Receipts.Length', 'SequenceEqual', 'DeliveryStage.Evidence') | ForEach-Object {
        if ($run -notmatch [regex]::Escape($_)) { throw "Vertical slice run invariant '$($_)' is missing." }
    }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('StartAsync', 'AdvanceAsync', 'RunToCompletionAsync', 'vertical-slice:',
      'Only the governed deployment stage may record an external effect',
      'policy gate is required', 'human approval is required', 'separation of duties',
      'Verified supply chain is required', 'OpenTelemetry evidence is required',
      'Automatic Enterprise Model registration is required', 'ValidatePersisted',
      'EquivalentRequest', 'EquivalentReceipt') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Vertical slice engine guard '$($_)' is missing." }
    }
}

if ($phaseNumber -ge 30) {
    $stagePath = Join-Path $repositoryRoot 'backend\Platform.Evidence\Chain\EvidenceStage.cs'
    $appendPath = Join-Path $repositoryRoot 'backend\Platform.Evidence\Chain\EvidenceAppendRequest.cs'
    $verifyPath = Join-Path $repositoryRoot 'backend\Platform.Evidence\Chain\EvidenceVerificationRequest.cs'
    $entryPath = Join-Path $repositoryRoot 'backend\Platform.Evidence\Chain\EvidenceEntry.cs'
    $signaturePath = Join-Path $repositoryRoot 'backend\Platform.Evidence\Chain\SignatureEnvelope.cs'
    $accessPath = Join-Path $repositoryRoot 'backend\Platform.Evidence\Chain\EvidenceAuthorizationDecision.cs'
    $storePath = Join-Path $repositoryRoot 'backend\Platform.Evidence\Chain\IEvidenceChainStore.cs'
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.Evidence\Chain\CryptographicEvidenceEngine.cs'
    $proofPath = Join-Path $repositoryRoot 'backend\Platform.Evidence\Chain\EvidenceProofReport.cs'
    $registrationPath = Join-Path $repositoryRoot 'backend\Platform.Evidence\EvidenceServiceCollectionExtensions.cs'
    $programPath = Join-Path $repositoryRoot 'backend\Platform.Api\Program.cs'
    @($stagePath, $appendPath, $verifyPath, $entryPath, $signaturePath, $accessPath, $storePath, $enginePath, $proofPath, $registrationPath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Phase 30 artifact is missing: $_" }
    }

    $expectedStages = @('Request', 'Context', 'Knowledge', 'Decision', 'Policy',
        'Approval', 'Action', 'Result', 'Telemetry', 'Evidence')
    $stageContent = Get-Content -LiteralPath $stagePath -Raw
    $actualStages = [regex]::Matches($stageContent, '(?m)^\s{4}([A-Za-z][A-Za-z0-9]*),?\s*$') |
        ForEach-Object { $_.Groups[1].Value }
    if (($actualStages -join '|') -ne ($expectedStages -join '|')) {
        throw 'Evidence stages do not match the exact approved sequence.'
    }

    $append = Get-Content -LiteralPath $appendPath -Raw
    @('evidence.append', 'TenantId', 'CorrelationId', 'Classification', 'Purpose',
      'PayloadSha256Digest', 'TraceReferences') | ForEach-Object {
        if ($append -notmatch [regex]::Escape($_)) { throw "Evidence append guard '$($_)' is missing." }
    }
    $verify = Get-Content -LiteralPath $verifyPath -Raw
    @('evidence.verify', 'MaximumAuthorizedClassification', 'Purpose', 'TenantId') | ForEach-Object {
        if ($verify -notmatch [regex]::Escape($_)) { throw "Evidence verification guard '$($_)' is missing." }
    }

    $entry = Get-Content -LiteralPath $entryPath -Raw
    @('PreviousEntrySha256Digest', 'EntrySha256Digest', 'SignatureEnvelope',
      'AuthorizationEvidenceReference', 'ValidateShape') | ForEach-Object {
        if ($entry -notmatch [regex]::Escape($_)) { throw "Evidence entry field '$($_)' is missing." }
    }
    $signature = Get-Content -LiteralPath $signaturePath -Raw
    @('IEvidenceSigner', 'IEvidenceSignatureVerifier', 'Algorithm', 'KeyId',
      'SignatureBase64', 'CertificateChainReference', 'SignedAt') | ForEach-Object {
        if ($signature -notmatch [regex]::Escape($_)) { throw "Evidence signature contract '$($_)' is missing." }
    }

    $access = Get-Content -LiteralPath $accessPath -Raw
    @('AuthorizeAppendAsync', 'AuthorizeVerificationAsync', 'AuthorizeClassificationAsync', 'Demand') | ForEach-Object {
        if ($access -notmatch [regex]::Escape($_)) { throw "Evidence access guard '$($_)' is missing." }
    }
    $store = Get-Content -LiteralPath $storePath -Raw
    @('AppendAtomicallyAsync', 'expectedSequence', 'expectedPreviousEntrySha256Digest',
      'maximumAuthorizedClassification', 'authorizationEvidenceReference', 'LoadOrderedAsync') | ForEach-Object {
        if ($store -notmatch [regex]::Escape($_)) { throw "Evidence store contract '$($_)' is missing." }
    }
    if ($store -match '(?i)Delete|Update') { throw 'Evidence store must not expose update or deletion operations.' }

    $engine = Get-Content -LiteralPath $enginePath -Raw
    @('ApprovedSequence', 'exact next approved stage', 'GenesisSha256Digest', 'SHA256.HashData',
      'ComputeDigest', 'ValidateHeadAsync', 'signatureVerifier.VerifyAsync', 'ValidatePersisted',
      'signed append-only entry', 'chain_link_invalid', 'stage_order_invalid',
      'AuthorizeClassificationAsync') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Cryptographic evidence guard '$($_)' is missing." }
    }
    $proof = Get-Content -LiteralPath $proofPath -Raw
    @('IsComplete', 'RootSha256Digest', 'HeadSha256Digest', 'EntryProofs', 'Failures',
      'HashValid', 'SignatureValid', 'AuthorizationEvidenceReference') | ForEach-Object {
        if ($proof -notmatch [regex]::Escape($_)) { throw "Evidence proof field '$($_)' is missing." }
    }
    $registration = (Get-Content -LiteralPath $registrationPath -Raw) + (Get-Content -LiteralPath $programPath -Raw)
    @('AddPlatformEvidenceFoundation', 'AddScoped<CryptographicEvidenceEngine>') | ForEach-Object {
        if ($registration -notmatch [regex]::Escape($_)) { throw "Evidence registration '$($_)' is missing." }
    }

    $solutionContent = Get-Content -LiteralPath $solutionPath -Raw
    1..30 | ForEach-Object {
        $acceptanceReference = 'docs\phase-{0:D2}\PHASE_{0:D2}_ACCEPTANCE.md' -f $_
        if ($solutionContent -notmatch [regex]::Escape($acceptanceReference)) {
            throw "Visual Studio solution is missing '$acceptanceReference'."
        }
    }
    @('backend\Platform.Evidence\Platform.Evidence.csproj',
      'docs\phase-30\EVIDENCE_ENGINE.md', 'architecture\system-architecture.v2.json') | ForEach-Object {
        if ($solutionContent -notmatch [regex]::Escape($_)) { throw "Visual Studio solution item '$($_)' is missing." }
    }
}

$increment03AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_03_ACCEPTANCE.md'
if (Test-Path -LiteralPath $increment03AcceptancePath) {
    $intentRegistrationPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedIntentRegistration.cs'
    $intentEndpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    @($intentRegistrationPath, $intentEndpointPath, $readinessPath, $openApiPath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Operational Increment 03 artifact is missing: $_" }
    }

    $intentRegistration = Get-Content -LiteralPath $intentRegistrationPath -Raw
    @('developer.internal-service.intent.register', 'PolicySignatureValid',
      'PolicyVerificationEvidenceReference', 'GovernedIntentPolicyOutcome.Permit',
      'RegisterAtomicallyAsync', 'ExpectedVersion', 'idempotencyKey',
      'pending-atomic-registration', 'ValidatePersisted', 'CanAdvance: false',
      'OPA returned a mismatched intent decision') | ForEach-Object {
        if ($intentRegistration -notmatch [regex]::Escape($_)) {
            throw "Governed intent registration guard '$($_)' is missing."
        }
    }

    $intentEndpoint = Get-Content -LiteralPath $intentEndpointPath -Raw
    @('/api/v1/internal-services/intents/register', 'RequiredPermissions',
      'IGovernedIntentPolicyGate', 'IGovernedIntentRegistrationRepository',
      'Status503ServiceUnavailable', 'Status409Conflict', 'RequireAuthorization') | ForEach-Object {
        if ($intentEndpoint -notmatch [regex]::Escape($_)) {
            throw "Governed intent endpoint guard '$($_)' is missing."
        }
    }

    $readiness = Get-Content -LiteralPath $readinessPath -Raw
    @('Governed intent OPA policy gate', 'Governed intent atomic registration repository') | ForEach-Object {
        if ($readiness -notmatch [regex]::Escape($_)) {
            throw "Governed intent readiness dependency '$($_)' is missing."
        }
    }

    $openApi = Get-Content -LiteralPath $openApiPath -Raw
    @('/api/v1/internal-services/intents/register', 'GovernedIntentRegistrationInput',
      'GovernedIntentRegistrationReceipt', 'expectedVersion', '503') | ForEach-Object {
        if ($openApi -notmatch [regex]::Escape($_)) {
            throw "Governed intent OpenAPI boundary '$($_)' is missing."
        }
    }
}

$increment04AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_04_ACCEPTANCE.md'
if (Test-Path -LiteralPath $increment04AcceptancePath) {
    $contextEnginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\AuthorizedEnterpriseContextDiscovery.cs'
    $intentEndpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $softwareFactoryProjectPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Platform.SoftwareFactory.csproj'
    $changeControlPath = Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-01-ENTERPRISE-CONTEXT.md'
    @($contextEnginePath, $intentEndpointPath, $readinessPath, $openApiPath,
      $softwareFactoryProjectPath, $changeControlPath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Operational Increment 04 artifact is missing: $_" }
    }

    $changeControl = Get-Content -LiteralPath $changeControlPath -Raw
    @('Status: **Approved for Operational Increment 04**',
      'Decision: **Approved by the repository owner',
      'Operational Increment 04 — Authorized Enterprise Context Discovery') | ForEach-Object {
        if ($changeControl -notmatch [regex]::Escape($_)) {
            throw "Increment 04 Change Control approval '$($_)' is missing."
        }
    }

    $contextEngine = Get-Content -LiteralPath $contextEnginePath -Raw
    @('developer.internal-service.context.discover', 'IGovernedIntentRegistrationReader',
      'IEnterpriseContextPolicyGate', 'PolicySignatureValid',
      'PolicyVerificationEvidenceReference', 'AllowedResourceIds.IsEmpty',
      'AllowedModalities.IsEmpty', 'AuthorizedKnowledgeRetriever',
      'IsSubsetOf(_availableRetrievalModalities)', 'EnterpriseContextDependencyUnavailableException',
      'Knowledge retrieval released an out-of-scope Enterprise Context candidate',
      'IEnterpriseContextEvidenceRecorder', 'ContextSha256Digest',
      'CanAdvance: false', 'Separately approved Existing Systems discovery',
      'OPA returned a mismatched Enterprise Context decision') | ForEach-Object {
        if ($contextEngine -notmatch [regex]::Escape($_)) {
            throw "Enterprise Context discovery guard '$($_)' is missing."
        }
    }
    $decisionValidationIndex = $contextEngine.IndexOf('ValidateDecision(policyInput', [StringComparison]::Ordinal)
    $retrievalIndex = $contextEngine.IndexOf('_knowledgeRetriever.RetrieveAsync', [StringComparison]::Ordinal)
    if ($decisionValidationIndex -lt 0 -or $retrievalIndex -lt 0 -or $decisionValidationIndex -ge $retrievalIndex) {
        throw 'Enterprise Context retrieval is not structurally ordered after OPA decision validation.'
    }

    $intentEndpoint = Get-Content -LiteralPath $intentEndpointPath -Raw
    @('/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context',
      'developer.internal-service.context.discover', 'IGovernedIntentRegistrationReader',
      'IEnterpriseContextPolicyGate', 'IEnterpriseContextEvidenceRecorder',
      'GetServices<IKnowledgeRetrievalSource>', 'Status503ServiceUnavailable',
      'RequireAuthorization') | ForEach-Object {
        if ($intentEndpoint -notmatch [regex]::Escape($_)) {
            throw "Enterprise Context endpoint guard '$($_)' is missing."
        }
    }

    $readiness = Get-Content -LiteralPath $readinessPath -Raw
    @('Governed intent registration reader', 'Enterprise Context OPA policy gate',
      'Enterprise Context retrieval source', 'Enterprise Context evidence recorder') | ForEach-Object {
        if ($readiness -notmatch [regex]::Escape($_)) {
            throw "Enterprise Context readiness dependency '$($_)' is missing."
        }
    }

    $openApi = Get-Content -LiteralPath $openApiPath -Raw
    @('/api/v1/internal-services/intents/{registrationId}/enterprise-context',
      'EnterpriseContextDiscoveryInput', 'AuthorizedEnterpriseContextDiscoveryReceipt',
      'expectedRegistrationVersion', 'expectedIntentSha256Digest', 'canAdvance', '503') | ForEach-Object {
        if ($openApi -notmatch [regex]::Escape($_)) {
            throw "Enterprise Context OpenAPI boundary '$($_)' is missing."
        }
    }

    $softwareFactoryProject = Get-Content -LiteralPath $softwareFactoryProjectPath -Raw
    @('Platform.Identity\Platform.Identity.csproj', 'Platform.Knowledge\Platform.Knowledge.csproj') | ForEach-Object {
        if ($softwareFactoryProject -notmatch [regex]::Escape($_)) {
            throw "Software Factory module dependency '$($_)' is missing."
        }
    }
}

$increment05AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_05_ACCEPTANCE.md'
if (Test-Path -LiteralPath $increment05AcceptancePath) {
    $systemsEnginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\AuthorizedExistingSystemsDiscovery.cs'
    $inventoryContractPath = Join-Path $repositoryRoot 'backend\Platform.Integrations\ExistingSystems\ExistingSystemInventory.cs'
    $intentEndpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $softwareFactoryProjectPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Platform.SoftwareFactory.csproj'
    $integrationsProjectPath = Join-Path $repositoryRoot 'backend\Platform.Integrations\Platform.Integrations.csproj'
    $changeControlPath = Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-02-EXISTING-SYSTEMS.md'
    @($systemsEnginePath, $inventoryContractPath, $intentEndpointPath, $readinessPath,
      $openApiPath, $softwareFactoryProjectPath, $integrationsProjectPath, $changeControlPath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Operational Increment 05 artifact is missing: $_" }
    }

    $changeControl = Get-Content -LiteralPath $changeControlPath -Raw
    @('Status: **Approved for Operational Increment 05**',
      'Decision: **Approved by the repository owner',
      'Operational Increment 05 — Authorized Existing Systems Discovery') | ForEach-Object {
        if ($changeControl -notmatch [regex]::Escape($_)) {
            throw "Increment 05 Change Control approval '$($_)' is missing."
        }
    }

    $inventoryContract = Get-Content -LiteralPath $inventoryContractPath -Raw
    @('IExistingSystemInventorySource', 'ExistingSystemInventoryScope',
      'AllowedSystemIds', 'AllowedRelationshipTypes', 'AllowedSourceKinds',
      'CredentialsIncluded', 'LiveSessionIncluded', 'ExecutableCommandIncluded',
      'ExternalEffectOccurred') | ForEach-Object {
        if ($inventoryContract -notmatch [regex]::Escape($_)) {
            throw "Existing Systems inventory contract '$($_)' is missing."
        }
    }
    if ($inventoryContract -match '(?i)SaveAsync|UpdateAsync|DeleteAsync|ExecuteAsync') {
        throw 'Existing Systems inventory contract must not expose mutation or execution operations.'
    }

    $systemsEngine = Get-Content -LiteralPath $systemsEnginePath -Raw
    @('developer.internal-service.systems.discover', 'IAuthorizedEnterpriseContextSnapshotReader',
      'IExistingSystemsPolicyGate', 'PolicySignatureValid',
      'PolicyVerificationEvidenceReference', 'AllowedSystemIds.IsEmpty',
      'AllowedRelationshipTypes.IsEmpty', 'AllowedSourceKinds.IsEmpty',
      'IExistingSystemResultAuthorizer', 'existing-system.read',
      'existing-system.relationship.read', 'CredentialsIncluded', 'LiveSessionIncluded',
      'ExecutableCommandIncluded', 'ExternalEffectOccurred',
      'RelationshipKnowledgeState', 'IExistingSystemsEvidenceRecorder',
      'InventorySha256Digest', 'CanAdvance: false',
      'Separately approved Existing Architecture discovery',
      'OPA returned a mismatched Existing Systems decision') | ForEach-Object {
        if ($systemsEngine -notmatch [regex]::Escape($_)) {
            throw "Existing Systems discovery guard '$($_)' is missing."
        }
    }
    $decisionValidationIndex = $systemsEngine.IndexOf('ValidateDecision(policyInput', [StringComparison]::Ordinal)
    $sourceAccessIndex = $systemsEngine.IndexOf('var sourceResults = await Task.WhenAll', [StringComparison]::Ordinal)
    if ($decisionValidationIndex -lt 0 -or $sourceAccessIndex -lt 0 -or $decisionValidationIndex -ge $sourceAccessIndex) {
        throw 'Existing Systems inventory access is not structurally ordered after OPA decision validation.'
    }

    $intentEndpoint = Get-Content -LiteralPath $intentEndpointPath -Raw
    @('/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems',
      'developer.internal-service.systems.discover', 'IAuthorizedEnterpriseContextSnapshotReader',
      'IExistingSystemsPolicyGate', 'IExistingSystemResultAuthorizer',
      'IExistingSystemsEvidenceRecorder', 'GetServices<IExistingSystemInventorySource>',
      'Status503ServiceUnavailable', 'RequireAuthorization') | ForEach-Object {
        if ($intentEndpoint -notmatch [regex]::Escape($_)) {
            throw "Existing Systems endpoint guard '$($_)' is missing."
        }
    }

    $readiness = Get-Content -LiteralPath $readinessPath -Raw
    @('Authorized Enterprise Context snapshot reader', 'Existing Systems OPA policy gate',
      'Existing Systems inventory source', 'Existing Systems result authorizer',
      'Existing Systems evidence recorder') | ForEach-Object {
        if ($readiness -notmatch [regex]::Escape($_)) {
            throw "Existing Systems readiness dependency '$($_)' is missing."
        }
    }

    $openApi = Get-Content -LiteralPath $openApiPath -Raw
    @('/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems',
      'ExistingSystemsDiscoveryInput', 'AuthorizedExistingSystemsDiscoveryReceipt',
      'expectedContextSha256Digest', 'AuthorizedExistingSystemRelationship',
      'knowledgeState', 'canAdvance', '503') | ForEach-Object {
        if ($openApi -notmatch [regex]::Escape($_)) {
            throw "Existing Systems OpenAPI boundary '$($_)' is missing."
        }
    }

    $softwareFactoryProject = Get-Content -LiteralPath $softwareFactoryProjectPath -Raw
    @('Platform.EnterpriseModel\Platform.EnterpriseModel.csproj',
      'Platform.Integrations\Platform.Integrations.csproj') | ForEach-Object {
        if ($softwareFactoryProject -notmatch [regex]::Escape($_)) {
            throw "Software Factory module dependency '$($_)' is missing."
        }
    }
    $integrationsProject = Get-Content -LiteralPath $integrationsProjectPath -Raw
    if ($integrationsProject -notmatch [regex]::Escape('Platform.EnterpriseModel\Platform.EnterpriseModel.csproj')) {
        throw 'Integrations module is not bound to the approved Enterprise Model contract.'
    }
}

$increment06AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_06_ACCEPTANCE.md'
if (Test-Path -LiteralPath $increment06AcceptancePath) {
    $architectureEnginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\AuthorizedExistingArchitectureDiscovery.cs'
    $architectureContractPath = Join-Path $repositoryRoot 'backend\Platform.Integrations\ExistingArchitecture\ExistingArchitectureSource.cs'
    $intentEndpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changeControlPath = Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-03-EXISTING-ARCHITECTURE.md'
    @($architectureEnginePath, $architectureContractPath, $intentEndpointPath,
      $readinessPath, $openApiPath, $changeControlPath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Operational Increment 06 artifact is missing: $_" }
    }

    $changeControl = Get-Content -LiteralPath $changeControlPath -Raw
    @('Status: **Approved for Operational Increment 06**',
      'Decision: **Approved by the repository owner',
      'Operational Increment 06 — Authorized Existing Architecture Discovery') | ForEach-Object {
        if ($changeControl -notmatch [regex]::Escape($_)) {
            throw "Increment 06 Change Control approval '$($_)' is missing."
        }
    }

    $architectureContract = Get-Content -LiteralPath $architectureContractPath -Raw
    @('IExistingArchitectureSource', 'ExistingArchitectureSourceScope',
      'AllowedSystemIds', 'AllowedItemKinds', 'AllowedRelationshipTypes', 'AllowedSourceKinds',
      'ExistingArchitectureApprovalState', 'CredentialsIncluded', 'LiveSessionIncluded',
      'ExecutableCommandIncluded', 'GeneratedContentIncluded', 'ExternalEffectOccurred') | ForEach-Object {
        if ($architectureContract -notmatch [regex]::Escape($_)) {
            throw "Existing Architecture source contract '$($_)' is missing."
        }
    }
    if ($architectureContract -match '(?i)SaveAsync|UpdateAsync|DeleteAsync|ExecuteAsync') {
        throw 'Existing Architecture source contract must not expose mutation or execution operations.'
    }

    $architectureEngine = Get-Content -LiteralPath $architectureEnginePath -Raw
    @('developer.internal-service.architecture.discover', 'IAuthorizedExistingSystemsSnapshotReader',
      'IExistingArchitecturePolicyGate', 'PolicySignatureValid',
      'PolicyVerificationEvidenceReference', 'AllowedSystemIds.IsEmpty',
      'decision.RegistrationVersion != input.RegistrationVersion',
      'decision.InventorySha256Digest, input.InventorySha256Digest',
      'AllowedItemKinds.IsEmpty', 'AllowedRelationshipTypes.IsEmpty', 'AllowedSourceKinds.IsEmpty',
      'IExistingArchitectureConformanceValidator', 'ValidateScopeAsync', 'ValidateItemAsync',
      'docs/PROJECT_MASTER_SPECIFICATION_V2.md', 'IExistingArchitectureResultAuthorizer',
      'existing-architecture.item.read', 'ExistingArchitectureApprovalState.Approved',
      'CredentialsIncluded', 'LiveSessionIncluded', 'ExecutableCommandIncluded',
      'GeneratedContentIncluded', 'ExternalEffectOccurred',
      'IExistingArchitectureEvidenceRecorder', 'ArchitectureSha256Digest', 'CanAdvance: false',
      'Separately approved Approved Packages selection',
      'OPA returned a mismatched Existing Architecture decision') | ForEach-Object {
        if ($architectureEngine -notmatch [regex]::Escape($_)) {
            throw "Existing Architecture discovery guard '$($_)' is missing."
        }
    }
    $decisionValidationIndex = $architectureEngine.IndexOf('ValidateDecision(policyInput', [StringComparison]::Ordinal)
    $scopeConformanceIndex = $architectureEngine.IndexOf('var scopeConformance = await conformanceValidator.ValidateScopeAsync(', [StringComparison]::Ordinal)
    $sourceAccessIndex = $architectureEngine.IndexOf('var sourceResults = await Task.WhenAll', [StringComparison]::Ordinal)
    if ($decisionValidationIndex -lt 0 -or $scopeConformanceIndex -lt 0 -or $sourceAccessIndex -lt 0 -or
        $decisionValidationIndex -ge $scopeConformanceIndex -or $scopeConformanceIndex -ge $sourceAccessIndex) {
        throw 'Existing Architecture source access is not structurally ordered after OPA validation and scope conformance.'
    }

    $intentEndpoint = Get-Content -LiteralPath $intentEndpointPath -Raw
    @('/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture',
      'developer.internal-service.architecture.discover', 'IAuthorizedExistingSystemsSnapshotReader',
      'IExistingArchitecturePolicyGate', 'IExistingArchitectureConformanceValidator',
      'IExistingArchitectureResultAuthorizer', 'IExistingArchitectureEvidenceRecorder',
      'GetServices<IExistingArchitectureSource>', 'Status503ServiceUnavailable',
      'RequireAuthorization') | ForEach-Object {
        if ($intentEndpoint -notmatch [regex]::Escape($_)) {
            throw "Existing Architecture endpoint guard '$($_)' is missing."
        }
    }

    $readiness = Get-Content -LiteralPath $readinessPath -Raw
    @('Authorized Existing Systems snapshot reader', 'Existing Architecture OPA policy gate',
      'Existing Architecture source', 'Existing Architecture conformance validator',
      'Existing Architecture result authorizer', 'Existing Architecture evidence recorder') | ForEach-Object {
        if ($readiness -notmatch [regex]::Escape($_)) {
            throw "Existing Architecture readiness dependency '$($_)' is missing."
        }
    }

    $openApi = Get-Content -LiteralPath $openApiPath -Raw
    @('/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems/{systemsDiscoveryId}/existing-architecture',
      'ExistingArchitectureDiscoveryInput', 'AuthorizedExistingArchitectureDiscoveryReceipt',
      'expectedInventorySha256Digest', 'AuthorizedExistingArchitectureItem',
      'conformanceEvidenceReferences', 'canAdvance', '503') | ForEach-Object {
        if ($openApi -notmatch [regex]::Escape($_)) {
            throw "Existing Architecture OpenAPI boundary '$($_)' is missing."
        }
    }

    $acceptance = Get-Content -LiteralPath $increment06AcceptancePath -Raw
    @('Status: **Satisfied**', 'CR-001-AMENDMENT-03-EXISTING-ARCHITECTURE.md',
      'all 33 required runtime dependencies remain fail-closed') | ForEach-Object {
        if ($acceptance -notmatch [regex]::Escape($_)) {
            throw "Increment 06 acceptance evidence '$($_)' is missing."
        }
    }
}

$increment07AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_07_ACCEPTANCE.md'
if (Test-Path -LiteralPath $increment07AcceptancePath) {
    $packagesEnginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedApprovedPackagesSelection.cs'
    $registryReaderPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Packages\IInstitutionalPackageRegistryReader.cs'
    $endpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changeControlPath = Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-04-APPROVED-PACKAGES.md'
    @($packagesEnginePath, $registryReaderPath, $endpointPath, $readinessPath, $openApiPath, $changeControlPath) | ForEach-Object {
        if (-not (Test-Path -LiteralPath $_)) { throw "Operational Increment 07 artifact is missing: $_" }
    }

    $changeControl = Get-Content -LiteralPath $changeControlPath -Raw
    @('Status: **Approved for Operational Increment 07**', 'Decision: **Approved by the repository owner',
      'Operational Increment 07 — Governed Approved Packages Selection') | ForEach-Object {
        if ($changeControl -notmatch [regex]::Escape($_)) { throw "Increment 07 approval '$($_)' is missing." }
    }

    $reader = Get-Content -LiteralPath $registryReaderPath -Raw
    @('IInstitutionalPackageRegistryReader', 'FindExactAsync') | ForEach-Object {
        if ($reader -notmatch [regex]::Escape($_)) { throw "Approved Packages registry read boundary '$($_)' is missing." }
    }
    if ($reader -match '(?i)SaveAsync|UpdateAsync|DeleteAsync|Download|Install|Execute') {
        throw 'Approved Packages registry reader must expose exact read only.'
    }

    $engine = Get-Content -LiteralPath $packagesEnginePath -Raw
    @('developer.internal-service.packages.select', 'IAuthorizedExistingArchitectureSnapshotReader',
      'IApprovedPackagesPolicyGate', 'PolicySignatureValid', 'AllowedCoordinates.SetEquals',
      'IInstitutionalPackageRegistryReader', 'FindExactAsync', 'IPackageEligibilityEvaluator',
      'IApprovedPackageSupplyChainVerifier', 'DigestVerified', 'ProvenanceVerified',
      'SbomVerified', 'SignatureVerified', 'SovereignRegistryVerified', 'PackageTransferred',
      'PackageExecuted', 'ExternalEffectOccurred', 'IApprovedPackageResultAuthorizer',
      'approved-package.read', 'IApprovedPackagesEvidenceRecorder', 'SelectionSha256Digest',
      'CanAdvance', 'Separately approved AI Planning',
      'OPA returned a mismatched Approved Packages decision') | ForEach-Object {
        if ($engine -notmatch [regex]::Escape($_)) { throw "Approved Packages guard '$($_)' is missing." }
    }
    $policyIndex = $engine.IndexOf('ValidateDecision(input', [StringComparison]::Ordinal)
    $registryIndex = $engine.IndexOf('registryReader.FindExactAsync', [StringComparison]::Ordinal)
    if ($policyIndex -lt 0 -or $registryIndex -lt 0 -or $policyIndex -ge $registryIndex) {
        throw 'Institutional registry access is not structurally ordered after OPA validation.'
    }

    $endpoint = Get-Content -LiteralPath $endpointPath -Raw
    @('/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages',
      'developer.internal-service.packages.select', 'IAuthorizedExistingArchitectureSnapshotReader',
      'IApprovedPackagesPolicyGate', 'IInstitutionalPackageRegistryReader',
      'IApprovedPackageSupplyChainVerifier', 'IApprovedPackageResultAuthorizer',
      'IApprovedPackagesEvidenceRecorder', 'Status503ServiceUnavailable', 'RequireAuthorization') | ForEach-Object {
        if ($endpoint -notmatch [regex]::Escape($_)) { throw "Approved Packages endpoint guard '$($_)' is missing." }
    }

    $readiness = Get-Content -LiteralPath $readinessPath -Raw
    @('Authorized Existing Architecture snapshot reader', 'Approved Packages OPA policy gate',
      'Institutional package registry reader', 'Approved Package supply-chain verifier',
      'Approved Package result authorizer', 'Approved Packages evidence recorder') | ForEach-Object {
        if ($readiness -notmatch [regex]::Escape($_)) { throw "Approved Packages readiness '$($_)' is missing." }
    }

    $openApi = Get-Content -LiteralPath $openApiPath -Raw
    @('/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems/{systemsDiscoveryId}/existing-architecture/{architectureDiscoveryId}/approved-packages',
      'ApprovedPackagesSelectionInput', 'GovernedApprovedPackagesSelectionReceipt',
      'expectedArchitectureSha256Digest', 'requestedCoordinates', 'GovernedApprovedPackage',
      'selectionSha256Digest', 'canAdvance', '503') | ForEach-Object {
        if ($openApi -notmatch [regex]::Escape($_)) { throw "Approved Packages OpenAPI '$($_)' is missing." }
    }

    $acceptance = Get-Content -LiteralPath $increment07AcceptancePath -Raw
    @('Status: **Satisfied**', 'CR-001-AMENDMENT-04-APPROVED-PACKAGES.md',
      'all 39 required runtime dependencies remain fail-closed') | ForEach-Object {
        if ($acceptance -notmatch [regex]::Escape($_)) { throw "Increment 07 acceptance '$($_)' is missing." }
    }
}

$increment08AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_08_ACCEPTANCE.md'
if (Test-Path -LiteralPath $increment08AcceptancePath) {
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedAiPlanningCandidate.cs'
    $endpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath = Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-05-AI-PLANNING.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath) | ForEach-Object { if(-not(Test-Path $_)){throw "Increment 08 artifact missing: $_"} }
    $change = Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 08**','Decision: **Approved by the repository owner') | ForEach-Object { if($change -notmatch [regex]::Escape($_)){throw "Increment 08 approval missing: $_"} }
    $engine = Get-Content -Raw $enginePath
    @('DeliveryStage.ApprovedPackages','IAiPlanningPolicyGate','PolicySignatureValid','IGovernedPlanningPromptTemplateReader',
      'IAiPlanningContextAuthorizer','AiDevelopmentTaskKind.Planning','GovernedAiDevelopmentService','IAiOutputEvaluator',
      'IsExecutable: false','CanAdvance: false','Separately approved Code Generation','GeneratedFilePaths','IAiPlanningResultAuthorizer','IAiPlanningEvidenceRecorder') | ForEach-Object { if($engine -notmatch [regex]::Escape($_)){throw "AI Planning guard missing: $_"} }
    $policyIndex=$engine.IndexOf('ValidateDecision(input',[StringComparison]::Ordinal);$runtimeIndex=$engine.IndexOf('ProduceCandidateAsync',[StringComparison]::Ordinal)
    if($policyIndex -lt 0 -or $runtimeIndex -lt 0 -or $policyIndex -ge $runtimeIndex){throw 'AI invocation is not ordered after OPA validation.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/approved-packages/{packageSelectionId:guid}/ai-planning','developer.internal-service.ai-planning.create','RequireAuthorization','Status503ServiceUnavailable') | ForEach-Object { if($endpoint -notmatch [regex]::Escape($_)){throw "AI Planning endpoint guard missing: $_"} }
    $readiness=Get-Content -Raw $readinessPath
    @('Authorized Approved Packages snapshot reader','AI Planning delivery-run reader','AI Planning OPA policy gate','Governed planning prompt-template reader','AI Planning context authorizer','Independent AI output evaluator','AI Planning result authorizer','AI Planning evidence recorder') | ForEach-Object {if($readiness -notmatch [regex]::Escape($_)){throw "AI Planning readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/approved-packages/{packageSelectionId}/ai-planning','AiPlanningInput','GovernedAiPlanningReceipt','isExecutable','canAdvance','503') | ForEach-Object {if($openApi -notmatch [regex]::Escape($_)){throw "AI Planning OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment08AcceptancePath
    @('Status: **Satisfied**','all 47 required runtime dependencies remain fail-closed') | ForEach-Object {if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 08 acceptance missing: $_"}}
}

$increment09AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_09_ACCEPTANCE.md'
if (Test-Path -LiteralPath $increment09AcceptancePath) {
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedCodeGenerationCandidate.cs'
    $endpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath = Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-06-CODE-GENERATION.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath) | ForEach-Object {
        if(-not(Test-Path $_)){throw "Increment 09 artifact missing: $_"}
    }
    $change = Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 09**','Decision: **Approved by the repository owner') | ForEach-Object {
        if($change -notmatch [regex]::Escape($_)){throw "Increment 09 approval missing: $_"}
    }
    $engine = Get-Content -Raw $enginePath
    @('IAuthorizedAiPlanningCandidateReader','DeliveryStage.AiPlanning','ICodeGenerationPolicyGate',
      'PolicySignatureValid','IGovernedCodeGenerationPromptTemplateReader','ICodeGenerationContextAuthorizer',
      'AiDevelopmentTaskKind.CodeGeneration','GovernedAiDevelopmentService','IAiOutputEvaluator',
      'GovernedGeneratedPath.Validate','Path.IsPathFullyQualified','ProhibitedSegments',
      'IsExecutable: false','IsApplied: false','CanAdvance: false','Separately approved Static Validation',
      'ICodeGenerationResultAuthorizer','ICodeGenerationEvidenceRecorder') | ForEach-Object {
        if($engine -notmatch [regex]::Escape($_)){throw "Code Generation guard missing: $_"}
    }
    $policyIndex=$engine.IndexOf('ValidateDecision(input',[StringComparison]::Ordinal)
    $runtimeIndex=$engine.IndexOf('ProduceCandidateAsync',[StringComparison]::Ordinal)
    if($policyIndex -lt 0 -or $runtimeIndex -lt 0 -or $policyIndex -ge $runtimeIndex){
        throw 'Code Generation AI invocation is not ordered after OPA validation.'
    }
    $endpoint=Get-Content -Raw $endpointPath
    @('/ai-planning/{planningId:guid}/code-generation','developer.internal-service.code-generation.create',
      'RequireAuthorization','Status503ServiceUnavailable') | ForEach-Object {
        if($endpoint -notmatch [regex]::Escape($_)){throw "Code Generation endpoint guard missing: $_"}
    }
    $readiness=Get-Content -Raw $readinessPath
    @('Authorized AI Planning candidate reader','Code Generation delivery-run reader','Code Generation OPA policy gate',
      'Governed Code Generation prompt-template reader','Code Generation context authorizer',
      'Code Generation result and path authorizer','Code Generation evidence recorder') | ForEach-Object {
        if($readiness -notmatch [regex]::Escape($_)){throw "Code Generation readiness missing: $_"}
    }
    $openApi=Get-Content -Raw $openApiPath
    @('/ai-planning/{planningId}/code-generation','CodeGenerationInput','GovernedCodeGenerationReceipt',
      'requestedOutputPaths','isExecutable','isApplied','canAdvance','503') | ForEach-Object {
        if($openApi -notmatch [regex]::Escape($_)){throw "Code Generation OpenAPI missing: $_"}
    }
    $acceptance=Get-Content -Raw $increment09AcceptancePath
    @('Status: **Satisfied**','all 54 required runtime dependencies remain fail closed') | ForEach-Object {
        if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 09 acceptance missing: $_"}
    }
}

$increment10AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_10_ACCEPTANCE.md'
if (Test-Path -LiteralPath $increment10AcceptancePath) {
    $enginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedStaticValidation.cs'
    $endpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath = Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-07-STATIC-VALIDATION.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath) | ForEach-Object {
        if(-not(Test-Path $_)){throw "Increment 10 artifact missing: $_"}
    }
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 10**','Decision: **Approved by the repository owner') | ForEach-Object {
        if($change -notmatch [regex]::Escape($_)){throw "Increment 10 approval missing: $_"}
    }
    $engine=Get-Content -Raw $enginePath
    @('IStaticValidationPolicyGate','IAuthorizedCodeGenerationCandidateReader','IStaticValidationDeliveryRunReader',
      'DeliveryStage.CodeGeneration','ICodeValidationControl','CodeValidationPipeline','ValidationGate.Static',
      'report.IsAccepted','item.Passed','IStaticValidationResultAuthorizer',
      'IStaticValidationEvidenceRecorder','IsExecutable: false','CanAdvance: false',
      'Separately approved Security Validation') | ForEach-Object {
        if($engine -notmatch [regex]::Escape($_)){throw "Static Validation guard missing: $_"}
    }
    $policyIndex=$engine.IndexOf('ValidateDecision(input',[StringComparison]::Ordinal)
    $candidateIndex=$engine.IndexOf('candidateReader.LoadAsync',[StringComparison]::Ordinal)
    if($policyIndex -lt 0 -or $candidateIndex -lt 0 -or $policyIndex -ge $candidateIndex){
        throw 'Static Validation candidate read is not ordered after verified OPA permit.'
    }
    $endpoint=Get-Content -Raw $endpointPath
    @('/code-generation/{generationId:guid}/static-validation','developer.internal-service.static-validation.create',
      'RequireAuthorization','Status503ServiceUnavailable') | ForEach-Object {
        if($endpoint -notmatch [regex]::Escape($_)){throw "Static Validation endpoint guard missing: $_"}
    }
    $readiness=Get-Content -Raw $readinessPath
    @('Static Validation OPA policy gate','Authorized Code Generation candidate reader',
      'Static Validation delivery-run reader','Institutionally approved Static Validation controls',
      'Static Validation result authorizer','Static Validation evidence recorder') | ForEach-Object {
        if($readiness -notmatch [regex]::Escape($_)){throw "Static Validation readiness missing: $_"}
    }
    $openApi=Get-Content -Raw $openApiPath
    @('/code-generation/{generationId}/static-validation','StaticValidationInput',
      'GovernedStaticValidationReceipt','requiredControlIds','isExecutable','canAdvance','503') | ForEach-Object {
        if($openApi -notmatch [regex]::Escape($_)){throw "Static Validation OpenAPI missing: $_"}
    }
    $acceptance=Get-Content -Raw $increment10AcceptancePath
    @('Status: **Satisfied**','all 60 required runtime dependencies remain fail closed') | ForEach-Object {
        if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 10 acceptance missing: $_"}
    }
}

$increment11AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_11_ACCEPTANCE.md'
if (Test-Path $increment11AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedSecurityValidation.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-08-SECURITY-VALIDATION.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 11 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 11**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 11 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('ISecurityValidationPolicyGate','IAuthorizedStaticValidationReceiptReader','IAuthorizedCodeGenerationCandidateReader','DeliveryStage.StaticValidation','CodeValidationPipeline','ValidationGate.Security','report.IsAccepted','item.Passed','ISecurityValidationResultAuthorizer','ISecurityValidationEvidenceRecorder','IsExecutable: false','CanAdvance: false','Separately approved Sandbox')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Security Validation guard missing: $_"}}
    if($engine.IndexOf('ValidateDecision(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('staticReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Security prerequisite read occurs before OPA validation.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/static-validation/{staticValidationId:guid}/security-validation','developer.internal-service.security-validation.create','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Security endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Security Validation OPA policy gate','Authorized Static Validation receipt reader','Security Validation delivery-run reader','Security Validation result authorizer','Security Validation evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Security readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/static-validation/{staticValidationId}/security-validation','SecurityValidationInput','GovernedSecurityValidationReceipt','isExecutable','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Security OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment11AcceptancePath
    @('Status: **Satisfied**','all 65 dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 11 acceptance missing: $_"}}
}

$increment12AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_12_ACCEPTANCE.md'
if (Test-Path $increment12AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedSandboxExecution.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $isolationPath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Sandbox\SandboxIsolationPolicy.cs'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-09-SANDBOX.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$isolationPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 12 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 12**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 12 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('ISandboxPolicyGate','IAuthorizedSecurityValidationReceiptReader','IAuthorizedCodeGenerationCandidateReader','DeliveryStage.SecurityValidation','IInstitutionalPackageRegistryReader','IApprovedPackageSupplyChainVerifier','GovernedSandboxService','ISecuritySandboxRuntime','ISandboxResultAuthorizer','ISandboxEvidenceRecorder','ProductionEffectOccurred','CanAdvance','Separately approved Tests')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Sandbox guard missing: $_"}}
    $isolation=Get-Content -Raw $isolationPath
    @('Firecracker-class','ProductionCredentialsAllowed','HostFilesystemAccessAllowed','NetworkDefaultDeny')|ForEach-Object{if($isolation -notmatch [regex]::Escape($_)){throw "Sandbox isolation guard missing: $_"}}
    if($engine.IndexOf('ValidateDecision(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('securityReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Sandbox prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion'){throw 'Sandbox must not create a workflow stage completion.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/security-validation/{securityValidationId:guid}/sandbox','developer.internal-service.sandbox.execute','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Sandbox endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Sandbox OPA policy gate','Authorized Security Validation receipt reader','Sandbox delivery-run reader','Sandbox result authorizer','Sandbox evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Sandbox readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/security-validation/{securityValidationId}/sandbox','SandboxExecutionInput','GovernedSandboxExecutionReceipt','productionEffectOccurred','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Sandbox OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment12AcceptancePath
    @('Status: **Satisfied**','all 70 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 12 acceptance missing: $_"}}
}

$increment13AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_13_ACCEPTANCE.md'
if (Test-Path $increment13AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedTestsExecution.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $isolationPath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Sandbox\SandboxIsolationPolicy.cs'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-10-TESTS.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$isolationPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 13 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 13**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 13 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('ITestsPolicyGate','IAuthorizedSandboxExecutionReceiptReader','IAuthorizedSecurityValidationReceiptReader','IAuthorizedCodeGenerationCandidateReader','DeliveryStage.Sandbox','IGovernedTestManifestReader','IInstitutionalPackageRegistryReader','IApprovedPackageSupplyChainVerifier','IGovernedTestRuntime','RequiredTestIds','ManifestDigest','ITestsResultAuthorizer','ITestsEvidenceRecorder','ProductionEffectOccurred','CanAdvance','Separately approved Human Review')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Tests guard missing: $_"}}
    if($engine.IndexOf('ValidateDecision(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('sandboxReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Tests prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion'){throw 'Tests must not create a workflow stage completion.'}
    $isolation=Get-Content -Raw $isolationPath
    @('Firecracker-class','ProductionCredentialsAllowed','HostFilesystemAccessAllowed','NetworkDefaultDeny')|ForEach-Object{if($isolation -notmatch [regex]::Escape($_)){throw "Tests isolation guard missing: $_"}}
    $endpoint=Get-Content -Raw $endpointPath
    @('/sandbox/{sandboxExecutionId:guid}/tests','developer.internal-service.tests.execute','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Tests endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Tests OPA policy gate','Authorized Sandbox receipt reader','Tests delivery-run reader','Governed test-manifest reader','Governed test runtime','Tests result authorizer','Tests evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Tests readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/sandbox/{sandboxExecutionId}/tests','TestsExecutionInput','GovernedTestsExecutionReceipt','requiredTestIds','productionEffectOccurred','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Tests OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment13AcceptancePath
    @('Status: **Satisfied**','all 77 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 13 acceptance missing: $_"}}
}

$increment14AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_14_ACCEPTANCE.md'
if (Test-Path $increment14AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedHumanReview.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-11-HUMAN-REVIEW.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 14 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 14**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 14 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('IHumanReviewPolicyGate','IAuthorizedTestsExecutionReceiptReader','IAuthorizedSandboxExecutionReceiptReader','IAuthorizedSecurityValidationReceiptReader','IAuthorizedCodeGenerationCandidateReader','DeliveryStage.Tests','DeclaredConflictingSubjectIds','ReviewerIsHuman','IHumanReviewAttestationVerifier','IsNonRepudiable','IAtomicHumanReviewRepository','ExpectedVersion','ProductionEffectOccurred','CanAdvance','Separately approved Git boundary')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Human Review guard missing: $_"}}
    if($engine.IndexOf('ValidatePolicy(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('testsReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Human Review prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion'){throw 'Human Review must not create a workflow stage completion.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/tests/{testsExecutionId:guid}/human-review','developer.internal-service.human-review.decide','input.InitiatorSubjectId, true','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Human Review endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Human Review OPA policy gate','Authorized Tests receipt reader','Human Review delivery-run reader','Human Review attestation verifier','Atomic Human Review and evidence repository')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Human Review readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/tests/{testsExecutionId}/human-review','HumanReviewInput','GovernedHumanReviewReceipt','declaredConflictingSubjectIds','isApproved','productionEffectOccurred','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Human Review OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment14AcceptancePath
    @('Status: **Satisfied**','all 82 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 14 acceptance missing: $_"}}
}

$increment15AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_15_ACCEPTANCE.md'
if (Test-Path $increment15AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedGitSourceCommit.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-12-GIT.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 15 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 15**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 15 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('IGitPolicyGate','IAuthorizedHumanReviewReceiptReader','IAuthorizedTestsExecutionReceiptReader','IAuthorizedCodeGenerationCandidateReader','DeliveryStage.HumanReview','IGovernedGitChangeSetMaterializer','IGitChangePolicyValidator','IInstitutionalGitGateway','CommitSigned','ProtectedBranchMutated','ForceUpdateOccurred','CiCdTriggered','IGitResultAuthorizer','IGitEvidenceRecorder','SourceMutationOccurred','ProductionEffectOccurred','CanAdvance','Separately approved CI/CD')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Git guard missing: $_"}}
    if($engine.IndexOf('ValidatePolicy(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('reviewReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Git prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion'){throw 'Git must not create a workflow stage completion.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/human-review/{reviewId:guid}/git','developer.internal-service.git.commit','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Git endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Git OPA policy gate','Authorized Human Review receipt reader','Git delivery-run reader','Governed Git change-set materializer','Institutional Git change-policy validator','Institutional Git gateway','Git result authorizer','Git evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Git readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/human-review/{reviewId}/git','GitSourceCommitInput','GovernedGitSourceCommitReceipt','sourceMutationOccurred','productionEffectOccurred','ciCdTriggered','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Git OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment15AcceptancePath
    @('Status: **Satisfied**','all 90 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 15 acceptance missing: $_"}}
}

$increment16AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_16_ACCEPTANCE.md'
if (Test-Path $increment16AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedCiCdExecution.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-13-CICD.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 16 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 16**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 16 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('ICiCdPolicyGate','IAuthorizedGitSourceCommitReceiptReader','ICiCdDeliveryRunReader','DeliveryStage.Git','IGovernedCiCdWorkflowDefinitionReader','ICiCdWorkflowValidator','ImmutableTaskReferences','LeastPrivilege','UsesLockedDependencies','IInstitutionalCiCdGateway','EphemeralIsolationVerified','NetworkDefaultDeny','ProductionCredentialsPresent','GovernedPipelineOutputManifest','SbomReference','ProvenanceReference','BuildAttestationReference','SignatureReference','ICiCdResultAuthorizer','ICiCdEvidenceRecorder','CiCdTriggered','SourceMutationOccurred','ArtifactPublished','DeploymentOccurred','ProductionEffectOccurred','CanAdvance','Separately approved Artifact')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "CI/CD guard missing: $_"}}
    if($engine.IndexOf('ValidatePolicy(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('gitReader.LoadAsync',[StringComparison]::Ordinal)){throw 'CI/CD prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion'){throw 'CI/CD must not create a workflow stage completion.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/api/v1/internal-services/git/{gitOperationId:guid}/cicd','developer.internal-service.cicd.execute','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "CI/CD endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('CI/CD OPA policy gate','Authorized Git source-commit receipt reader','CI/CD delivery-run reader','Governed CI/CD workflow-definition reader','Institutional CI/CD workflow validator','Institutional CI/CD gateway','CI/CD result authorizer','CI/CD evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "CI/CD readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/api/v1/internal-services/git/{gitOperationId}/cicd','CiCdExecutionInput','GovernedCiCdExecutionReceipt','ciCdTriggered','sourceMutationOccurred','artifactPublished','deploymentOccurred','productionEffectOccurred','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "CI/CD OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment16AcceptancePath
    @('Status: **Satisfied**','all 98 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 16 acceptance missing: $_"}}
}

$increment17AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_17_ACCEPTANCE.md'
if (Test-Path $increment17AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedArtifactPublication.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-14-ARTIFACT.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 17 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 17**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 17 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('IArtifactPolicyGate','IAuthorizedCiCdExecutionReceiptReader','IAuthorizedPipelineOutputManifestReader','IArtifactDeliveryRunReader','DeliveryStage.CiCd','IArtifactPackageValidator','IInstitutionalArtifactRegistryGateway','ImmutableCoordinate','ExistingCoordinateOverwritten','IdempotencyVerified','SupplyChainVerificationPipeline','SupplyChainControl','IArtifactResultAuthorizer','IArtifactEvidenceRecorder','ArtifactPublished','RegistryMutated','SourceMutationOccurred','DeploymentOccurred','ProductionEffectOccurred','CanAdvance','Separately approved Deployment')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Artifact guard missing: $_"}}
    if($engine.IndexOf('ValidatePolicy(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('ciCdReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Artifact prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion'){throw 'Artifact must not create a workflow stage completion.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/api/v1/internal-services/cicd/{ciCdExecutionId:guid}/artifact','developer.internal-service.artifact.publish','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Artifact endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Artifact OPA policy gate','Authorized CI/CD execution receipt reader','Authorized pipeline-output manifest reader','Artifact delivery-run reader','Institutional Artifact package validator','Institutional Artifact registry gateway','Artifact supply-chain control verifiers','Artifact result authorizer','Artifact evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Artifact readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/api/v1/internal-services/cicd/{ciCdExecutionId}/artifact','ArtifactPublicationInput','GovernedArtifactPublicationReceipt','artifactPublished','registryMutated','sourceMutationOccurred','deploymentOccurred','productionEffectOccurred','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Artifact OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment17AcceptancePath
    @('Status: **Satisfied**','all 107 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 17 acceptance missing: $_"}}
}

$increment18AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_18_ACCEPTANCE.md'
if (Test-Path $increment18AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedSovereignDeployment.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-15-DEPLOYMENT.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 18 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 18**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 18 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('IDeploymentPolicyGate','IAuthorizedArtifactPublicationReceiptReader','IAuthorizedDeploymentArtifactReader','IDeploymentDeliveryRunReader','DeliveryStage.Artifact','IGovernedSovereignDeploymentProfileReader','GovernedDeploymentTopology.AirGapped','ExternalControlPlaneAllowed','OutboundNetworkDefaultDeny','IInstitutionalDeploymentPreflightValidator','ArtifactAvailable','WorkloadIdentityValid','SecretsReferencesValid','RollbackReady','IInstitutionalSovereignDeploymentGateway','ActivationVerified','IdempotencyVerified','RollbackReference','ProductionEffectOccurred','TelemetryConfigured','AutomaticRegistrationOccurred','EnterpriseModelMutated','IDeploymentResultAuthorizer','IDeploymentEvidenceRecorder','CanAdvance','Separately approved OpenTelemetry')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Deployment guard missing: $_"}}
    if($engine.IndexOf('ValidatePolicy(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('artifactReceiptReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Deployment prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion'){throw 'Deployment must not create a workflow stage completion.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/api/v1/internal-services/artifacts/{artifactPublicationId:guid}/deployment','operator.internal-service.deployment.execute','input.TargetEnvironment','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Deployment endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Deployment OPA policy gate','Authorized Artifact publication receipt reader','Authorized verified Deployment Artifact reader','Deployment delivery-run reader','Governed sovereign Deployment profile reader','Institutional Deployment preflight validator','Institutional sovereign Deployment gateway','Deployment result authorizer','Deployment evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Deployment readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/api/v1/internal-services/artifacts/{artifactPublicationId}/deployment','SovereignDeploymentInput','GovernedSovereignDeploymentReceipt','productionDeploymentRequested','deploymentOccurred','productionEffectOccurred','telemetryConfigured','automaticRegistrationOccurred','enterpriseModelMutated','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Deployment OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment18AcceptancePath
    @('Status: **Satisfied**','all 116 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 18 acceptance missing: $_"}}
}

$increment19AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_19_ACCEPTANCE.md'
if (Test-Path $increment19AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedOpenTelemetryActivation.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-16-OPENTELEMETRY.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 19 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 19**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 19 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('IOpenTelemetryPolicyGate','IAuthorizedSovereignDeploymentReceiptReader','IOpenTelemetryDeliveryRunReader','DeliveryStage.Deployment','IGovernedOpenTelemetryProfileReader','MandatoryExternalControlPlane','TraceAwareRoutingEnabled','public enum GovernedTelemetrySignal { Traces, Metrics, Logs }','IOpenTelemetryRedactionPolicyVerifier','SensitiveNamesDropped','UnknownAttributesRedacted','RetainedStringsBounded','BaggageClearedAtStart','BaggageClearedAtEnd','LowCardinalityAttributesOnly','IInstitutionalOpenTelemetryGateway','AutomaticRegistrationOccurred','EnterpriseModelMutated','IOpenTelemetryResultAuthorizer','IOpenTelemetryEvidenceRecorder','CanAdvance','Separately approved Automatic Registration')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "OpenTelemetry guard missing: $_"}}
    if($engine.IndexOf('ValidatePolicy(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('deploymentReader.LoadAsync',[StringComparison]::Ordinal)){throw 'OpenTelemetry prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion'){throw 'OpenTelemetry must not create a workflow stage completion.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/api/v1/internal-services/deployments/{deploymentId:guid}/opentelemetry','operator.internal-service.telemetry.activate','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "OpenTelemetry endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('OpenTelemetry OPA policy gate','Authorized sovereign Deployment receipt reader','OpenTelemetry delivery-run reader','Governed OpenTelemetry profile reader','OpenTelemetry redaction-policy verifier','Institutional OpenTelemetry gateway','OpenTelemetry result authorizer','OpenTelemetry evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "OpenTelemetry readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/api/v1/internal-services/deployments/{deploymentId}/opentelemetry','OpenTelemetryActivationInput','GovernedOpenTelemetryActivationReceipt','telemetryConfigured','automaticRegistrationOccurred','enterpriseModelMutated','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "OpenTelemetry OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment19AcceptancePath
    @('Status: **Satisfied**','all 124 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 19 acceptance missing: $_"}}
}

$increment20AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_20_ACCEPTANCE.md'
if (Test-Path $increment20AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedAutomaticRegistration.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-17-AUTOMATIC-REGISTRATION.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 20 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 20**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 20 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('IAutomaticRegistrationPolicyGate','IAuthorizedOpenTelemetryActivationReceiptReader','IAutomaticRegistrationDeliveryRunReader','DeliveryStage.OpenTelemetry','IGovernedAutomaticRegistrationManifestReader','AutomaticRegistrationEngine','IAutomaticRegistrationRepository','RegisterAsync','RegistrationDisposition','IAutomaticRegistrationResultAuthorizer','IAutomaticRegistrationEvidenceRecorder','WorkflowAdvanced','CanAdvance','Separately approved Enterprise Model contextualization')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Automatic Registration guard missing: $_"}}
    if($engine.IndexOf('ValidatePolicy(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('activationReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Automatic Registration prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion'){throw 'Automatic Registration must not create a workflow stage completion.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/api/v1/internal-services/opentelemetry/{activationId:guid}/automatic-registration','operator.internal-service.registration.execute','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Automatic Registration endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Automatic Registration OPA policy gate','Authorized OpenTelemetry activation receipt reader','Automatic Registration delivery-run reader','Governed Automatic Registration manifest reader','Automatic Registration result authorizer','Automatic Registration evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Automatic Registration readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/api/v1/internal-services/opentelemetry/{activationId}/automatic-registration','AutomaticRegistrationInput','GovernedAutomaticRegistrationReceipt','automaticRegistrationOccurred','enterpriseModelObjectPersisted','workflowAdvanced','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Automatic Registration OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment20AcceptancePath
    @('Status: **Satisfied**','all 130 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 20 acceptance missing: $_"}}
}

$increment21AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_21_ACCEPTANCE.md'
if (Test-Path $increment21AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedEnterpriseModelContextualization.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-18-ENTERPRISE-MODEL.md'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 21 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 21**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 21 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('IEnterpriseModelContextPolicyGate','IAuthorizedAutomaticRegistrationReceiptReader','IEnterpriseModelDeliveryRunReader','DeliveryStage.AutomaticRegistration','IAuthorizedRegisteredEnterpriseObjectReader','LifecycleState.Active','automatic-registration','IEnterpriseModelContextResultAuthorizer','IEnterpriseModelContextEvidenceRecorder','EnterpriseModelMutated','WorkflowAdvanced','EvidenceCompleted','CanAdvance','Separately approved Evidence completion')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Enterprise Model guard missing: $_"}}
    if($engine.IndexOf('ValidatePolicy(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('registrationReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Enterprise Model prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion|EnterpriseImpactAnalysisEngine|EnterpriseSimulationEngine'){throw 'Enterprise Model contextualization crossed its analysis, simulation, or workflow boundary.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/api/v1/internal-services/registrations/{registrationId:guid}/enterprise-model','operator.internal-service.enterprise-model.contextualize','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Enterprise Model endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Enterprise Model contextualization OPA policy gate','Authorized Automatic Registration receipt reader','Enterprise Model contextualization delivery-run reader','Authorized registered Enterprise Object reader','Enterprise Model contextualization result authorizer','Enterprise Model contextualization evidence recorder')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Enterprise Model readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/api/v1/internal-services/registrations/{registrationId}/enterprise-model','EnterpriseModelContextualizationInput','GovernedEnterpriseModelContextualizationReceipt','enterpriseModelContextualized','enterpriseModelMutated','workflowAdvanced','evidenceCompleted','canAdvance')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Enterprise Model OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment21AcceptancePath
    @('Status: **Satisfied**','all 136 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 21 acceptance missing: $_"}}
}

$increment22AcceptancePath = Join-Path $repositoryRoot 'docs\phase-29\OPERATIONAL_INCREMENT_22_ACCEPTANCE.md'
if (Test-Path $increment22AcceptancePath) {
    $enginePath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedEvidenceCompletion.cs'
    $endpointPath=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $readinessPath=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $openApiPath=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath=Join-Path $repositoryRoot 'docs\change-control\CR-001-AMENDMENT-19-EVIDENCE-COMPLETION.md'
    $projectPath=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Platform.SoftwareFactory.csproj'
    @($enginePath,$endpointPath,$readinessPath,$openApiPath,$changePath,$projectPath)|ForEach-Object{if(-not(Test-Path $_)){throw "Increment 22 artifact missing: $_"}}
    $change=Get-Content -Raw $changePath
    @('Status: **Approved for Operational Increment 22**','Decision: **Approved by the repository owner')|ForEach-Object{if($change -notmatch [regex]::Escape($_)){throw "Increment 22 approval missing: $_"}}
    $engine=Get-Content -Raw $enginePath
    @('IEvidenceCompletionPolicyGate','IAuthorizedEnterpriseModelContextualizationReceiptReader','IEvidenceCompletionDeliveryRunReader','DeliveryStage.EnterpriseModel','IEvidenceChainStore','IEvidenceAccessAuthorizer','IEvidenceSigner','IEvidenceSignatureVerifier','CryptographicEvidenceEngine','EvidenceStage.Evidence','AppendAsync','VerifyAsync','IsComplete','HashValid','SignatureValid','IEvidenceCompletionResultAuthorizer','EvidenceCryptographicallyVerified','EvidenceCompleted','VerticalSliceComplete','WorkflowAdvanced','CanAdvance','Complete — approved Create Internal Service vertical slice proven')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Evidence completion guard missing: $_"}}
    if($engine.IndexOf('ValidatePolicy(input',[StringComparison]::Ordinal) -ge $engine.IndexOf('contextualizationReader.LoadAsync',[StringComparison]::Ordinal)){throw 'Evidence completion prerequisite read occurs before OPA validation.'}
    if($engine -match 'StageCompletion|SaveAsync|UpdateAsync|DeleteAsync'){throw 'Evidence completion crossed its append-only or workflow boundary.'}
    $project=Get-Content -Raw $projectPath
    if($project -notmatch [regex]::Escape('Platform.Evidence\Platform.Evidence.csproj')){throw 'Software Factory does not reference the approved Evidence module.'}
    $endpoint=Get-Content -Raw $endpointPath
    @('/api/v1/internal-services/enterprise-model/{contextualizationId:guid}/evidence','operator.internal-service.evidence.complete','evidence.append','evidence.verify','RequireAuthorization')|ForEach-Object{if($endpoint -notmatch [regex]::Escape($_)){throw "Evidence completion endpoint missing: $_"}}
    $readiness=Get-Content -Raw $readinessPath
    @('Evidence access authorizer','Evidence sovereign signer','Evidence signature verifier','Evidence completion OPA policy gate','Authorized Enterprise Model contextualization receipt reader','Evidence completion delivery-run reader','Evidence completion result authorizer')|ForEach-Object{if($readiness -notmatch [regex]::Escape($_)){throw "Evidence completion readiness missing: $_"}}
    $openApi=Get-Content -Raw $openApiPath
    @('/api/v1/internal-services/enterprise-model/{contextualizationId}/evidence','EvidenceCompletionInput','GovernedEvidenceCompletionReceipt','evidenceAppended','evidenceCryptographicallyVerified','evidenceCompleted','verticalSliceComplete','workflowAdvanced','canAdvance','verifiedEntryCount')|ForEach-Object{if($openApi -notmatch [regex]::Escape($_)){throw "Evidence completion OpenAPI missing: $_"}}
    $acceptance=Get-Content -Raw $increment22AcceptancePath
    @('Status: **Satisfied**','all 143 required runtime dependencies remain fail closed')|ForEach-Object{if($acceptance -notmatch [regex]::Escape($_)){throw "Increment 22 acceptance missing: $_"}}
}

$wave01AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_01_ACCEPTANCE.md'
if (Test-Path $wave01AcceptancePath) {
    $identityOptionsPath = Join-Path $repositoryRoot 'backend\Platform.Identity\IdentityProviderOptions.cs'
    $identityServicesPath = Join-Path $repositoryRoot 'backend\Platform.Identity\IdentityServiceCollectionExtensions.cs'
    $identityProjectPath = Join-Path $repositoryRoot 'backend\Platform.Identity\Platform.Identity.csproj'
    $identityLockPath = Join-Path $repositoryRoot 'backend\Platform.Identity\packages.lock.json'
    $policyOptionsPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\PolicyControlPlaneOptions.cs'
    $policyAdapterPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\SovereignPolicyControlPlane.cs'
    $governedGatewayPath = Join-Path $repositoryRoot 'backend\Platform.Governance\GovernedActions\GovernedActionGateway.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
    $operationalEndpointsPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $changePath = Join-Path $repositoryRoot 'docs\change-control\CR-002-OPERATIONALIZATION-WAVE-01.md'
    $guidePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_01_IDENTITY_POLICY.md'
    @($identityOptionsPath,$identityServicesPath,$identityProjectPath,$identityLockPath,$policyOptionsPath,$policyAdapterPath,$governedGatewayPath,$readinessPath,$operationalEndpointsPath,$openApiPath,$changePath,$guidePath) | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 01 artifact missing: $_" } }

    $change = Get-Content -Raw $changePath
    @('Status: **Approved for Operationalization Wave 01**','Decision: **Approved by the repository owner','10.0.11') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 01 approval missing: $_" } }
    $identityProject = Get-Content -Raw $identityProjectPath
    $identityLock = Get-Content -Raw $identityLockPath
    if ($identityProject -notmatch 'Microsoft.AspNetCore.Authentication.JwtBearer" Version="10\.0\.11"') { throw 'JWT bearer package is not pinned at the approved version.' }
    @('"requested": "[10.0.11, )"','"resolved": "10.0.11"') | ForEach-Object { if ($identityLock -notmatch [regex]::Escape($_)) { throw "JWT bearer lock evidence missing: $_" } }

    $identityOptions = Get-Content -Raw $identityOptionsPath
    @('ControlPlaneConfigurationState.Unconfigured','ControlPlaneConfigurationState.Invalid','Uri.UriSchemeHttps','authority.UserInfo','authority.Query','authority.Fragment','Audience.Any(char.IsWhiteSpace)') | ForEach-Object { if ($identityOptions -notmatch [regex]::Escape($_)) { throw "Identity configuration guard missing: $_" } }
    $identityServices = Get-Content -Raw $identityServicesPath
    @('AddJwtBearer','JwtBearerDefaults.AuthenticationScheme','FailClosedAuthenticationDefaults.Scheme','ValidateIssuer = true','ValidateAudience = true','ValidateLifetime = true','ValidateIssuerSigningKey = true','RequireSignedTokens = true','RequireExpirationTime = true','ClockSkew = TimeSpan.Zero','MapInboundClaims = false','GovernedRequestContextFactory') | ForEach-Object { if ($identityServices -notmatch [regex]::Escape($_)) { throw "Identity adapter guard missing: $_" } }
    if ($identityServices -match 'JwtSecurityTokenHandler|ReadJwtToken') { throw 'An unapproved custom JWT parsing path was introduced.' }

    $policyOptions = Get-Content -Raw $policyOptionsPath
    @('OpaEndpoint','BundleVerificationEndpoint','TrustAnchorReference','Environment','RequestTimeoutSeconds','MaximumResponseBytes','Uri.UriSchemeHttps','endpoint.UserInfo','endpoint.Query','endpoint.Fragment') | ForEach-Object { if ($policyOptions -notmatch [regex]::Escape($_)) { throw "Policy configuration guard missing: $_" } }
    $policyAdapter = Get-Content -Raw $policyAdapterPath
    @('IPolicyBundleVerifier','IOpaPolicyDecisionPoint','SignatureValid','TrustAnchorReference','HttpCompletionOption.ResponseHeadersRead','MaximumResponseBytes','RequestTimeoutSeconds','ContentType','HttpRequestException','OperationCanceledException','UnauthorizedAccessException') | ForEach-Object { if ($policyAdapter -notmatch [regex]::Escape($_)) { throw "Sovereign policy adapter guard missing: $_" } }
    $governedGateway = Get-Content -Raw $governedGatewayPath
    if ($governedGateway.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $governedGateway.IndexOf('policyDecisionPoint.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'OPA evaluation does not follow signed policy-bundle verification.' }

    $readiness = Get-Content -Raw $readinessPath
    @('OPA policy bundle verifier','OPA policy decision point') | ForEach-Object { if ($readiness -notmatch [regex]::Escape($_)) { throw "Control-plane readiness dependency missing: $_" } }
    $operationalEndpoints = Get-Content -Raw $operationalEndpointsPath
    @('IdentityControlPlaneReadiness','PolicyControlPlaneReadiness','controlPlanes') | ForEach-Object { if ($operationalEndpoints -notmatch [regex]::Escape($_)) { throw "Control-plane readiness disclosure missing: $_" } }
    $openApi = Get-Content -Raw $openApiPath
    @('PlatformReadiness','unconfigured','invalid','configured') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Control-plane OpenAPI contract missing: $_" } }
    $acceptance = Get-Content -Raw $wave01AcceptancePath
    @('Status: **Satisfied**','remaining 142 institutional runtime dependencies remain disconnected and fail closed','15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 01 acceptance missing: $_" } }
}

$wave02AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_02_ACCEPTANCE.md'
if (Test-Path $wave02AcceptancePath) {
    $changePath = Join-Path $repositoryRoot 'docs\change-control\CR-003-OPERATIONALIZATION-WAVE-02.md'
    $policyContractPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs'
    $policyClientPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\SovereignPolicyEvaluationClient.cs'
    $intentPolicyPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignGovernedIntentPolicyGate.cs'
    $intentEnginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedIntentRegistration.cs'
    $persistenceOptionsPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlIntentRegistrationOptions.cs'
    $repositoryPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlGovernedIntentRegistrationRepository.cs'
    $migrationPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\001_governed_intent_registration.sql'
    $servicesPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
    $projectPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Platform.SoftwareFactory.csproj'
    $lockPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\packages.lock.json'
    $endpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
    $operationalEndpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $guidePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_02_GOVERNED_INTENT_REGISTRATION.md'
    @($changePath,$policyContractPath,$policyClientPath,$intentPolicyPath,$intentEnginePath,$persistenceOptionsPath,$repositoryPath,$migrationPath,$servicesPath,$projectPath,$lockPath,$endpointPath,$operationalEndpointPath,$openApiPath,$guidePath) | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 02 artifact missing: $_" } }

    $change = Get-Content -Raw $changePath
    @('Status: **Approved for Operationalization Wave 02**','Decision: **Approved by the repository owner','Npgsql` package at version `10.0.3') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 02 approval missing: $_" } }
    $project = Get-Content -Raw $projectPath
    $lock = Get-Content -Raw $lockPath
    if ($project -notmatch 'PackageReference Include="Npgsql" Version="10\.0\.3"') { throw 'Npgsql is not pinned at the approved version.' }
    @('"requested": "[10.0.3, )"','"resolved": "10.0.3"') | ForEach-Object { if ($lock -notmatch [regex]::Escape($_)) { throw "Npgsql lock evidence missing: $_" } }
    if ($project -match 'EntityFramework|Dapper') { throw 'An unapproved ORM or database package was introduced.' }

    $persistenceOptions = Get-Content -Raw $persistenceOptionsPath
    @('PostgreSqlIntentRegistrationConfigurationState.Unconfigured','PostgreSqlIntentRegistrationConfigurationState.Invalid','SslMode.VerifyFull','IncludeErrorDetail','CommandTimeoutSeconds <= 0') | ForEach-Object { if ($persistenceOptions -notmatch [regex]::Escape($_)) { throw "PostgreSQL profile guard missing: $_" } }
    $services = Get-Content -Raw $servicesPath
    @('persistenceOptions.IsOperationallyConfigured && policyOptions.IsOperationallyConfigured','NpgsqlDataSource.Create','IGovernedIntentPolicyGate, SovereignGovernedIntentPolicyGate','IGovernedIntentRegistrationRepository') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "Governed Intent runtime composition missing: $_" } }

    $policyClient = Get-Content -Raw $policyClientPath
    @('SovereignPolicyBundleVerifier.PostAsync','DecisionRequestId','Action','ResourceId','BundleSha256Digest','Environment','Enum.TryParse<OpaDecisionOutcome>','EvidenceReferences','DecidedAt') | ForEach-Object { if ($policyClient -notmatch [regex]::Escape($_)) { throw "Typed sovereign policy validation missing: $_" } }
    $intentPolicy = Get-Content -Raw $intentPolicyPath
    @('internal-service.intent.register','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','registrationId','intentSha256Digest','PolicySignatureValid: true') | ForEach-Object { if ($intentPolicy -notmatch [regex]::Escape($_)) { throw "Governed Intent OPA adapter guard missing: $_" } }
    if ($intentPolicy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $intentPolicy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'Intent OPA evaluation does not follow signed bundle verification.' }
    $intentEngine = Get-Content -Raw $intentEnginePath
    if ($intentEngine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $intentEngine.IndexOf('repository.RegisterAtomicallyAsync',[StringComparison]::Ordinal)) { throw 'Governed Intent persistence does not follow OPA evaluation.' }

    $repository = Get-Content -Raw $repositoryPath
    @('BeginTransactionAsync','IsolationLevel.ReadCommitted','ON CONFLICT DO NOTHING','FOR UPDATE','CommandTimeout','NpgsqlParameter','tenant_id = @tenant_id AND registration_id = @registration_id','expectedVersion != -1','candidate.Version != 0','ValidateUnchanged','SHA256.HashData','evidence://intent-registration','CommitAsync','GovernedIntentPersistenceUnavailableException','PostgresErrorCodes.UniqueViolation') | ForEach-Object { if ($repository -notmatch [regex]::Escape($_)) { throw "Atomic PostgreSQL repository guard missing: $_" } }
    if ($repository -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'The runtime repository contains an unapproved mutation or startup DDL path.' }
    $migration = Get-Content -Raw $migrationPath
    @('BEGIN;','CREATE SCHEMA IF NOT EXISTS software_factory','PRIMARY KEY (tenant_id, registration_id)','UNIQUE (tenant_id, submission_id)','UNIQUE (tenant_id, idempotency_key)','CHECK (version >= 0)','COMMIT;') | ForEach-Object { if ($migration -notmatch [regex]::Escape($_)) { throw "Governed Intent migration guard missing: $_" } }

    $endpoint = Get-Content -Raw $endpointPath
    @('GovernedIntentPersistenceUnavailableException','StatusCodes.Status503ServiceUnavailable','GovernedIntentConcurrencyException','StatusCodes.Status409Conflict') | ForEach-Object { if ($endpoint -notmatch [regex]::Escape($_)) { throw "Governed Intent endpoint failure mapping missing: $_" } }
    $operationalEndpoint = Get-Content -Raw $operationalEndpointPath
    @('PostgreSqlIntentRegistrationReadiness','postgresqlIntentRegistration') | ForEach-Object { if ($operationalEndpoint -notmatch [regex]::Escape($_)) { throw "PostgreSQL readiness disclosure missing: $_" } }
    $openApi = Get-Content -Raw $openApiPath
    @('postgresqlIntentRegistration','unconfigured','invalid','configured') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "PostgreSQL OpenAPI readiness contract missing: $_" } }
    $acceptance = Get-Content -Raw $wave02AcceptancePath
    @('Status: **Satisfied**','all 142 disconnected institutional dependencies fail closed','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 02 acceptance missing: $_" } }
}

$wave03AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_03_ACCEPTANCE.md'
if (Test-Path $wave03AcceptancePath) {
    $changePath = Join-Path $repositoryRoot 'docs\change-control\CR-004-OPERATIONALIZATION-WAVE-03.md'
    $policyContractPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs'
    $policyClientPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\SovereignPolicyEvaluationClient.cs'
    $contextPolicyPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignEnterpriseContextPolicyGate.cs'
    $contextEnginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\AuthorizedEnterpriseContextDiscovery.cs'
    $registrationReaderPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlGovernedIntentRegistrationRepository.cs'
    $evidenceRecorderPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlEnterpriseContextEvidenceRecorder.cs'
    $migrationPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\002_enterprise_context_evidence.sql'
    $graphOptionsPath = Join-Path $repositoryRoot 'backend\Platform.Knowledge\Retrieval\Neo4jEnterpriseGraphOptions.cs'
    $graphSourcePath = Join-Path $repositoryRoot 'backend\Platform.Knowledge\Retrieval\Neo4jEnterpriseGraphRetrievalSource.cs'
    $knowledgeServicesPath = Join-Path $repositoryRoot 'backend\Platform.Knowledge\KnowledgeServiceCollectionExtensions.cs'
    $softwareServicesPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
    $knowledgeProjectPath = Join-Path $repositoryRoot 'backend\Platform.Knowledge\Platform.Knowledge.csproj'
    $knowledgeLockPath = Join-Path $repositoryRoot 'backend\Platform.Knowledge\packages.lock.json'
    $operationalEndpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $guidePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_03_ENTERPRISE_CONTEXT.md'
    @($changePath,$policyContractPath,$policyClientPath,$contextPolicyPath,$contextEnginePath,$registrationReaderPath,$evidenceRecorderPath,$migrationPath,$graphOptionsPath,$graphSourcePath,$knowledgeServicesPath,$softwareServicesPath,$knowledgeProjectPath,$knowledgeLockPath,$operationalEndpointPath,$openApiPath,$guidePath) | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 03 artifact missing: $_" } }

    $change = Get-Content -Raw $changePath
    @('Status: **Approved for Operationalization Wave 03**','Decision: **Approved by the repository owner','Neo4j.Driver` package at version `6.3.0') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 03 approval missing: $_" } }
    $project = Get-Content -Raw $knowledgeProjectPath
    $lock = Get-Content -Raw $knowledgeLockPath
    if ($project -notmatch 'PackageReference Include="Neo4j.Driver" Version="6\.3\.0"') { throw 'Neo4j.Driver is not pinned at the approved version.' }
    @('"requested": "[6.3.0, )"','"resolved": "6.3.0"') | ForEach-Object { if ($lock -notmatch [regex]::Escape($_)) { throw "Neo4j.Driver lock evidence missing: $_" } }
    if ($project -match 'Qdrant|pgvector|Embedding|ObjectGraphMapper') { throw 'An unapproved retrieval or graph-mapping package was introduced.' }

    $graphOptions = Get-Content -Raw $graphOptionsPath
    @('Neo4jEnterpriseGraphConfigurationState.Unconfigured','Neo4jEnterpriseGraphConfigurationState.Invalid','neo4j+s','bolt+s','endpoint.UserInfo','endpoint.Query','endpoint.Fragment','QueryTimeoutSeconds <= 0','MaximumRecords <= 0') | ForEach-Object { if ($graphOptions -notmatch [regex]::Escape($_)) { throw "Neo4j profile guard missing: $_" } }
    $knowledgeServices = Get-Content -Raw $knowledgeServicesPath
    @('options.IsOperationallyConfigured','GraphDatabase.Driver','AuthTokens.Basic') | ForEach-Object { if ($knowledgeServices -notmatch [regex]::Escape($_)) { throw "Neo4j driver composition missing: $_" } }
    $graphSource = Get-Content -Raw $graphSourcePath
    @('RetrievalModality.Graph','UNWIND $resourceIds','tenantId: $tenantId','classificationRank <= $maximumClassificationRank','LIMIT $maximumResults','AccessMode.Read','WithTimeout','scope.MaximumResults > options.MaximumRecords','KnowledgeRetrievalSourceUnavailableException') | ForEach-Object { if ($graphSource -notmatch [regex]::Escape($_)) { throw "Scope-first Enterprise Graph guard missing: $_" } }
    if ($graphSource -match '(?im)^\s*(CREATE|MERGE|SET|DELETE|REMOVE|DROP)\s') { throw 'Enterprise Graph retrieval source contains a write or schema mutation.' }
    if ($graphSource.IndexOf('UNWIND $resourceIds',[StringComparison]::Ordinal) -ge $graphSource.IndexOf('MATCH (resource:EnterpriseObject',[StringComparison]::Ordinal) -or
        $graphSource.IndexOf('MATCH (resource:EnterpriseObject',[StringComparison]::Ordinal) -ge $graphSource.IndexOf('WHERE resource.classificationRank',[StringComparison]::Ordinal)) { throw 'Enterprise Graph query does not establish scope before matching and classification.' }

    $policyContract = Get-Content -Raw $policyContractPath
    @('SovereignPolicyEvaluationScope','MaximumClassification','AllowedResourceIds','AllowedModalities','RequiredRoles','MaximumResults') | ForEach-Object { if ($policyContract -notmatch [regex]::Escape($_)) { throw "Typed policy scope contract missing: $_" } }
    $contextPolicy = Get-Content -Raw $contextPolicyPath
    @('internal-service.enterprise-context.discover','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','RetrievalModality.Graph','scope.MaximumResults','AllowedResourceIds','AllowedModalities','MaximumClassification') | ForEach-Object { if ($contextPolicy -notmatch [regex]::Escape($_)) { throw "Enterprise Context policy adapter guard missing: $_" } }
    if ($contextPolicy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $contextPolicy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'Enterprise Context OPA evaluation does not follow signed bundle verification.' }

    $contextEngine = Get-Content -Raw $contextEnginePath
    if ($contextEngine.IndexOf('registrationReader.LoadAsync',[StringComparison]::Ordinal) -ge $contextEngine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -or
        $contextEngine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $contextEngine.IndexOf('_knowledgeRetriever.RetrieveAsync',[StringComparison]::Ordinal) -or
        $contextEngine.IndexOf('_knowledgeRetriever.RetrieveAsync',[StringComparison]::Ordinal) -ge $contextEngine.IndexOf('evidenceRecorder.RecordAsync',[StringComparison]::Ordinal)) { throw 'Enterprise Context read, policy, retrieval, and evidence order is invalid.' }
    @('ValidateSourceScope','knowledge.context.read','ValidateAndMapContext','CanAdvance: false') | ForEach-Object { if (($contextEngine + (Get-Content -Raw (Join-Path $repositoryRoot 'backend\Platform.Knowledge\Retrieval\AuthorizedKnowledgeRetriever.cs'))) -notmatch [regex]::Escape($_)) { throw "Enterprise Context re-authorization guard missing: $_" } }

    $registrationReader = Get-Content -Raw $registrationReaderPath
    @('IGovernedIntentRegistrationReader','ReadSql','tenant_id = @tenant_id AND registration_id = @registration_id','CommandTimeout','NpgsqlParameter') | ForEach-Object { if ($registrationReader -notmatch [regex]::Escape($_)) { throw "Tenant-scoped registered-intent reader missing: $_" } }
    $evidenceRecorder = Get-Content -Raw $evidenceRecorderPath
    @('IEnterpriseContextEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','RecordSha256Digest','evidence://enterprise-context','CommitAsync','EnterpriseContextDependencyUnavailableException') | ForEach-Object { if ($evidenceRecorder -notmatch [regex]::Escape($_)) { throw "Atomic Enterprise Context evidence guard missing: $_" } }
    if ($evidenceRecorder -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Enterprise Context runtime evidence adapter contains mutation outside append.' }
    $migration = Get-Content -Raw $migrationPath
    @('BEGIN;','software_factory.enterprise_context_evidence','PRIMARY KEY (tenant_id, discovery_id)','record_sha256_digest','record_json jsonb','FOREIGN KEY (tenant_id, registration_id)','ON UPDATE RESTRICT ON DELETE RESTRICT','COMMIT;') | ForEach-Object { if ($migration -notmatch [regex]::Escape($_)) { throw "Enterprise Context migration guard missing: $_" } }

    $softwareServices = Get-Content -Raw $softwareServicesPath
    @('graphOptions.IsOperationallyConfigured','IGovernedIntentRegistrationReader','IEnterpriseContextPolicyGate','IEnterpriseContextEvidenceRecorder','IKnowledgeRetrievalSource, Neo4jEnterpriseGraphRetrievalSource','EnterpriseContextRuntimeReadiness') | ForEach-Object { if ($softwareServices -notmatch [regex]::Escape($_)) { throw "Enterprise Context runtime composition missing: $_" } }
    $operationalEndpoint = Get-Content -Raw $operationalEndpointPath
    @('Neo4jEnterpriseGraphReadiness','EnterpriseContextRuntimeReadiness','neo4jEnterpriseGraph','enterpriseContext') | ForEach-Object { if ($operationalEndpoint -notmatch [regex]::Escape($_)) { throw "Enterprise Context readiness disclosure missing: $_" } }
    $openApi = Get-Content -Raw $openApiPath
    @('neo4jEnterpriseGraph','enterpriseContext','unconfigured','invalid','configured') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Enterprise Context OpenAPI readiness contract missing: $_" } }
    $acceptance = Get-Content -Raw $wave03AcceptancePath
    @('Status: **Satisfied**','all 142 disconnected institutional dependencies fail closed','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 03 acceptance missing: $_" } }
}

if (-not $NoBuild) {
    & dotnet build $solutionPath --no-restore --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed with exit code $LASTEXITCODE."
    }

    $runtimeVerificationPath = Join-Path $repositoryRoot 'scripts\verify-runtime.ps1'
    if (-not (Test-Path -LiteralPath $runtimeVerificationPath)) {
        throw "Missing runtime verification script: $runtimeVerificationPath"
    }
    & $runtimeVerificationPath
}

Write-Output ('VERIFIED: Phase {0:D2}, {1} projects, acceptance and runtime satisfied.' -f $phaseNumber, $projectCount)
