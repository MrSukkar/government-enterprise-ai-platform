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
    @('ISecurityValidationPolicyGate','IAuthorizedStaticValidationReceiptReader','ISecurityValidationCodeGenerationCandidateReader','DeliveryStage.StaticValidation','CodeValidationPipeline','ValidationGate.Security','report.IsAccepted','item.Passed','ISecurityValidationResultAuthorizer','ISecurityValidationEvidenceRecorder','IsExecutable: false','CanAdvance: false','Separately approved Sandbox')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Security Validation guard missing: $_"}}
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
    @('ISandboxPolicyGate','ISandboxSecurityValidationReceiptReader','ISandboxCodeGenerationCandidateReader','DeliveryStage.SecurityValidation','IInstitutionalPackageRegistryReader','IApprovedPackageSupplyChainVerifier','GovernedSandboxService','ISecuritySandboxRuntime','ISandboxResultAuthorizer','ISandboxEvidenceRecorder','ProductionEffectOccurred','CanAdvance','Separately approved Tests')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Sandbox guard missing: $_"}}
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
    @('ITestsPolicyGate','ITestsSandboxExecutionReceiptReader','ITestsSecurityValidationReceiptReader','ITestsCodeGenerationCandidateReader','DeliveryStage.Sandbox','IGovernedTestManifestReader','IInstitutionalPackageRegistryReader','IApprovedPackageSupplyChainVerifier','IGovernedTestRuntime','RequiredTestIds','ManifestDigest','ITestsResultAuthorizer','ITestsEvidenceRecorder','ProductionEffectOccurred','CanAdvance','Separately approved Human Review')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Tests guard missing: $_"}}
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
    @('IHumanReviewPolicyGate','IHumanReviewTestsReceiptReader','IHumanReviewSandboxReceiptReader','IHumanReviewSecurityReceiptReader','IHumanReviewCandidateReader','DeliveryStage.Tests','DeclaredConflictingSubjectIds','ReviewerIsHuman','IHumanReviewAttestationVerifier','IsNonRepudiable','IAtomicHumanReviewRepository','ExpectedVersion','ProductionEffectOccurred','CanAdvance','Separately approved Git boundary')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Human Review guard missing: $_"}}
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
    @('IGitPolicyGate','IGitHumanReviewReceiptReader','IGitTestsReceiptReader','IGitCandidateReader','DeliveryStage.HumanReview','IGovernedGitChangeSetMaterializer','IGitChangePolicyValidator','IInstitutionalGitGateway','CommitSigned','ProtectedBranchMutated','ForceUpdateOccurred','CiCdTriggered','IGitResultAuthorizer','IGitEvidenceRecorder','SourceMutationOccurred','ProductionEffectOccurred','CanAdvance','Separately approved CI/CD')|ForEach-Object{if($engine -notmatch [regex]::Escape($_)){throw "Git guard missing: $_"}}
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

$wave04AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_04_ACCEPTANCE.md'
if (Test-Path $wave04AcceptancePath) {
    $changePath = Join-Path $repositoryRoot 'docs\change-control\CR-005-OPERATIONALIZATION-WAVE-04.md'
    $masterPath = Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md'
    $policyContractPath = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs'
    $systemsPolicyPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignExistingSystemsPolicyGate.cs'
    $systemsEnginePath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\AuthorizedExistingSystemsDiscovery.cs'
    $contextReaderPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAuthorizedEnterpriseContextSnapshotReader.cs'
    $resultAuthorizerPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeterministicExistingSystemResultAuthorizer.cs'
    $graphSourcePath = Join-Path $repositoryRoot 'backend\Platform.Knowledge\Retrieval\Neo4jExistingSystemInventorySource.cs'
    $evidenceRecorderPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlExistingSystemsEvidenceRecorder.cs'
    $migrationPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\003_existing_systems_evidence.sql'
    $servicesPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
    $readinessPath = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\ExistingSystemsRuntimeReadiness.cs'
    $operationalEndpointPath = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
    $openApiPath = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
    $guidePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_04_EXISTING_SYSTEMS.md'
    @($changePath,$masterPath,$policyContractPath,$systemsPolicyPath,$systemsEnginePath,$contextReaderPath,$resultAuthorizerPath,$graphSourcePath,$evidenceRecorderPath,$migrationPath,$servicesPath,$readinessPath,$operationalEndpointPath,$openApiPath,$guidePath) | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 04 artifact missing: $_" } }

    $change = Get-Content -Raw $changePath
    @('Status: **Approved for Operationalization Wave 04**','Decision: **Approved by the repository owner','No new package is requested','Npgsql` `10.0.3','Neo4j.Driver` `6.3.0') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 04 approval missing: $_" } }
    $master = Get-Content -Raw $masterPath
    @('Approved operationalization addendum — CR-005','CR-005-OPERATIONALIZATION-WAVE-04.md') | ForEach-Object { if ($master -notmatch [regex]::Escape($_)) { throw "Master Specification CR-005 authority missing: $_" } }

    $policyContract = Get-Content -Raw $policyContractPath
    @('SovereignExistingSystemsPolicyScope','AllowedSystemIds','AllowedRelationshipTypes','AllowedSourceKind','RequiredRoles','MaximumResults') | ForEach-Object { if ($policyContract -notmatch [regex]::Escape($_)) { throw "Typed Existing Systems policy scope missing: $_" } }
    $systemsPolicy = Get-Content -Raw $systemsPolicyPath
    @('internal-service.existing-systems.discover','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','scope.AllowedSystemIds','scope.AllowedRelationshipTypes','enterprise-graph','scope.RequiredRoles','scope.MaximumResults','mixed scopes from another action') | ForEach-Object { if ($systemsPolicy -notmatch [regex]::Escape($_)) { throw "Existing Systems OPA adapter guard missing: $_" } }
    if ($systemsPolicy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $systemsPolicy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'Existing Systems OPA evaluation does not follow signed bundle verification.' }

    $contextReader = Get-Content -Raw $contextReaderPath
    @('IAuthorizedEnterpriseContextSnapshotReader','tenant_id = @tenant_id AND discovery_id = @discovery_id','CommandTimeout','NpgsqlParameter','record_sha256_digest','JsonSerializer.Deserialize','SHA256.HashData','evidence://enterprise-context','ComputeContextDigest','ExistingSystemsDependencyUnavailableException') | ForEach-Object { if ($contextReader -notmatch [regex]::Escape($_)) { throw "Enterprise Context prerequisite reader guard missing: $_" } }
    if ($contextReader -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Enterprise Context snapshot reader contains a mutation.' }

    $graphSource = Get-Content -Raw $graphSourcePath
    @('IExistingSystemInventorySource','SourceKind => "enterprise-graph"','UNWIND $systemIds','tenantId: $tenantId','classificationRank <= $maximumClassificationRank','system.sourceKind = $sourceKind','target.resourceId IN $systemIds','type(relationship) IN $relationshipTypes','LIMIT $maximumResults','AccessMode.Read','WithTimeout','scope.MaximumResults > options.MaximumRecords','credentialsIncluded','liveSessionIncluded','executableCommandIncluded','externalEffectOccurred') | ForEach-Object { if ($graphSource -notmatch [regex]::Escape($_)) { throw "Scope-first Existing Systems Graph guard missing: $_" } }
    if ($graphSource -match '(?im)^\s*(CREATE|MERGE|SET|DELETE|REMOVE|DROP)\s') { throw 'Existing Systems Graph source contains a write or schema mutation.' }
    if ($graphSource.IndexOf('UNWIND $systemIds',[StringComparison]::Ordinal) -ge $graphSource.IndexOf('MATCH (system:EnterpriseObject',[StringComparison]::Ordinal) -or
        $graphSource.IndexOf('MATCH (system:EnterpriseObject',[StringComparison]::Ordinal) -ge $graphSource.IndexOf('WHERE system.classificationRank',[StringComparison]::Ordinal)) { throw 'Existing Systems Graph query does not establish exact scope before matching and classification.' }

    $systemsEngine = Get-Content -Raw $systemsEnginePath
    if ($systemsEngine.IndexOf('contextReader.LoadAsync',[StringComparison]::Ordinal) -ge $systemsEngine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -or
        $systemsEngine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $systemsEngine.IndexOf('DiscoverFromSourceAsync',[StringComparison]::Ordinal) -or
        $systemsEngine.IndexOf('DiscoverFromSourceAsync',[StringComparison]::Ordinal) -ge $systemsEngine.IndexOf('evidenceRecorder.RecordAsync',[StringComparison]::Ordinal)) { throw 'Existing Systems prerequisite, policy, source, and evidence order is invalid.' }
    @('ValidateCandidate','existing-system.read','existing-system.relationship.read','RequiredRoles','AllowedSystemIds','AllowedRelationshipTypes','AllowedSourceKinds','CanAdvance: false') | ForEach-Object { if ($systemsEngine -notmatch [regex]::Escape($_)) { throw "Existing Systems engine guard missing: $_" } }
    $resultAuthorizer = Get-Content -Raw $resultAuthorizerPath
    @('IAccessPolicyEvaluator','request.Identity','request.RequiredRoles','developer.internal-service.systems.discover','request.AllowedSystemIds','request.AllowedRelationshipTypes','request.AllowedSourceKinds','SHA256.HashData','evidence://existing-systems/authorization') | ForEach-Object { if ($resultAuthorizer -notmatch [regex]::Escape($_)) { throw "Existing Systems result authorization guard missing: $_" } }

    $evidenceRecorder = Get-Content -Raw $evidenceRecorderPath
    @('IExistingSystemsEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','record_sha256_digest','evidence://existing-systems','CommitAsync','ExistingSystemsDependencyUnavailableException') | ForEach-Object { if ($evidenceRecorder -notmatch [regex]::Escape($_)) { throw "Atomic Existing Systems evidence guard missing: $_" } }
    if ($evidenceRecorder -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Existing Systems runtime evidence adapter contains mutation outside append.' }
    $migration = Get-Content -Raw $migrationPath
    @('BEGIN;','software_factory.existing_systems_evidence','PRIMARY KEY (tenant_id, discovery_id)','record_sha256_digest','record_json jsonb','FOREIGN KEY (tenant_id, context_discovery_id)','software_factory.enterprise_context_evidence','ON UPDATE RESTRICT ON DELETE RESTRICT','COMMIT;') | ForEach-Object { if ($migration -notmatch [regex]::Escape($_)) { throw "Existing Systems migration guard missing: $_" } }

    $services = Get-Content -Raw $servicesPath
    @('ExistingSystemsRuntimeReadiness','PostgreSqlAuthorizedEnterpriseContextSnapshotReader','IAuthorizedEnterpriseContextSnapshotReader','IExistingSystemsPolicyGate, SovereignExistingSystemsPolicyGate','IExistingSystemInventorySource, Neo4jExistingSystemInventorySource','IExistingSystemResultAuthorizer','IExistingSystemsEvidenceRecorder') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "Existing Systems runtime composition missing: $_" } }
    $operationalEndpoint = Get-Content -Raw $operationalEndpointPath
    @('ExistingSystemsRuntimeReadiness','existingSystems') | ForEach-Object { if ($operationalEndpoint -notmatch [regex]::Escape($_)) { throw "Existing Systems readiness disclosure missing: $_" } }
    $openApi = Get-Content -Raw $openApiPath
    @('existingSystems','unconfigured','invalid','configured') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Existing Systems OpenAPI readiness contract missing: $_" } }
    $acceptance = Get-Content -Raw $wave04AcceptancePath
    @('Status: **Satisfied**','all 142 institutional runtime dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 04 acceptance missing: $_" } }
}

$wave05AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_05_ACCEPTANCE.md'
if (Test-Path $wave05AcceptancePath) {
    $paths = @{
        Change = Join-Path $repositoryRoot 'docs\change-control\CR-006-OPERATIONALIZATION-WAVE-05.md'
        Master = Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md'
        Engine = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\AuthorizedExistingArchitectureDiscovery.cs'
        Policy = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignExistingArchitecturePolicyGate.cs'
        Reader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAuthorizedExistingSystemsSnapshotReader.cs'
        Conformance = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeterministicExistingArchitectureConformanceValidator.cs'
        Authorization = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeterministicExistingArchitectureResultAuthorizer.cs'
        Graph = Join-Path $repositoryRoot 'backend\Platform.Knowledge\Retrieval\Neo4jExistingArchitectureSource.cs'
        Evidence = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlExistingArchitectureEvidenceRecorder.cs'
        Migration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\004_existing_architecture_evidence.sql'
        Services = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
        Readiness = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\ExistingArchitectureRuntimeReadiness.cs'
        Operations = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
        OpenApi = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
        Guide = Join-Path $repositoryRoot 'docs\operationalization\WAVE_05_EXISTING_ARCHITECTURE.md'
    }
    $paths.Values | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 05 artifact missing: $_" } }
    $change = Get-Content -Raw $paths.Change
    @('Status: **Approved for Operationalization Wave 05**','Decision: **Approved by the repository owner','No new package is requested','Npgsql` `10.0.3','Neo4j.Driver` `6.3.0') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 05 approval missing: $_" } }
    $master = Get-Content -Raw $paths.Master
    @('Approved operationalization addendum — CR-006','CR-006-OPERATIONALIZATION-WAVE-05.md') | ForEach-Object { if ($master -notmatch [regex]::Escape($_)) { throw "Master Specification CR-006 authority missing: $_" } }

    $reader = Get-Content -Raw $paths.Reader
    @('IAuthorizedExistingSystemsSnapshotReader','tenant_id = @tenant_id AND discovery_id = @discovery_id','record_sha256_digest','JsonSerializer.Deserialize','SHA256.HashData','evidence://existing-systems','Digest(record)','ExistingArchitectureDependencyUnavailableException') | ForEach-Object { if ($reader -notmatch [regex]::Escape($_)) { throw "Existing Systems prerequisite reader guard missing: $_" } }
    if ($reader -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Existing Systems snapshot reader contains a mutation.' }
    $policy = Get-Content -Raw $paths.Policy
    @('internal-service.existing-architecture.discover','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','scope.AllowedSystemIds','scope.AllowedItemKinds','scope.AllowedRelationshipTypes','enterprise-graph','scope.RequiredRoles','mixed scopes from another action') | ForEach-Object { if ($policy -notmatch [regex]::Escape($_)) { throw "Existing Architecture OPA guard missing: $_" } }
    if ($policy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $policy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'Existing Architecture OPA evaluation does not follow bundle verification.' }
    $engine = Get-Content -Raw $paths.Engine
    if ($engine.IndexOf('systemsReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('conformanceValidator.ValidateScopeAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('conformanceValidator.ValidateScopeAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('DiscoverFromSourceAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('DiscoverFromSourceAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('evidenceRecorder.RecordAsync',[StringComparison]::Ordinal)) { throw 'Existing Architecture prerequisite, policy, conformance, source, and evidence order is invalid.' }
    @('ValidateCandidate','ValidateItemAsync','existing-architecture.item.read','RequiredRoles','CanAdvance: false') | ForEach-Object { if ($engine -notmatch [regex]::Escape($_)) { throw "Existing Architecture engine guard missing: $_" } }
    $graph = Get-Content -Raw $paths.Graph
    @('IExistingArchitectureSource','SourceKind => "enterprise-graph"','UNWIND $systemIds','tenantId: $tenantId','classificationRank <= $maximumClassificationRank','item.approvalState = $approvedState','item.lifecycle = $activeLifecycle','item.kind IN $itemKinds','item.relationshipType IS NULL OR item.relationshipType IN $relationshipTypes','LIMIT $maximumResults','AccessMode.Read') | ForEach-Object { if ($graph -notmatch [regex]::Escape($_)) { throw "Scope-first Existing Architecture Graph guard missing: $_" } }
    if ($graph -match '(?im)^\s*(CREATE|MERGE|SET|DELETE|REMOVE|DROP)\s') { throw 'Existing Architecture Graph source contains a write or schema mutation.' }
    $conformance = Get-Content -Raw $paths.Conformance
    @('docs/PROJECT_MASTER_SPECIFICATION_V2.md','ExistingArchitectureApprovalState.Approved','LifecycleState.Active','CredentialsIncluded','GeneratedContentIncluded','evidence://existing-architecture/conformance') | ForEach-Object { if ($conformance -notmatch [regex]::Escape($_)) { throw "Constitutional conformance guard missing: $_" } }
    $authorization = Get-Content -Raw $paths.Authorization
    @('IAccessPolicyEvaluator','request.Identity','request.RequiredRoles','developer.internal-service.architecture.discover','request.AllowedSystemIds','request.AllowedItemKinds','request.AllowedRelationshipTypes','request.AllowedSourceKinds','evidence://existing-architecture/authorization') | ForEach-Object { if ($authorization -notmatch [regex]::Escape($_)) { throw "Existing Architecture result authorization guard missing: $_" } }
    $evidence = Get-Content -Raw $paths.Evidence
    @('IExistingArchitectureEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','evidence://existing-architecture','CommitAsync') | ForEach-Object { if ($evidence -notmatch [regex]::Escape($_)) { throw "Existing Architecture evidence guard missing: $_" } }
    if ($evidence -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Existing Architecture runtime evidence adapter contains mutation outside append.' }
    $migration = Get-Content -Raw $paths.Migration
    @('BEGIN;','software_factory.existing_architecture_evidence','PRIMARY KEY (tenant_id, discovery_id)','record_json jsonb','FOREIGN KEY (tenant_id, systems_discovery_id)','software_factory.existing_systems_evidence','COMMIT;') | ForEach-Object { if ($migration -notmatch [regex]::Escape($_)) { throw "Existing Architecture migration guard missing: $_" } }
    $services = Get-Content -Raw $paths.Services
    @('ExistingArchitectureRuntimeReadiness','IAuthorizedExistingSystemsSnapshotReader','IExistingArchitecturePolicyGate','IExistingArchitectureConformanceValidator','IExistingArchitectureSource, Neo4jExistingArchitectureSource','IExistingArchitectureResultAuthorizer','IExistingArchitectureEvidenceRecorder') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "Existing Architecture composition missing: $_" } }
    $operations = Get-Content -Raw $paths.Operations
    @('ExistingArchitectureRuntimeReadiness','existingArchitecture') | ForEach-Object { if ($operations -notmatch [regex]::Escape($_)) { throw "Existing Architecture readiness disclosure missing: $_" } }
    $openApi = Get-Content -Raw $paths.OpenApi
    @('existingArchitecture','unconfigured','invalid','configured') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Existing Architecture OpenAPI readiness contract missing: $_" } }
    $acceptance = Get-Content -Raw $wave05AcceptancePath
    @('Status: **Satisfied**','all 142 institutional runtime dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 05 acceptance missing: $_" } }
}

$wave06AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_06_ACCEPTANCE.md'
if (Test-Path $wave06AcceptancePath) {
    $wave06Paths = @{
        Change = Join-Path $repositoryRoot 'docs\change-control\CR-007-OPERATIONALIZATION-WAVE-06.md'
        Master = Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md'
        Engine = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedApprovedPackagesSelection.cs'
        Policy = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignApprovedPackagesPolicyGate.cs'
        Reader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAuthorizedExistingArchitectureSnapshotReader.cs'
        Registry = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlInstitutionalPackageRegistryReader.cs'
        Trust = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Packages\ApprovedPackagesTrustOptions.cs'
        Attestation = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Packages\PackageSupplyChainAttestation.cs'
        Assurance = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\CryptographicApprovedPackageSupplyChainVerifier.cs'
        Authorization = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeterministicApprovedPackageResultAuthorizer.cs'
        Evidence = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlApprovedPackagesEvidenceRecorder.cs'
        CatalogMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\005_institutional_package_catalog.sql'
        EvidenceMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\006_approved_packages_evidence.sql'
        Services = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
        Operations = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
        OpenApi = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
        Guide = Join-Path $repositoryRoot 'docs\operationalization\WAVE_06_APPROVED_PACKAGES.md'
    }
    $wave06Paths.Values | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 06 artifact missing: $_" } }
    $change = Get-Content -Raw $wave06Paths.Change
    @('Status: **Approved for Operationalization Wave 06**','Decision: **Approved by the repository owner','No new package is requested','Npgsql` `10.0.3','built-in .NET cryptography') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 06 approval missing: $_" } }
    $master = Get-Content -Raw $wave06Paths.Master
    @('Approved operationalization addendum — CR-007','CR-007-OPERATIONALIZATION-WAVE-06.md') | ForEach-Object { if ($master -notmatch [regex]::Escape($_)) { throw "Master Specification CR-007 authority missing: $_" } }
    $reader = Get-Content -Raw $wave06Paths.Reader
    @('IAuthorizedExistingArchitectureSnapshotReader','tenant_id = @tenant_id AND discovery_id = @discovery_id','record_sha256_digest','JsonSerializer.Deserialize','SHA256.HashData','evidence://existing-architecture','Digest(record)','ApprovedPackagesDependencyUnavailableException') | ForEach-Object { if ($reader -notmatch [regex]::Escape($_)) { throw "Existing Architecture prerequisite reader guard missing: $_" } }
    if ($reader -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Existing Architecture snapshot reader contains a mutation.' }
    $policy = Get-Content -Raw $wave06Paths.Policy
    @('internal-service.approved-packages.select','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','scope.AllowedCoordinates','scope.RequiredRoles','scope.MaximumResults','sha256:','mixed scopes from another action') | ForEach-Object { if ($policy -notmatch [regex]::Escape($_)) { throw "Approved Packages OPA guard missing: $_" } }
    if ($policy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $policy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'Approved Packages OPA evaluation does not follow bundle verification.' }
    $engine = Get-Content -Raw $wave06Paths.Engine
    if ($engine.IndexOf('architectureReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('registryReader.FindExactAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('registryReader.FindExactAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('supplyChainVerifier.VerifyAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('supplyChainVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('evidenceRecorder.RecordAsync',[StringComparison]::Ordinal)) { throw 'Approved Packages prerequisite, policy, registry, assurance, authorization, and evidence order is invalid.' }
    @('ValidateExactCoordinate','IPackageEligibilityEvaluator','RequiredRoles','AllowedCoordinates','CanAdvance: false') | ForEach-Object { if ($engine -notmatch [regex]::Escape($_)) { throw "Approved Packages engine guard missing: $_" } }
    $registry = Get-Content -Raw $wave06Paths.Registry
    @('IInstitutionalPackageRegistryReader','package_kind = @package_kind','package_name = @package_name','package_version = @package_version','content_digest = @content_digest','@tenant_id = ANY(allowed_tenant_ids)','@environment = ANY(allowed_environments)','record_sha256_digest','SHA256.HashData','NpgsqlParameter') | ForEach-Object { if ($registry -notmatch [regex]::Escape($_)) { throw "Institutional package catalog guard missing: $_" } }
    if ($registry -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Institutional package catalog reader contains a mutation.' }
    $trust = Get-Content -Raw $wave06Paths.Trust
    @('ApprovedPackagesTrustConfigurationState.Unconfigured','ApprovedPackagesTrustConfigurationState.Invalid','RS256','ES256','TrustedPublicKeysPem') | ForEach-Object { if ($trust -notmatch [regex]::Escape($_)) { throw "Approved Packages trust guard missing: $_" } }
    $assurance = Get-Content -Raw $wave06Paths.Assurance
    @('RSA.Create','ECDsa.Create','ImportFromPem','VerifyHash','CoordinateSha256Digest','ContentSha256Digest','ProvenanceSha256Digest','SbomSha256Digest','SovereignRegistrySha256Digest','PackageTransferred: false','PackageExecuted: false','ExternalEffectOccurred: false') | ForEach-Object { if ($assurance -notmatch [regex]::Escape($_)) { throw "Approved Packages cryptographic assurance guard missing: $_" } }
    $authorization = Get-Content -Raw $wave06Paths.Authorization
    @('IAccessPolicyEvaluator','request.Identity','request.RequiredRoles','developer.internal-service.packages.select','request.AllowedCoordinates','evidence://approved-packages/authorization') | ForEach-Object { if ($authorization -notmatch [regex]::Escape($_)) { throw "Approved Package authorization guard missing: $_" } }
    $evidence = Get-Content -Raw $wave06Paths.Evidence
    @('IApprovedPackagesEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','evidence://approved-packages','CommitAsync') | ForEach-Object { if ($evidence -notmatch [regex]::Escape($_)) { throw "Approved Packages evidence guard missing: $_" } }
    if ($evidence -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Approved Packages evidence adapter contains mutation outside append.' }
    $catalogMigration = Get-Content -Raw $wave06Paths.CatalogMigration
    @('software_factory.institutional_package_catalog','PRIMARY KEY','content_digest','allowed_tenant_ids','allowed_environments','record_json jsonb') | ForEach-Object { if ($catalogMigration -notmatch [regex]::Escape($_)) { throw "Package catalog migration guard missing: $_" } }
    $evidenceMigration = Get-Content -Raw $wave06Paths.EvidenceMigration
    @('software_factory.approved_packages_evidence','PRIMARY KEY (tenant_id, selection_id)','FOREIGN KEY (tenant_id, architecture_discovery_id)','software_factory.existing_architecture_evidence','record_json jsonb') | ForEach-Object { if ($evidenceMigration -notmatch [regex]::Escape($_)) { throw "Approved Packages evidence migration guard missing: $_" } }
    $services = Get-Content -Raw $wave06Paths.Services
    @('ApprovedPackagesRuntimeReadiness','ApprovedPackagesTrustOptions','IAuthorizedExistingArchitectureSnapshotReader','IApprovedPackagesPolicyGate','IInstitutionalPackageRegistryReader','IApprovedPackageSupplyChainVerifier','IApprovedPackageResultAuthorizer','IApprovedPackagesEvidenceRecorder') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "Approved Packages composition missing: $_" } }
    $operations = Get-Content -Raw $wave06Paths.Operations
    @('ApprovedPackagesRuntimeReadiness','approvedPackages') | ForEach-Object { if ($operations -notmatch [regex]::Escape($_)) { throw "Approved Packages readiness disclosure missing: $_" } }
    $openApi = Get-Content -Raw $wave06Paths.OpenApi
    @('approvedPackages','unconfigured','invalid','configured') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Approved Packages OpenAPI readiness missing: $_" } }
    $acceptance = Get-Content -Raw $wave06AcceptancePath
    @('Status: **Satisfied**','all 142 institutional runtime dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 06 acceptance missing: $_" } }
}

$wave07AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_07_ACCEPTANCE.md'
if (Test-Path $wave07AcceptancePath) {
    $wave07Paths = @{
        Change = Join-Path $repositoryRoot 'docs\change-control\CR-008-OPERATIONALIZATION-WAVE-07.md'
        Master = Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md'
        Engine = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedAiPlanningCandidate.cs'
        Policy = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignAiPlanningPolicyGate.cs'
        PackagesReader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAuthorizedApprovedPackagesSnapshotReader.cs'
        RunReader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAiPlanningDeliveryRunReader.cs'
        PromptReader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlGovernedPlanningPromptTemplateReader.cs'
        Context = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAiPlanningContextAuthorizer.cs'
        Options = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\AiPlanningRuntimeOptions.cs'
        Runtime = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignHttpAiPlanningRuntime.cs'
        Evaluator = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignHttpAiOutputEvaluator.cs'
        Authorization = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeterministicAiPlanningResultAuthorizer.cs'
        Evidence = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAiPlanningEvidenceRecorder.cs'
        InputsMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\007_ai_planning_inputs.sql'
        EvidenceMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\008_ai_planning_evidence.sql'
        Services = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
        Operations = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
        OpenApi = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
        Guide = Join-Path $repositoryRoot 'docs\operationalization\WAVE_07_AI_PLANNING.md'
    }
    $wave07Paths.Values | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 07 artifact missing: $_" } }
    $change = Get-Content -Raw $wave07Paths.Change
    @('Status: **Approved for Operationalization Wave 07**','Decision: **Approved by the repository owner','No new package is requested','Npgsql` `10.0.3','built-in .NET cryptography') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 07 approval missing: $_" } }
    $master = Get-Content -Raw $wave07Paths.Master
    @('CR-008','CR-008-OPERATIONALIZATION-WAVE-07.md','provider-neutral sovereign HTTPS planning protocol') | ForEach-Object { if ($master -notmatch [regex]::Escape($_)) { throw "Master Specification CR-008 authority missing: $_" } }
    $packagesReader = Get-Content -Raw $wave07Paths.PackagesReader
    @('IAuthorizedApprovedPackagesSnapshotReader','packages.tenant_id = @tenant_id AND packages.selection_id = @selection_id','JOIN software_factory.existing_architecture_evidence','record_sha256_digest','JsonSerializer.Deserialize','SHA256.HashData','evidence://approved-packages','SelectionDigest(record)','AiPlanningDependencyUnavailableException') | ForEach-Object { if ($packagesReader -notmatch [regex]::Escape($_)) { throw "Approved Packages prerequisite reader guard missing: $_" } }
    if ($packagesReader -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Approved Packages snapshot reader contains a mutation.' }
    $runReader = Get-Content -Raw $wave07Paths.RunReader
    @('IAiPlanningDeliveryRunReader','tenant_id = @tenant_id AND run_id = @run_id','package_selection_id = @package_selection_id','selection_sha256_digest = @selection_sha256_digest','purpose = @purpose','record_sha256_digest','SHA256.HashData','evidence://delivery-runs','DeliveryStage.ApprovedPackages') | ForEach-Object { if ($runReader -notmatch [regex]::Escape($_)) { throw "AI Planning delivery-run reader guard missing: $_" } }
    if ($runReader -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'AI Planning delivery-run reader contains a mutation.' }
    $policy = Get-Content -Raw $wave07Paths.Policy
    @('internal-service.ai-planning.create','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','envelope.AiPlanning','AllowedPromptSha256Digest','AllowedRuntimeProfile','AllowedContextReferences','AllowedPackages','AllowedConstraints','RequiredRoles','OutputKind','RequestTimeoutSeconds','MaximumRequestBytes','MaximumResponseBytes','mixed scopes from another action') | ForEach-Object { if ($policy -notmatch [regex]::Escape($_)) { throw "AI Planning OPA guard missing: $_" } }
    if ($policy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $policy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'AI Planning OPA evaluation does not follow bundle verification.' }
    $engine = Get-Content -Raw $wave07Paths.Engine
    if ($engine.IndexOf('packagesReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('promptReader.LoadExactAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('promptReader.LoadExactAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('contextAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('contextAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('ProduceCandidateAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('ProduceCandidateAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('evidenceRecorder.RecordAsync',[StringComparison]::Ordinal)) { throw 'AI Planning prerequisite, policy, prompt, context, runtime, authorization, and evidence order is invalid.' }
    @('AiDevelopmentTaskKind.Planning','VerifiedPromptContent','AuthorizedContextItems','runtime.RequestTimeoutSeconds','IsExecutable: false','CanAdvance: false','Separately approved Code Generation') | ForEach-Object { if (($engine + (Get-Content -Raw (Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\AiDevelopment\AiDevelopmentRequest.cs'))) -notmatch [regex]::Escape($_)) { throw "AI Planning engine guard missing: $_" } }
    $options = Get-Content -Raw $wave07Paths.Options
    @('AiPlanningRuntimeConfigurationState.Unconfigured','AiPlanningRuntimeConfigurationState.Invalid','GenerationEndpoint','EvaluationEndpoint','GenerationOperatorId','EvaluationOperatorId','PromptTrustedPublicKeysPem','GenerationTrustedPublicKeysPem','EvaluationTrustedPublicKeysPem','RequestTimeoutSeconds','MaximumRequestBytes','MaximumResponseBytes','Uri.UriSchemeHttps') | ForEach-Object { if ($options -notmatch [regex]::Escape($_)) { throw "AI Planning runtime configuration guard missing: $_" } }
    $prompt = Get-Content -Raw $wave07Paths.PromptReader
    @('governed_planning_prompt_templates','@tenant_id = ANY(allowed_tenant_ids)','@purpose = ANY(allowed_purposes)','@environment = ANY(allowed_environments)','content_sha256_digest','SHA256.HashData','AiPlanningSignatureVerifier.Verify','PromptTrustedPublicKeysPem','SignatureValid: true') | ForEach-Object { if ($prompt -notmatch [regex]::Escape($_)) { throw "Governed planning prompt guard missing: $_" } }
    $context = Get-Content -Raw $wave07Paths.Context
    @('enterprise_context_evidence','existing_systems_evidence','existing_architecture_evidence','approved_packages_evidence','evidence_reference = @evidence_reference','IAccessPolicyEvaluator','ai-context.read','developer.internal-service.ai-planning.create','AiDevelopmentContextItem') | ForEach-Object { if ($context -notmatch [regex]::Escape($_)) { throw "AI Planning context guard missing: $_" } }
    if ($context -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'AI Planning context adapter contains a mutation.' }
    $runtime = Get-Content -Raw $wave07Paths.Runtime
    @('IHttpClientFactory','sovereign-ai-planning-generation','ToolsEnabled: false','GeneratedFilesAllowed: false','MaximumRequestBytes','MaximumResponseBytes','ResponseHeadersRead','ReadBoundedAsync','GenerationTrustedPublicKeysPem','AiPlanningSignatureVerifier.Verify') | ForEach-Object { if ($runtime -notmatch [regex]::Escape($_)) { throw "Sovereign AI Planning runtime guard missing: $_" } }
    $evaluator = Get-Content -Raw $wave07Paths.Evaluator
    @('sovereign-ai-planning-evaluation','EvaluationOperatorId','EvaluationRuntimeProfile','EvaluationTrustedPublicKeysPem','IsIndependentFromGenerationRuntime','Enum.GetValues<AiEvaluationCriterion>','AiPlanningSignatureVerifier.Verify') | ForEach-Object { if ($evaluator -notmatch [regex]::Escape($_)) { throw "Independent AI evaluator guard missing: $_" } }
    $authorization = Get-Content -Raw $wave07Paths.Authorization
    @('IAccessPolicyEvaluator','request.Identity','request.RequiredRoles','developer.internal-service.ai-planning.create','request.ContextSha256Digests','request.AllowedPackages','evidence://ai-planning/result-authorization') | ForEach-Object { if ($authorization -notmatch [regex]::Escape($_)) { throw "AI Planning result authorization guard missing: $_" } }
    $evidence = Get-Content -Raw $wave07Paths.Evidence
    @('IAiPlanningEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','evidence://ai-planning','CommitAsync') | ForEach-Object { if ($evidence -notmatch [regex]::Escape($_)) { throw "AI Planning evidence guard missing: $_" } }
    if ($evidence -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'AI Planning evidence adapter contains mutation outside append.' }
    $inputsMigration = Get-Content -Raw $wave07Paths.InputsMigration
    @('ai_planning_delivery_run_snapshots','governed_planning_prompt_templates','PRIMARY KEY (tenant_id, run_id)','package_selection_id uuid NOT NULL','selection_sha256_digest text NOT NULL','purpose text NOT NULL','fk_ai_planning_delivery_run_packages','signature_algorithm','signature_evidence_reference','COMMIT;') | ForEach-Object { if ($inputsMigration -notmatch [regex]::Escape($_)) { throw "AI Planning input migration guard missing: $_" } }
    $evidenceMigration = Get-Content -Raw $wave07Paths.EvidenceMigration
    @('software_factory.ai_planning_evidence','PRIMARY KEY (tenant_id, planning_id)','FOREIGN KEY (tenant_id, package_selection_id)','FOREIGN KEY (tenant_id, delivery_run_id)','record_json jsonb') | ForEach-Object { if ($evidenceMigration -notmatch [regex]::Escape($_)) { throw "AI Planning evidence migration guard missing: $_" } }
    $services = Get-Content -Raw $wave07Paths.Services
    @('AiPlanningRuntimeReadiness','AiPlanningRuntimeOptions','AllowAutoRedirect = false','IAuthorizedApprovedPackagesSnapshotReader','IAiPlanningDeliveryRunReader','IAiPlanningPolicyGate','IGovernedPlanningPromptTemplateReader','IAiPlanningContextAuthorizer','IAiDevelopmentRuntime','IAiOutputEvaluator','IAiPlanningResultAuthorizer','IAiPlanningEvidenceRecorder') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "AI Planning composition missing: $_" } }
    $operations = Get-Content -Raw $wave07Paths.Operations
    @('AiPlanningRuntimeReadiness','aiPlanning') | ForEach-Object { if ($operations -notmatch [regex]::Escape($_)) { throw "AI Planning readiness disclosure missing: $_" } }
    $openApi = Get-Content -Raw $wave07Paths.OpenApi
    @('aiPlanning','unconfigured','invalid','configured') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "AI Planning OpenAPI readiness missing: $_" } }
    $acceptance = Get-Content -Raw $wave07AcceptancePath
    @('Status: **Satisfied**','all 142 institutional runtime dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 07 acceptance missing: $_" } }
}

$wave08AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_08_ACCEPTANCE.md'
if (Test-Path $wave08AcceptancePath) {
    $wave08Paths = @{
        Change = Join-Path $repositoryRoot 'docs\change-control\CR-009-OPERATIONALIZATION-WAVE-08.md'
        Master = Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md'
        Engine = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedCodeGenerationCandidate.cs'
        Policy = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignCodeGenerationPolicyGate.cs'
        PlanningReader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAuthorizedAiPlanningCandidateReader.cs'
        RunReader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlCodeGenerationDeliveryRunReader.cs'
        PromptReader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlGovernedCodeGenerationPromptTemplateReader.cs'
        Context = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlCodeGenerationContextAuthorizer.cs'
        Options = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\CodeGenerationRuntimeOptions.cs'
        Runtime = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignHttpCodeGenerationRuntime.cs'
        Evaluator = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignHttpCodeGenerationEvaluator.cs'
        Authorization = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeterministicCodeGenerationResultAuthorizer.cs'
        Evidence = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlCodeGenerationEvidenceRecorder.cs'
        InputsMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\009_code_generation_inputs.sql'
        EvidenceMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\010_code_generation_evidence.sql'
        Services = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
        Endpoint = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
        Operations = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
        OpenApi = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
        Guide = Join-Path $repositoryRoot 'docs\operationalization\WAVE_08_CODE_GENERATION.md'
    }
    $wave08Paths.Values | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 08 artifact missing: $_" } }
    $change = Get-Content -Raw $wave08Paths.Change
    @('Status: **Approved for Operationalization Wave 08**','Decision: **Approved by the repository owner','No new package','Npgsql` `10.0.3','built-in .NET') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 08 approval missing: $_" } }
    $master = Get-Content -Raw $wave08Paths.Master
    @('CR-009','CR-009-OPERATIONALIZATION-WAVE-08.md','provider-neutral sovereign HTTPS generation protocol') | ForEach-Object { if ($master -notmatch [regex]::Escape($_)) { throw "Master Specification CR-009 authority missing: $_" } }
    $planningReader = Get-Content -Raw $wave08Paths.PlanningReader
    @('IAuthorizedAiPlanningCandidateReader','tenant_id = @tenant_id AND planning_id = @planning_id','package_selection_id = @package_selection_id','candidate_sha256_digest = @planning_sha256_digest','JsonSerializer.Deserialize<AiPlanningEvidenceRecord>','SHA256.HashData','evidence://ai-planning','record.Candidate.GeneratedFilePaths.Length != 0','record.Evaluation.IsAccepted') | ForEach-Object { if ($planningReader -notmatch [regex]::Escape($_)) { throw "Code Generation planning reader guard missing: $_" } }
    if ($planningReader -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Code Generation planning reader contains mutation.' }
    $runReader = Get-Content -Raw $wave08Paths.RunReader
    @('ICodeGenerationDeliveryRunReader','planning_id = @planning_id','planning_sha256_digest = @planning_sha256_digest','package_selection_id = @package_selection_id','selection_sha256_digest = @selection_sha256_digest','purpose = @purpose','SHA256.HashData','DeliveryStage.AiPlanning') | ForEach-Object { if ($runReader -notmatch [regex]::Escape($_)) { throw "Code Generation delivery-run guard missing: $_" } }
    if ($runReader -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Code Generation delivery-run reader contains mutation.' }
    $policy = Get-Content -Raw $wave08Paths.Policy
    @('internal-service.code-generation.create','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','envelope.CodeGeneration','AllowedPromptSha256Digest','AllowedRuntimeProfile','AllowedContextReferences','AllowedPackages','AllowedConstraints','AllowedOutputPaths','RequiredRoles','inert-code-candidate','RequestTimeoutSeconds','MaximumRequestBytes','MaximumResponseBytes','mixed scopes from another action') | ForEach-Object { if ($policy -notmatch [regex]::Escape($_)) { throw "Code Generation OPA guard missing: $_" } }
    if ($policy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $policy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'Code Generation OPA evaluation does not follow bundle verification.' }
    $engine = Get-Content -Raw $wave08Paths.Engine
    if ($engine.IndexOf('planningReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('packagesReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('packagesReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('promptReader.LoadExactAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('promptReader.LoadExactAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('contextAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('contextAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('ProduceCandidateAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('ProduceCandidateAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('evidenceRecorder.RecordAsync',[StringComparison]::Ordinal)) { throw 'Code Generation prerequisite, policy, prompt, context, runtime, authorization, and evidence order is invalid.' }
    @('AiDevelopmentTaskKind.CodeGeneration','VerifiedPromptContent','AuthorizedContextItems','AuthorizedOutputPaths','runtime.RequestTimeoutSeconds','ProhibitedSegments','ProhibitedExtensions','IsExecutable: false','IsApplied: false','CanAdvance: false','Separately approved Static Validation') | ForEach-Object { if (($engine + (Get-Content -Raw (Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\AiDevelopment\AiDevelopmentRequest.cs'))) -notmatch [regex]::Escape($_)) { throw "Code Generation engine guard missing: $_" } }
    $options = Get-Content -Raw $wave08Paths.Options
    @('CodeGenerationRuntimeConfigurationState.Unconfigured','CodeGenerationRuntimeConfigurationState.Invalid','GenerationEndpoint','EvaluationEndpoint','GenerationOperatorId','EvaluationOperatorId','PromptTrustedPublicKeysPem','GenerationTrustedPublicKeysPem','EvaluationTrustedPublicKeysPem','RequestTimeoutSeconds','MaximumRequestBytes','MaximumResponseBytes','Uri.UriSchemeHttps') | ForEach-Object { if ($options -notmatch [regex]::Escape($_)) { throw "Code Generation runtime configuration guard missing: $_" } }
    $prompt = Get-Content -Raw $wave08Paths.PromptReader
    @('governed_code_generation_prompt_templates','@tenant_id = ANY(allowed_tenant_ids)','@purpose = ANY(allowed_purposes)','@environment = ANY(allowed_environments)','content_sha256_digest','SHA256.HashData','AiPlanningSignatureVerifier.Verify','PromptTrustedPublicKeysPem') | ForEach-Object { if ($prompt -notmatch [regex]::Escape($_)) { throw "Code Generation prompt guard missing: $_" } }
    $context = Get-Content -Raw $wave08Paths.Context
    @('enterprise_context_evidence','existing_systems_evidence','existing_architecture_evidence','approved_packages_evidence','ai_planning_evidence','planning_id = @planning_id','IAccessPolicyEvaluator','ai-context.read','developer.internal-service.code-generation.create','AiDevelopmentContextItem') | ForEach-Object { if ($context -notmatch [regex]::Escape($_)) { throw "Code Generation context guard missing: $_" } }
    if ($context -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Code Generation context adapter contains mutation.' }
    $runtime = Get-Content -Raw $wave08Paths.Runtime
    @('ICodeGenerationAiDevelopmentRuntime','sovereign-code-generation','ToolsEnabled: false','FilesystemWriteEnabled: false','CommandsEnabled: false','GeneratedFilesApplied: false','AuthorizedOutputPaths','ResponseHeadersRead','ReadBoundedAsync','GenerationTrustedPublicKeysPem','AiPlanningSignatureVerifier.Verify') | ForEach-Object { if ($runtime -notmatch [regex]::Escape($_)) { throw "Sovereign Code Generation runtime guard missing: $_" } }
    $evaluator = Get-Content -Raw $wave08Paths.Evaluator
    @('ICodeGenerationAiOutputEvaluator','sovereign-code-generation-evaluation','EvaluationOperatorId','EvaluationRuntimeProfile','EvaluationTrustedPublicKeysPem','IsIndependentFromGenerationRuntime','Enum.GetValues<AiEvaluationCriterion>','AiPlanningSignatureVerifier.Verify') | ForEach-Object { if ($evaluator -notmatch [regex]::Escape($_)) { throw "Independent Code Generation evaluator guard missing: $_" } }
    $authorization = Get-Content -Raw $wave08Paths.Authorization
    @('IAccessPolicyEvaluator','request.Identity','request.RequiredRoles','developer.internal-service.code-generation.create','request.ContextSha256Digests','request.ApprovedPackages','request.AllowedOutputPaths','evidence://code-generation/result-authorization') | ForEach-Object { if ($authorization -notmatch [regex]::Escape($_)) { throw "Code Generation result authorization guard missing: $_" } }
    $evidence = Get-Content -Raw $wave08Paths.Evidence
    @('ICodeGenerationEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','evidence://code-generation','CommitAsync') | ForEach-Object { if ($evidence -notmatch [regex]::Escape($_)) { throw "Code Generation evidence guard missing: $_" } }
    if ($evidence -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Code Generation evidence adapter contains mutation outside append.' }
    $inputsMigration = Get-Content -Raw $wave08Paths.InputsMigration
    @('code_generation_delivery_run_snapshots','governed_code_generation_prompt_templates','planning_sha256_digest text NOT NULL','selection_sha256_digest text NOT NULL','fk_code_generation_run_planning','signature_algorithm','COMMIT;') | ForEach-Object { if ($inputsMigration -notmatch [regex]::Escape($_)) { throw "Code Generation input migration guard missing: $_" } }
    $evidenceMigration = Get-Content -Raw $wave08Paths.EvidenceMigration
    @('software_factory.code_generation_evidence','PRIMARY KEY (tenant_id, generation_id)','FOREIGN KEY (tenant_id, planning_id)','FOREIGN KEY (tenant_id, package_selection_id)','FOREIGN KEY (tenant_id, delivery_run_id)','record_json jsonb') | ForEach-Object { if ($evidenceMigration -notmatch [regex]::Escape($_)) { throw "Code Generation evidence migration guard missing: $_" } }
    $services = Get-Content -Raw $wave08Paths.Services
    @('CodeGenerationRuntimeReadiness','CodeGenerationRuntimeOptions','AllowAutoRedirect = false','IAuthorizedAiPlanningCandidateReader','ICodeGenerationDeliveryRunReader','ICodeGenerationPolicyGate','IGovernedCodeGenerationPromptTemplateReader','ICodeGenerationContextAuthorizer','ICodeGenerationAiDevelopmentRuntime','ICodeGenerationAiOutputEvaluator','ICodeGenerationResultAuthorizer','ICodeGenerationEvidenceRecorder') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "Code Generation composition missing: $_" } }
    $endpoint = Get-Content -Raw $wave08Paths.Endpoint
    @('GetService<ICodeGenerationAiDevelopmentRuntime>','GetService<ICodeGenerationAiOutputEvaluator>','Governed Code Generation is not operationally ready') | ForEach-Object { if ($endpoint -notmatch [regex]::Escape($_)) { throw "Code Generation endpoint composition missing: $_" } }
    $operations = Get-Content -Raw $wave08Paths.Operations
    @('CodeGenerationRuntimeReadiness','codeGeneration') | ForEach-Object { if ($operations -notmatch [regex]::Escape($_)) { throw "Code Generation readiness disclosure missing: $_" } }
    $openApi = Get-Content -Raw $wave08Paths.OpenApi
    @('codeGeneration','unconfigured','invalid','configured') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Code Generation OpenAPI readiness missing: $_" } }
    $acceptance = Get-Content -Raw $wave08AcceptancePath
    @('Status: **Satisfied**','all 142 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Operationalization Wave 08 acceptance missing: $_" } }
}

$wave09AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_09_ACCEPTANCE.md'
if (Test-Path $wave09AcceptancePath) {
    $wave09Paths = @{
        Change = Join-Path $repositoryRoot 'docs\change-control\CR-010-OPERATIONALIZATION-WAVE-09.md'
        Master = Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md'
        Engine = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedStaticValidation.cs'
        Policy = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignStaticValidationPolicyGate.cs'
        Candidate = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAuthorizedCodeGenerationCandidateReader.cs'
        Run = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlStaticValidationDeliveryRunReader.cs'
        Controls = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\StaticValidationRuntimeOptions.cs'
        Authorization = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeterministicStaticValidationResultAuthorizer.cs'
        Evidence = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlStaticValidationEvidenceRecorder.cs'
        InputsMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\011_static_validation_inputs.sql'
        EvidenceMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\012_static_validation_evidence.sql'
        Services = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
        Operations = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
        OpenApi = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
        Guide = Join-Path $repositoryRoot 'docs\operationalization\WAVE_09_STATIC_VALIDATION.md'
    }
    $wave09Paths.Values | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 09 artifact missing: $_" } }
    $change = Get-Content -Raw $wave09Paths.Change
    @('Status: **Approved for Operationalization Wave 09**','Decision: **Approved by the repository owner','No new package','deterministic in-process Static control profiles') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Wave 09 approval missing: $_" } }
    $master = Get-Content -Raw $wave09Paths.Master
    @('CR-010','CR-010-OPERATIONALIZATION-WAVE-09.md','cryptographically signed deterministic in-process Static control profiles') | ForEach-Object { if ($master -notmatch [regex]::Escape($_)) { throw "Master CR-010 authority missing: $_" } }
    $policy = Get-Content -Raw $wave09Paths.Policy
    @('internal-service.static-validation.create','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','envelope.StaticValidation','AllowedControlIds','RequiredRoles','static-report','mixed scopes from another action') | ForEach-Object { if ($policy -notmatch [regex]::Escape($_)) { throw "Static OPA guard missing: $_" } }
    if ($policy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $policy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'Static OPA evaluation does not follow bundle verification.' }
    $engine = Get-Content -Raw $wave09Paths.Engine
    if ($engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('candidateReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('candidateReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('CodeValidationPipeline',[StringComparison]::Ordinal) -or
        $engine.IndexOf('CodeValidationPipeline',[StringComparison]::Ordinal) -ge $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('evidenceRecorder.RecordAsync',[StringComparison]::Ordinal)) { throw 'Static policy/read/control/authorization/evidence order is invalid.' }
    @('ValidationGate.Static','IsExecutable: false','CanAdvance: false','Separately approved Security Validation','RequiredRoles','static-report') | ForEach-Object { if ($engine -notmatch [regex]::Escape($_)) { throw "Static engine guard missing: $_" } }
    $candidate = Get-Content -Raw $wave09Paths.Candidate
    @('IStaticValidationCodeGenerationCandidateReader','candidate_sha256_digest = @candidate_sha256_digest','evidence_reference = @evidence_reference','JsonSerializer.Deserialize<CodeGenerationEvidenceRecord>','SHA256.HashData','GovernedGeneratedPath.Validate','record.Evaluation.IsAccepted') | ForEach-Object { if ($candidate -notmatch [regex]::Escape($_)) { throw "Static candidate reader guard missing: $_" } }
    if ($candidate -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Static candidate reader contains mutation.' }
    $run = Get-Content -Raw $wave09Paths.Run
    @('IStaticValidationDeliveryRunReader','purpose = @purpose','generation_id = @generation_id','candidate_sha256_digest = @candidate_sha256_digest','SHA256.HashData','DeliveryStage.CodeGeneration') | ForEach-Object { if ($run -notmatch [regex]::Escape($_)) { throw "Static run reader guard missing: $_" } }
    if ($run -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Static run reader contains mutation.' }
    $controls = Get-Content -Raw $wave09Paths.Controls
    @('StaticValidationRuntimeConfigurationState.Unconfigured','TrustedPublicKeysPem','SignedDeterministicStaticValidationControl','ValidationGate.Static','AiPlanningSignatureVerifier.Verify','RequiredText','ForbiddenText','AllowedFileExtensions','ValidationSeverity.Error','evidence://static-validation/controls') | ForEach-Object { if ($controls -notmatch [regex]::Escape($_)) { throw "Signed Static control guard missing: $_" } }
    if ($controls -match 'HttpClient|Process|File\.Write|Directory\.Create|ExecuteNonQuery') { throw 'Static control exposes an unauthorized effect capability.' }
    $authorization = Get-Content -Raw $wave09Paths.Authorization
    @('IAccessPolicyEvaluator','request.Identity','request.RequiredRoles','developer.internal-service.static-validation.create','static-validation-report.read','evidence://static-validation/result-authorization') | ForEach-Object { if ($authorization -notmatch [regex]::Escape($_)) { throw "Static authorization guard missing: $_" } }
    $evidence = Get-Content -Raw $wave09Paths.Evidence
    @('IStaticValidationEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','evidence://static-validation','CommitAsync') | ForEach-Object { if ($evidence -notmatch [regex]::Escape($_)) { throw "Static evidence guard missing: $_" } }
    if ($evidence -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Static evidence adapter contains mutation outside append.' }
    $inputs = Get-Content -Raw $wave09Paths.InputsMigration
    @('static_validation_delivery_run_snapshots','generation_id uuid NOT NULL','candidate_sha256_digest text NOT NULL','purpose text NOT NULL','fk_static_validation_run_generation','COMMIT;') | ForEach-Object { if ($inputs -notmatch [regex]::Escape($_)) { throw "Static input migration guard missing: $_" } }
    $evidenceMigration = Get-Content -Raw $wave09Paths.EvidenceMigration
    @('software_factory.static_validation_evidence','PRIMARY KEY (tenant_id, validation_id)','FOREIGN KEY (tenant_id, generation_id)','FOREIGN KEY (tenant_id, delivery_run_id)','record_json jsonb') | ForEach-Object { if ($evidenceMigration -notmatch [regex]::Escape($_)) { throw "Static evidence migration guard missing: $_" } }
    $services = Get-Content -Raw $wave09Paths.Services
    @('StaticValidationRuntimeReadiness','StaticValidationRuntimeOptions','IStaticValidationPolicyGate','IStaticValidationCodeGenerationCandidateReader','IStaticValidationDeliveryRunReader','ICodeValidationControl','SignedDeterministicStaticValidationControl','IStaticValidationResultAuthorizer','IStaticValidationEvidenceRecorder') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "Static composition missing: $_" } }
    $operations = Get-Content -Raw $wave09Paths.Operations
    @('StaticValidationRuntimeReadiness','staticValidation') | ForEach-Object { if ($operations -notmatch [regex]::Escape($_)) { throw "Static readiness missing: $_" } }
    $openApi = Get-Content -Raw $wave09Paths.OpenApi
    @('staticValidation','unconfigured','invalid','configured') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Static OpenAPI readiness missing: $_" } }
    $acceptance = Get-Content -Raw $wave09AcceptancePath
    @('Status: **Satisfied**','all 142 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Wave 09 acceptance missing: $_" } }
}

$wave10AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_10_ACCEPTANCE.md'
if (Test-Path $wave10AcceptancePath) {
    $wave10Paths = @{
        Change = Join-Path $repositoryRoot 'docs\change-control\CR-011-OPERATIONALIZATION-WAVE-10.md'
        Master = Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md'
        Envelope = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs'
        Engine = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedSecurityValidation.cs'
        Policy = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignSecurityValidationPolicyGate.cs'
        StaticReader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAuthorizedStaticValidationReceiptReader.cs'
        Candidate = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlSecurityValidationCodeGenerationCandidateReader.cs'
        Run = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlSecurityValidationDeliveryRunReader.cs'
        Controls = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SecurityValidationRuntimeOptions.cs'
        Authorization = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeterministicSecurityValidationResultAuthorizer.cs'
        Evidence = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlSecurityValidationEvidenceRecorder.cs'
        InputsMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\013_security_validation_inputs.sql'
        EvidenceMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\014_security_validation_evidence.sql'
        Services = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
        Readiness = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
        Operations = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
        Endpoint = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
        OpenApi = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
        Guide = Join-Path $repositoryRoot 'docs\operationalization\WAVE_10_SECURITY_VALIDATION.md'
    }
    $wave10Paths.Values | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 10 artifact missing: $_" } }
    $change = Get-Content -Raw $wave10Paths.Change
    @('Status: **Approved for Operationalization Wave 10**','Decision: **Approved by the repository owner','No scanner vendor','deterministic in-process Security control profiles') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Wave 10 approval missing: $_" } }
    $master = Get-Content -Raw $wave10Paths.Master
    @('CR-011','CR-011-OPERATIONALIZATION-WAVE-10.md','cryptographically signed deterministic in-process Security control profiles') | ForEach-Object { if ($master -notmatch [regex]::Escape($_)) { throw "Master CR-011 authority missing: $_" } }
    $envelope = Get-Content -Raw $wave10Paths.Envelope
    @('SovereignSecurityValidationPolicyScope','SecurityValidation') | ForEach-Object { if ($envelope -notmatch [regex]::Escape($_)) { throw "Security policy envelope missing: $_" } }
    $policy = Get-Content -Raw $wave10Paths.Policy
    @('internal-service.security-validation.create','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','envelope.SecurityValidation','AllowedControlIds','RequiredRoles','security-report','mixed scopes from another action') | ForEach-Object { if ($policy -notmatch [regex]::Escape($_)) { throw "Security OPA guard missing: $_" } }
    if ($policy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $policy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'Security OPA evaluation does not follow bundle verification.' }
    $engine = Get-Content -Raw $wave10Paths.Engine
    if ($engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('staticReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('staticReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('candidateReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('candidateReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('CodeValidationPipeline',[StringComparison]::Ordinal) -or
        $engine.IndexOf('CodeValidationPipeline',[StringComparison]::Ordinal) -ge $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('evidenceRecorder.RecordAsync',[StringComparison]::Ordinal)) { throw 'Security policy/read/control/authorization/evidence order is invalid.' }
    @('ISecurityValidationCodeGenerationCandidateReader','ValidationGate.Security','DeliveryStage.StaticValidation','IsExecutable: false','CanAdvance: false','Separately approved Sandbox','RequiredRoles','security-report') | ForEach-Object { if ($engine -notmatch [regex]::Escape($_)) { throw "Security engine guard missing: $_" } }
    $staticReader = Get-Content -Raw $wave10Paths.StaticReader
    @('IAuthorizedStaticValidationReceiptReader','purpose','candidate_sha256_digest = @candidate_sha256_digest','report_sha256_digest = @report_sha256_digest','evidence_reference = @evidence_reference','JsonSerializer.Deserialize<StaticValidationEvidenceRecord>','SHA256.HashData','ValidationGate.Static','ValidationSeverity.Critical') | ForEach-Object { if ($staticReader -notmatch [regex]::Escape($_)) { throw "Security Static-reader guard missing: $_" } }
    $candidate = Get-Content -Raw $wave10Paths.Candidate
    @('ISecurityValidationCodeGenerationCandidateReader','JOIN software_factory.static_validation_evidence','static.evidence_reference = @static_evidence_reference','record.Purpose','record.Evaluation.IsAccepted','GovernedGeneratedPath.Validate','SHA256.HashData') | ForEach-Object { if ($candidate -notmatch [regex]::Escape($_)) { throw "Security candidate-reader guard missing: $_" } }
    $run = Get-Content -Raw $wave10Paths.Run
    @('ISecurityValidationDeliveryRunReader','purpose = @purpose','static_validation_id = @static_validation_id','static_report_sha256_digest = @static_report_sha256_digest','SHA256.HashData','DeliveryStage.StaticValidation') | ForEach-Object { if ($run -notmatch [regex]::Escape($_)) { throw "Security run-reader guard missing: $_" } }
    foreach ($readerText in @($staticReader,$candidate,$run)) { if ($readerText -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Security prerequisite reader contains mutation.' } }
    $controls = Get-Content -Raw $wave10Paths.Controls
    @('SecurityValidationRuntimeConfigurationState.Unconfigured','TrustedPublicKeysPem','SignedDeterministicSecurityValidationControl','ValidationGate.Security','AiPlanningSignatureVerifier.Verify','RequiredText','ForbiddenText','AllowedFileExtensions','ValidationSeverity.Error','evidence://security-validation/controls') | ForEach-Object { if ($controls -notmatch [regex]::Escape($_)) { throw "Signed Security control guard missing: $_" } }
    if ($controls -match 'HttpClient|Process|File\.Write|Directory\.Create|ExecuteNonQuery') { throw 'Security control exposes an unauthorized effect capability.' }
    $authorization = Get-Content -Raw $wave10Paths.Authorization
    @('IAccessPolicyEvaluator','request.Identity','request.RequiredRoles','developer.internal-service.security-validation.create','security-validation-report.read','evidence://security-validation/result-authorization') | ForEach-Object { if ($authorization -notmatch [regex]::Escape($_)) { throw "Security authorization guard missing: $_" } }
    $evidence = Get-Content -Raw $wave10Paths.Evidence
    @('ISecurityValidationEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','evidence://security-validation','CommitAsync') | ForEach-Object { if ($evidence -notmatch [regex]::Escape($_)) { throw "Security evidence guard missing: $_" } }
    if ($evidence -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Security evidence adapter contains mutation outside append.' }
    $inputs = Get-Content -Raw $wave10Paths.InputsMigration
    @('security_validation_delivery_run_snapshots','static_validation_id uuid NOT NULL','candidate_sha256_digest text NOT NULL','static_report_sha256_digest text NOT NULL','purpose text NOT NULL','COMMIT;') | ForEach-Object { if ($inputs -notmatch [regex]::Escape($_)) { throw "Security input migration guard missing: $_" } }
    $evidenceMigration = Get-Content -Raw $wave10Paths.EvidenceMigration
    @('software_factory.security_validation_evidence','PRIMARY KEY (tenant_id, validation_id)','FOREIGN KEY (tenant_id, static_validation_id)','FOREIGN KEY (tenant_id, delivery_run_id)','record_json jsonb') | ForEach-Object { if ($evidenceMigration -notmatch [regex]::Escape($_)) { throw "Security evidence migration guard missing: $_" } }
    $services = Get-Content -Raw $wave10Paths.Services
    @('SecurityValidationRuntimeReadiness','SecurityValidationRuntimeOptions','ISecurityValidationPolicyGate','IAuthorizedStaticValidationReceiptReader','ISecurityValidationCodeGenerationCandidateReader','ISecurityValidationDeliveryRunReader','SignedDeterministicSecurityValidationControl','ISecurityValidationResultAuthorizer','ISecurityValidationEvidenceRecorder','hasOverlappingValidationControls') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "Security composition missing: $_" } }
    $readiness = Get-Content -Raw $wave10Paths.Readiness
    @('Security Validation OPA policy gate','Authorized Static Validation receipt reader','Security Validation Code Generation candidate reader','Security Validation delivery-run reader','Security Validation result authorizer','Security Validation evidence recorder') | ForEach-Object { if ($readiness -notmatch [regex]::Escape($_)) { throw "Security readiness dependency missing: $_" } }
    $operations = Get-Content -Raw $wave10Paths.Operations
    @('SecurityValidationRuntimeReadiness','securityValidation') | ForEach-Object { if ($operations -notmatch [regex]::Escape($_)) { throw "Security readiness missing: $_" } }
    $endpoint = Get-Content -Raw $wave10Paths.Endpoint
    @('GetService<ISecurityValidationCodeGenerationCandidateReader>','Governed Security Validation is not operationally ready','cannot invoke Sandbox') | ForEach-Object { if ($endpoint -notmatch [regex]::Escape($_)) { throw "Security endpoint composition missing: $_" } }
    $openApi = Get-Content -Raw $wave10Paths.OpenApi
    @('securityValidation','unconfigured','invalid','configured','signed Security controls','cannot invoke Sandbox') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Security OpenAPI readiness missing: $_" } }
    $acceptance = Get-Content -Raw $wave10AcceptancePath
    @('Status: **Satisfied**','all 143 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Wave 10 acceptance missing: $_" } }
}

$wave11AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_11_ACCEPTANCE.md'
if (Test-Path $wave11AcceptancePath) {
    $wave11Paths = @{
        Change = Join-Path $repositoryRoot 'docs\change-control\CR-012-OPERATIONALIZATION-WAVE-11.md'
        Master = Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md'
        Envelope = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs'
        Engine = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedSandboxExecution.cs'
        Policy = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignSandboxPolicyGate.cs'
        Options = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SandboxRuntimeOptions.cs'
        Runtime = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\SovereignHttpSecuritySandboxRuntime.cs'
        SecurityReader = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlSandboxSecurityValidationReceiptReader.cs'
        Candidate = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlSandboxCodeGenerationCandidateReader.cs'
        Run = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlSandboxDeliveryRunReader.cs'
        Authorization = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeterministicSandboxResultAuthorizer.cs'
        Evidence = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlSandboxEvidenceRecorder.cs'
        InputsMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\015_sandbox_inputs.sql'
        EvidenceMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\016_sandbox_evidence.sql'
        Services = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
        Readiness = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
        Operations = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
        Endpoint = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
        OpenApi = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
        Guide = Join-Path $repositoryRoot 'docs\operationalization\WAVE_11_GOVERNED_SECURITY_SANDBOX.md'
    }
    $wave11Paths.Values | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 11 artifact missing: $_" } }
    $change = Get-Content -Raw $wave11Paths.Change
    @('Status: **Approved for Operationalization Wave 11**','Decision: **Approved by the repository owner','No provider, image','provider-neutral sovereign HTTPS sandbox protocol') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Wave 11 approval missing: $_" } }
    $master = Get-Content -Raw $wave11Paths.Master
    @('CR-012','CR-012-OPERATIONALIZATION-WAVE-11.md','signed response must attest Firecracker-class ephemeral microVM isolation') | ForEach-Object { if ($master -notmatch [regex]::Escape($_)) { throw "Master CR-012 authority missing: $_" } }
    $envelope = Get-Content -Raw $wave11Paths.Envelope
    @('SovereignSandboxPolicyScope','AllowedSandboxImage','AllowedEnvironmentReferences','AllowedNetworkDestinations','Sandbox') | ForEach-Object { if ($envelope -notmatch [regex]::Escape($_)) { throw "Sandbox policy envelope missing: $_" } }
    $policy = Get-Content -Raw $wave11Paths.Policy
    @('internal-service.sandbox.execute','policyBundleVerifier.VerifyAsync','policyClient.EvaluateAsync','envelope.Sandbox','AllowedSandboxImage','sandbox-result','mixed scopes from another action') | ForEach-Object { if ($policy -notmatch [regex]::Escape($_)) { throw "Sandbox OPA guard missing: $_" } }
    if ($policy.IndexOf('policyBundleVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $policy.IndexOf('policyClient.EvaluateAsync',[StringComparison]::Ordinal)) { throw 'Sandbox OPA evaluation does not follow bundle verification.' }
    $engine = Get-Content -Raw $wave11Paths.Engine
    if ($engine.IndexOf('policyGate.EvaluateAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('securityReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('securityReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('candidateReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('candidateReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('runReader.LoadAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('registryReader.FindExactAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('registryReader.FindExactAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('supplyChainVerifier.VerifyAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('supplyChainVerifier.VerifyAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('GovernedSandboxService(runtime).ExecuteAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('GovernedSandboxService(runtime).ExecuteAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -or
        $engine.IndexOf('resultAuthorizer.AuthorizeAsync',[StringComparison]::Ordinal) -ge $engine.IndexOf('evidenceRecorder.RecordAsync',[StringComparison]::Ordinal)) { throw 'Sandbox policy/read/image/runtime/authorization/evidence order is invalid.' }
    @('ISandboxSecurityValidationReceiptReader','ISandboxCodeGenerationCandidateReader','DeliveryStage.SecurityValidation','IInstitutionalPackageRegistryReader','IApprovedPackageSupplyChainVerifier','GovernedSandboxService','ISecuritySandboxRuntime','RequiredRoles','sandbox-result','ProductionEffectOccurred','CanAdvance','Separately approved Tests') | ForEach-Object { if ($engine -notmatch [regex]::Escape($_)) { throw "Sandbox engine guard missing: $_" } }
    if ($engine -match 'StageCompletion') { throw 'Operationalized Sandbox must not advance workflow.' }
    $securityReader = Get-Content -Raw $wave11Paths.SecurityReader
    @('ISandboxSecurityValidationReceiptReader','purpose','security_report_sha256_digest = @security_report_sha256_digest','evidence_reference = @evidence_reference','JsonSerializer.Deserialize<SecurityValidationEvidenceRecord>','SHA256.HashData','ValidationGate.Security','ValidationSeverity.Critical') | ForEach-Object { if ($securityReader -notmatch [regex]::Escape($_)) { throw "Sandbox Security-reader guard missing: $_" } }
    $candidate = Get-Content -Raw $wave11Paths.Candidate
    @('ISandboxCodeGenerationCandidateReader','JOIN software_factory.security_validation_evidence','security.evidence_reference = @security_evidence_reference','record.Purpose','record.Evaluation.IsAccepted','GovernedGeneratedPath.Validate','SHA256.HashData') | ForEach-Object { if ($candidate -notmatch [regex]::Escape($_)) { throw "Sandbox candidate-reader guard missing: $_" } }
    $run = Get-Content -Raw $wave11Paths.Run
    @('ISandboxDeliveryRunReader','purpose = @purpose','security_validation_id = @security_validation_id','security_report_sha256_digest = @security_report_sha256_digest','SHA256.HashData','DeliveryStage.SecurityValidation') | ForEach-Object { if ($run -notmatch [regex]::Escape($_)) { throw "Sandbox run-reader guard missing: $_" } }
    foreach ($readerText in @($securityReader,$candidate,$run)) { if ($readerText -match 'UPDATE software_factory|DELETE FROM software_factory|INSERT INTO software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Sandbox prerequisite reader contains mutation.' } }
    $options = Get-Content -Raw $wave11Paths.Options
    @('SandboxRuntimeConfigurationState.Unconfigured','Uri.UriSchemeHttps','TrustedPublicKeysPem','RequestTimeoutSeconds','MaximumRequestBytes','MaximumResponseBytes') | ForEach-Object { if ($options -notmatch [regex]::Escape($_)) { throw "Sandbox runtime configuration guard missing: $_" } }
    $runtime = Get-Content -Raw $wave11Paths.Runtime
    @('ISecuritySandboxRuntime','sovereign-security-sandbox','ResponseHeadersRead','ReadBoundedAsync','AiPlanningSignatureVerifier.Verify','request.IsolationPolicy.Validate','ProductionCredentialsMounted','HostFilesystemMounted','NetworkDefaultDenyEnforced','AllowedNetworkDestinations','ExitCode != 0','evidence://') | ForEach-Object { if ($runtime -notmatch [regex]::Escape($_)) { throw "Sovereign Sandbox runtime guard missing: $_" } }
    if ($runtime -match 'Process\.Start|File\.Write|Directory\.Create|PackageReference') { throw 'Sandbox adapter exposes an API-host execution or mutation capability.' }
    $authorization = Get-Content -Raw $wave11Paths.Authorization
    @('IAccessPolicyEvaluator','request.Identity','request.RequiredRoles','developer.internal-service.sandbox.execute','sandbox-result.read','evidence://sandbox/result-authorization') | ForEach-Object { if ($authorization -notmatch [regex]::Escape($_)) { throw "Sandbox authorization guard missing: $_" } }
    $evidence = Get-Content -Raw $wave11Paths.Evidence
    @('ISandboxEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','evidence://sandbox','CommitAsync') | ForEach-Object { if ($evidence -notmatch [regex]::Escape($_)) { throw "Sandbox evidence guard missing: $_" } }
    if ($evidence -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Sandbox evidence adapter contains mutation outside append.' }
    $inputs = Get-Content -Raw $wave11Paths.InputsMigration
    @('sandbox_delivery_run_snapshots','security_validation_id uuid NOT NULL','candidate_sha256_digest text NOT NULL','security_report_sha256_digest text NOT NULL','purpose text NOT NULL','COMMIT;') | ForEach-Object { if ($inputs -notmatch [regex]::Escape($_)) { throw "Sandbox input migration guard missing: $_" } }
    $evidenceMigration = Get-Content -Raw $wave11Paths.EvidenceMigration
    @('software_factory.sandbox_evidence','PRIMARY KEY (tenant_id, execution_id)','FOREIGN KEY (tenant_id, security_validation_id)','FOREIGN KEY (tenant_id, delivery_run_id)','record_json jsonb') | ForEach-Object { if ($evidenceMigration -notmatch [regex]::Escape($_)) { throw "Sandbox evidence migration guard missing: $_" } }
    $services = Get-Content -Raw $wave11Paths.Services
    @('SandboxRuntimeReadiness','SandboxRuntimeOptions','ISandboxPolicyGate','ISandboxSecurityValidationReceiptReader','ISandboxCodeGenerationCandidateReader','ISandboxDeliveryRunReader','ISecuritySandboxRuntime','SovereignHttpSecuritySandboxRuntime','ISandboxResultAuthorizer','ISandboxEvidenceRecorder') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "Sandbox composition missing: $_" } }
    $readiness = Get-Content -Raw $wave11Paths.Readiness
    @('Sandbox OPA policy gate','Sandbox-authorized Security Validation receipt reader','Sandbox Code Generation candidate reader','Sandbox delivery-run reader','Sandbox result authorizer','Sandbox evidence recorder') | ForEach-Object { if ($readiness -notmatch [regex]::Escape($_)) { throw "Sandbox readiness dependency missing: $_" } }
    $operations = Get-Content -Raw $wave11Paths.Operations
    @('SandboxRuntimeReadiness','sandbox') | ForEach-Object { if ($operations -notmatch [regex]::Escape($_)) { throw "Sandbox readiness missing: $_" } }
    $endpoint = Get-Content -Raw $wave11Paths.Endpoint
    @('GetService<ISandboxSecurityValidationReceiptReader>','GetService<ISandboxCodeGenerationCandidateReader>','Governed Sandbox is not operationally ready','No production effect, Tests approval, or workflow advancement') | ForEach-Object { if ($endpoint -notmatch [regex]::Escape($_)) { throw "Sandbox endpoint composition missing: $_" } }
    $openApi = Get-Content -Raw $wave11Paths.OpenApi
    @('sandbox','unconfigured','invalid','configured','signed provider-neutral sovereign Sandbox invocation','No production effect, Tests approval, or workflow advancement') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Sandbox OpenAPI readiness missing: $_" } }
    $acceptance = Get-Content -Raw $wave11AcceptancePath
    @('Status: **Satisfied**','CR-012','all 145 dependencies remain disconnected and fail closed','All seven Wave 11 runtime contracts','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Wave 11 acceptance artifact missing: $_" } }
}

$wave12AcceptancePath = Join-Path $repositoryRoot 'docs\operationalization\WAVE_12_ACCEPTANCE.md'
if (Test-Path $wave12AcceptancePath) {
    $wave12Paths = @{
        Change = Join-Path $repositoryRoot 'docs\change-control\CR-013-OPERATIONALIZATION-WAVE-12.md'
        Master = Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md'
        Envelope = Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs'
        Engine = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedTestsExecution.cs'
        Operationalization = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\TestsOperationalization.cs'
        Persistence = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlTestsOperationalization.cs'
        InputsMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\017_tests_inputs.sql'
        EvidenceMigration = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\018_tests_evidence.sql'
        Services = Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs'
        Readiness = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs'
        Operations = Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformOperationalEndpoints.cs'
        Endpoint = Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs'
        OpenApi = Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json'
        Guide = Join-Path $repositoryRoot 'docs\operationalization\WAVE_12_GOVERNED_TESTS.md'
    }
    $wave12Paths.Values | ForEach-Object { if (-not (Test-Path $_)) { throw "Operationalization Wave 12 artifact missing: $_" } }
    $change = Get-Content -Raw $wave12Paths.Change
    @('Status: **Approved for Operationalization Wave 12**','Decision: **Approved by the repository owner','provider-neutral sovereign HTTPS test runtime','Human Review') | ForEach-Object { if ($change -notmatch [regex]::Escape($_)) { throw "Wave 12 approval missing: $_" } }
    $master = Get-Content -Raw $wave12Paths.Master
    @('CR-013','CR-013-OPERATIONALIZATION-WAVE-12.md','signed response attests Firecracker-class isolation and exact manifest coverage') | ForEach-Object { if ($master -notmatch [regex]::Escape($_)) { throw "Master CR-013 authority missing: $_" } }
    $envelope = Get-Content -Raw $wave12Paths.Envelope
    @('SovereignTestsPolicyScope','AllowedTestImage','RequiredTestIds','AllowedTestCategories','Tests') | ForEach-Object { if ($envelope -notmatch [regex]::Escape($_)) { throw "Tests policy envelope missing: $_" } }
    $engine = Get-Content -Raw $wave12Paths.Engine
    @('ITestsSandboxExecutionReceiptReader','ITestsSecurityValidationReceiptReader','ITestsCodeGenerationCandidateReader','DeliveryStage.Sandbox','IGovernedTestManifestReader','IInstitutionalPackageRegistryReader','IApprovedPackageSupplyChainVerifier','IGovernedTestRuntime','RequiredRoles','tests-result','ITestsResultAuthorizer','ITestsEvidenceRecorder','ProductionEffectOccurred','CanAdvance','Separately approved Human Review') | ForEach-Object { if ($engine -notmatch [regex]::Escape($_)) { throw "Operationalized Tests engine guard missing: $_" } }
    if ($engine -match 'StageCompletion') { throw 'Operationalized Tests must not advance workflow.' }
    $operationalization = Get-Content -Raw $wave12Paths.Operationalization
    @('SovereignTestsPolicyGate','internal-service.tests.execute','bundleVerifier.VerifyAsync','envelope.Tests','TestsRuntimeConfigurationState.Unconfigured','Uri.UriSchemeHttps','SovereignHttpGovernedTestRuntime','ResponseHeadersRead','ReadBoundedAsync','AiPlanningSignatureVerifier.Verify','Firecracker-class','NetworkDefaultDenyEnforced','DeterministicTestsResultAuthorizer','tests-result.read') | ForEach-Object { if ($operationalization -notmatch [regex]::Escape($_)) { throw "Tests operationalization guard missing: $_" } }
    if ($operationalization -match 'Process\.Start|File\.Write|Directory\.Create|PackageReference') { throw 'Tests adapter exposes an API-host execution or mutation capability.' }
    $persistence = Get-Content -Raw $wave12Paths.Persistence
    @('ITestsSandboxExecutionReceiptReader','ITestsSecurityValidationReceiptReader','ITestsCodeGenerationCandidateReader','ITestsDeliveryRunReader','IGovernedTestManifestReader','ITestsEvidenceRecorder','BeginTransactionAsync','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','SHA256.HashData','evidence://tests','CommitAsync') | ForEach-Object { if ($persistence -notmatch [regex]::Escape($_)) { throw "Tests persistence guard missing: $_" } }
    if ($persistence -match 'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA') { throw 'Tests persistence contains mutation outside append.' }
    $inputs = Get-Content -Raw $wave12Paths.InputsMigration
    @('tests_delivery_run_snapshots','governed_test_manifests','sandbox_execution_id uuid NOT NULL','manifest_sha256_digest text NOT NULL','purpose text NOT NULL','COMMIT;') | ForEach-Object { if ($inputs -notmatch [regex]::Escape($_)) { throw "Tests input migration guard missing: $_" } }
    $evidenceMigration = Get-Content -Raw $wave12Paths.EvidenceMigration
    @('software_factory.tests_evidence','PRIMARY KEY (tenant_id, execution_id)','FOREIGN KEY (tenant_id, sandbox_execution_id)','FOREIGN KEY (tenant_id, delivery_run_id)','record_json jsonb') | ForEach-Object { if ($evidenceMigration -notmatch [regex]::Escape($_)) { throw "Tests evidence migration guard missing: $_" } }
    $services = Get-Content -Raw $wave12Paths.Services
    @('TestsRuntimeReadiness','TestsRuntimeOptions','ITestsPolicyGate','ITestsSandboxExecutionReceiptReader','ITestsSecurityValidationReceiptReader','ITestsCodeGenerationCandidateReader','ITestsDeliveryRunReader','IGovernedTestManifestReader','SovereignHttpGovernedTestRuntime','ITestsResultAuthorizer','ITestsEvidenceRecorder') | ForEach-Object { if ($services -notmatch [regex]::Escape($_)) { throw "Tests composition missing: $_" } }
    $readiness = Get-Content -Raw $wave12Paths.Readiness
    @('Tests OPA policy gate','Tests-authorized Sandbox receipt reader','Tests-authorized Security receipt reader','Tests Code Generation candidate reader','Tests delivery-run reader','Governed test-manifest reader','Governed test runtime','Tests result authorizer','Tests evidence recorder') | ForEach-Object { if ($readiness -notmatch [regex]::Escape($_)) { throw "Tests readiness dependency missing: $_" } }
    $operations = Get-Content -Raw $wave12Paths.Operations
    @('TestsRuntimeReadiness','tests') | ForEach-Object { if ($operations -notmatch [regex]::Escape($_)) { throw "Tests readiness missing: $_" } }
    $endpoint = Get-Content -Raw $wave12Paths.Endpoint
    @('GetService<ITestsSandboxExecutionReceiptReader>','GetService<ITestsSecurityValidationReceiptReader>','GetService<ITestsCodeGenerationCandidateReader>','Governed Tests are not operationally ready','No production effect, Human Review approval, or workflow advancement') | ForEach-Object { if ($endpoint -notmatch [regex]::Escape($_)) { throw "Tests endpoint composition missing: $_" } }
    $openApi = Get-Content -Raw $wave12Paths.OpenApi
    @('tests','unconfigured','invalid','configured','bounded signed sovereign test invocation','No production effect, Human Review approval, or workflow advancement') | ForEach-Object { if ($openApi -notmatch [regex]::Escape($_)) { throw "Tests OpenAPI readiness missing: $_" } }
    $acceptance = Get-Content -Raw $wave12AcceptancePath
    @('Status: **Satisfied**','CR-013','all 148 dependencies remain disconnected and fail closed','All nine Tests runtime contracts','All 15 projects build with zero warnings and zero errors') | ForEach-Object { if ($acceptance -notmatch [regex]::Escape($_)) { throw "Wave 12 acceptance artifact missing: $_" } }
}

$wave13AcceptancePath=Join-Path $repositoryRoot 'docs\operationalization\WAVE_13_ACCEPTANCE.md'
if(Test-Path $wave13AcceptancePath){
 $paths=@{Change=Join-Path $repositoryRoot 'docs\change-control\CR-014-OPERATIONALIZATION-WAVE-13.md';Master=Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md';Envelope=Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs';Engine=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedHumanReview.cs';Ops=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\HumanReviewOperationalization.cs';Persistence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlHumanReviewOperationalization.cs';Inputs=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\019_human_review_inputs.sql';Evidence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\020_human_review_evidence.sql';Services=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs';Readiness=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs';Endpoint=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs';OpenApi=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json';Guide=Join-Path $repositoryRoot 'docs\operationalization\WAVE_13_GOVERNED_HUMAN_REVIEW.md'}
 $paths.Values|ForEach-Object{if(-not(Test-Path $_)){throw "Wave 13 artifact missing: $_"}}
 $change=Get-Content -Raw $paths.Change;@('Status: **Approved for Operationalization Wave 13**','Decision: **Approved by the repository owner','no identity provider','Git and later stations')|ForEach-Object{if($change-notmatch[regex]::Escape($_)){throw "Wave 13 approval missing: $_"}}
 $master=Get-Content -Raw $paths.Master;@('CR-014','CR-014-OPERATIONALIZATION-WAVE-13.md','AI, policy, runtime, and API cannot supply or alter the human decision')|ForEach-Object{if($master-notmatch[regex]::Escape($_)){throw "Master CR-014 authority missing: $_"}}
 $envelope=Get-Content -Raw $paths.Envelope;@('SovereignHumanReviewPolicyScope','ReviewerSubjectId','RationaleSha256Digest','ReviewerIsHuman','ExpectedVersion','HumanReview')|ForEach-Object{if($envelope-notmatch[regex]::Escape($_)){throw "Human Review policy envelope missing: $_"}}
 $engine=Get-Content -Raw $paths.Engine;@('IHumanReviewTestsReceiptReader','IHumanReviewSandboxReceiptReader','IHumanReviewSecurityReceiptReader','IHumanReviewCandidateReader','DeliveryStage.Tests','policyGate.EvaluateAsync','attestationVerifier.VerifyAsync','RecordDecisionAndEvidenceAsync','ExpectedVersion','ProductionEffectOccurred','CanAdvance','Separately approved Git boundary')|ForEach-Object{if($engine-notmatch[regex]::Escape($_)){throw "Human Review engine guard missing: $_"}};if($engine-match'StageCompletion'){throw 'Human Review must not advance workflow.'}
 $ops=Get-Content -Raw $paths.Ops;@('HumanReviewRuntimeConfigurationState.Unconfigured','Uri.UriSchemeHttps','SovereignHumanReviewPolicyGate','internal-service.human-review.decide','e.HumanReview','Denied Human Review decision returned scope','SovereignHttpHumanReviewAttestationVerifier','ResponseHeadersRead','ReadBoundedAsync','AiPlanningSignatureVerifier.Verify')|ForEach-Object{if($ops-notmatch[regex]::Escape($_)){throw "Human Review operationalization missing: $_"}};if($ops-match'Process\.Start|File\.Write|Directory\.Create'){throw 'Human Review adapter exposes unauthorized effects.'}
 $p=Get-Content -Raw $paths.Persistence;@('IHumanReviewTestsReceiptReader','IHumanReviewSandboxReceiptReader','IHumanReviewSecurityReceiptReader','IHumanReviewCandidateReader','IHumanReviewDeliveryRunReader','IAtomicHumanReviewRepository','BeginTransactionAsync','IsolationLevel.Serializable','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','evidence://human-review','CommitAsync')|ForEach-Object{if($p-notmatch[regex]::Escape($_)){throw "Human Review persistence missing: $_"}};if($p-match'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA'){throw 'Human Review persistence mutates outside append.'}
 $inputs=Get-Content -Raw $paths.Inputs;@('human_review_delivery_run_snapshots','tests_execution_id uuid NOT NULL','purpose text NOT NULL','PRIMARY KEY (tenant_id, run_id)')|ForEach-Object{if($inputs-notmatch[regex]::Escape($_)){throw "Human Review input migration missing: $_"}}
 $evidence=Get-Content -Raw $paths.Evidence;@('human_review_evidence','PRIMARY KEY (tenant_id, review_id)','decision IN','version bigint NOT NULL','record_json jsonb NOT NULL')|ForEach-Object{if($evidence-notmatch[regex]::Escape($_)){throw "Human Review evidence migration missing: $_"}}
 $services=Get-Content -Raw $paths.Services;@('HumanReviewRuntimeReadiness','SovereignHumanReviewPolicyGate','PostgreSqlHumanReviewTestsReader','PostgreSqlHumanReviewSandboxReader','PostgreSqlHumanReviewSecurityReader','PostgreSqlHumanReviewCandidateReader','PostgreSqlHumanReviewRunReader','SovereignHttpHumanReviewAttestationVerifier','PostgreSqlAtomicHumanReviewRepository')|ForEach-Object{if($services-notmatch[regex]::Escape($_)){throw "Human Review composition missing: $_"}}
 $ready=Get-Content -Raw $paths.Readiness;@('Human Review-authorized Tests receipt reader','Human Review-authorized Sandbox receipt reader','Human Review-authorized Security receipt reader','Human Review-authorized Code Generation candidate reader','Human Review attestation verifier','Atomic Human Review and evidence repository')|ForEach-Object{if($ready-notmatch[regex]::Escape($_)){throw "Human Review readiness missing: $_"}}
 $endpoint=Get-Content -Raw $paths.Endpoint;@('GetService<IHumanReviewTestsReceiptReader>','GetService<IHumanReviewSandboxReceiptReader>','GetService<IHumanReviewSecurityReceiptReader>','GetService<IHumanReviewCandidateReader>','Governed Human Review is not operationally ready','cannot advance to Git or production')|ForEach-Object{if($endpoint-notmatch[regex]::Escape($_)){throw "Human Review endpoint missing: $_"}}
 $openapi=Get-Content -Raw $paths.OpenApi;@('humanReview','unconfigured','invalid','configured','human-review','cannot advance to Git or production')|ForEach-Object{if($openapi-notmatch[regex]::Escape($_)){throw "Human Review OpenAPI missing: $_"}}
 $acceptance=Get-Content -Raw $wave13AcceptancePath;@('Status: **Satisfied**','CR-014','all 151 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors')|ForEach-Object{if($acceptance-notmatch[regex]::Escape($_)){throw "Wave 13 acceptance missing: $_"}}
}

$wave14AcceptancePath=Join-Path $repositoryRoot 'docs\operationalization\WAVE_14_ACCEPTANCE.md'
if(Test-Path $wave14AcceptancePath){
 $paths=@{Change=Join-Path $repositoryRoot 'docs\change-control\CR-015-OPERATIONALIZATION-WAVE-14.md';Master=Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md';Envelope=Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs';Engine=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedGitSourceCommit.cs';Ops=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GitOperationalization.cs';Persistence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlGitOperationalization.cs';Inputs=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\021_git_inputs.sql';Evidence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\022_git_evidence.sql';Services=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs';Readiness=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs';Endpoint=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs';OpenApi=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json';Guide=Join-Path $repositoryRoot 'docs\operationalization\WAVE_14_GOVERNED_GIT.md'};$paths.Values|ForEach-Object{if(-not(Test-Path $_)){throw "Wave 14 artifact missing: $_"}}
 $change=Get-Content -Raw $paths.Change;@('Status: **Approved for Operationalization Wave 14**','Decision: **Approved by the repository owner','No provider','CI/CD and later stations remain disconnected')|ForEach-Object{if($change-notmatch[regex]::Escape($_)){throw "Wave 14 approval missing: $_"}}
 $master=Get-Content -Raw $paths.Master;@('CR-015','CR-015-OPERATIONALIZATION-WAVE-14.md','one signed commit from the exact clean base on a non-protected change branch')|ForEach-Object{if($master-notmatch[regex]::Escape($_)){throw "Master CR-015 authority missing: $_"}}
 $envelope=Get-Content -Raw $paths.Envelope;@('SovereignGitPolicyScope','AllowedRelativePaths','ExpectedBaseCommitId','ProtectedBranch','ForceUpdateAllowed','CiCdTriggerAllowed','Git')|ForEach-Object{if($envelope-notmatch[regex]::Escape($_)){throw "Git policy envelope missing: $_"}}
 $engine=Get-Content -Raw $paths.Engine;@('IGitHumanReviewReceiptReader','IGitTestsReceiptReader','IGitCandidateReader','DeliveryStage.HumanReview','policyGate.EvaluateAsync','materializer.MaterializeAsync','changeValidator.ValidateAsync','gitGateway.CommitAsync','resultAuthorizer.AuthorizeAsync','evidenceRecorder.RecordAsync','ProtectedBranchMutated','ForceUpdateOccurred','HistoryRewritten','PullRequestCreated','CiCdTriggered','ProductionEffectOccurred','CanAdvance','Separately approved CI/CD')|ForEach-Object{if($engine-notmatch[regex]::Escape($_)){throw "Operationalized Git engine missing: $_"}};if($engine-match'StageCompletion'){throw 'Git must not advance workflow.'}
 $ops=Get-Content -Raw $paths.Ops;@('GitRuntimeConfigurationState.Unconfigured','Uri.UriSchemeHttps','SovereignGitPolicyGate','internal-service.git.commit','e.Git','Denied Git decision returned scope','SovereignHttpInstitutionalGitGateway','X-Governed-Operation','ResponseHeadersRead','ReadBoundedAsync','AiPlanningSignatureVerifier.Verify','DeterministicGitResultAuthorizer','git-commit-result.read')|ForEach-Object{if($ops-notmatch[regex]::Escape($_)){throw "Git operationalization missing: $_"}};if($ops-match'Process\.Start|File\.Write|Directory\.Create'){throw 'Git adapter exposes local mutation.'}
 $p=Get-Content -Raw $paths.Persistence;@('IGitHumanReviewReceiptReader','IGitTestsReceiptReader','IGitCandidateReader','IGitDeliveryRunReader','IGitEvidenceRecorder','BeginTransactionAsync','IsolationLevel.Serializable','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','evidence://git','CommitAsync')|ForEach-Object{if($p-notmatch[regex]::Escape($_)){throw "Git persistence missing: $_"}};if($p-match'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA'){throw 'Git persistence mutates outside append.'}
 $inputs=Get-Content -Raw $paths.Inputs;@('git_delivery_run_snapshots','review_id uuid NOT NULL','purpose text NOT NULL','review_package_sha256_digest text NOT NULL')|ForEach-Object{if($inputs-notmatch[regex]::Escape($_)){throw "Git input migration missing: $_"}};$evidence=Get-Content -Raw $paths.Evidence;@('git_evidence','commit_id text NOT NULL','PRIMARY KEY(tenant_id,operation_id)','record_json jsonb NOT NULL')|ForEach-Object{if($evidence-notmatch[regex]::Escape($_)){throw "Git evidence migration missing: $_"}}
 $services=Get-Content -Raw $paths.Services;@('GitRuntimeReadiness','SovereignGitPolicyGate','PostgreSqlGitHumanReviewReader','PostgreSqlGitTestsReader','PostgreSqlGitCandidateReader','PostgreSqlGitRunReader','SovereignHttpInstitutionalGitGateway','DeterministicGitResultAuthorizer','PostgreSqlGitEvidenceRecorder')|ForEach-Object{if($services-notmatch[regex]::Escape($_)){throw "Git composition missing: $_"}}
 $ready=Get-Content -Raw $paths.Readiness;@('Git-authorized Human Review receipt reader','Git-authorized Tests receipt reader','Git-authorized Code Generation candidate reader','Institutional Git gateway','Git result authorizer','Git evidence recorder')|ForEach-Object{if($ready-notmatch[regex]::Escape($_)){throw "Git readiness missing: $_"}}
 $endpoint=Get-Content -Raw $paths.Endpoint;@('GetService<IGitHumanReviewReceiptReader>','GetService<IGitTestsReceiptReader>','GetService<IGitCandidateReader>','Governed Git is not operationally ready','No force update, PR, CI/CD, workflow advancement, or production effect')|ForEach-Object{if($endpoint-notmatch[regex]::Escape($_)){throw "Git endpoint missing: $_"}}
 $openapi=Get-Content -Raw $paths.OpenApi;@('git','unconfigured','invalid','configured','non-protected branch','No force update, PR, CI/CD, workflow advancement, or production effect')|ForEach-Object{if($openapi-notmatch[regex]::Escape($_)){throw "Git OpenAPI missing: $_"}}
 $acceptance=Get-Content -Raw $wave14AcceptancePath;@('Status: **Satisfied**','CR-015','all 153 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors')|ForEach-Object{if($acceptance-notmatch[regex]::Escape($_)){throw "Wave 14 acceptance missing: $_"}}
}

$wave15AcceptancePath=Join-Path $repositoryRoot 'docs\operationalization\WAVE_15_ACCEPTANCE.md'
if(Test-Path $wave15AcceptancePath){
 $paths=@{Change=Join-Path $repositoryRoot 'docs\change-control\CR-016-OPERATIONALIZATION-WAVE-15.md';Master=Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md';Envelope=Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs';Engine=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedCiCdExecution.cs';Ops=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\CiCdOperationalization.cs';Persistence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlCiCdOperationalization.cs';Inputs=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\023_cicd_inputs.sql';Evidence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\024_cicd_evidence.sql';Services=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs';Readiness=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs';OpenApi=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json';Guide=Join-Path $repositoryRoot 'docs\operationalization\WAVE_15_GOVERNED_CICD.md'};$paths.Values|ForEach-Object{if(-not(Test-Path $_)){throw "Wave 15 artifact missing: $_"}}
 $change=Get-Content -Raw $paths.Change;@('Status: **Approved for Operationalization Wave 15**','Decision: **Approved by the repository owner','No provider','Artifact publication')|ForEach-Object{if($change-notmatch[regex]::Escape($_)){throw "Wave 15 approval missing: $_"}}
 $master=Get-Content -Raw $paths.Master;@('CR-016','CR-016-OPERATIONALIZATION-WAVE-15.md','signed non-released pipeline-output manifest')|ForEach-Object{if($master-notmatch[regex]::Escape($_)){throw "Master CR-016 authority missing: $_"}}
 $envelope=Get-Content -Raw $paths.Envelope;@('SovereignCiCdPolicyScope','AllowedStageIds','AllowedControlIds','SourceMutationAllowed','ArtifactPublicationAllowed','DeploymentAllowed','CiCd')|ForEach-Object{if($envelope-notmatch[regex]::Escape($_)){throw "CI/CD policy envelope missing: $_"}}
 $engine=Get-Content -Raw $paths.Engine;@('policyGate.EvaluateAsync','gitReader.LoadAsync','runReader.LoadAsync','workflowReader.LoadAsync','workflowValidator.ValidateAsync','gateway.ExecuteAsync','resultAuthorizer.AuthorizeAsync','evidenceRecorder.RecordAsync','DeliveryStage.Git','IsReleasedArtifact','ArtifactPublished','DeploymentOccurred','ProductionEffectOccurred','CanAdvance','Separately approved Artifact')|ForEach-Object{if($engine-notmatch[regex]::Escape($_)){throw "Operationalized CI/CD engine missing: $_"}};if($engine-match'StageCompletion'){throw 'CI/CD must not advance workflow.'}
 $ops=Get-Content -Raw $paths.Ops;@('CiCdRuntimeConfigurationState.Unconfigured','Uri.UriSchemeHttps','SovereignCiCdPolicyGate','internal-service.cicd.execute','e.CiCd','Denied CI/CD decision returned scope','SovereignHttpCiCdGateway','X-Governed-Operation','ResponseHeadersRead','ReadBoundedAsync','AiPlanningSignatureVerifier.Verify','DeterministicCiCdResultAuthorizer')|ForEach-Object{if($ops-notmatch[regex]::Escape($_)){throw "CI/CD operationalization missing: $_"}};if($ops-match'Process\.Start|File\.Write|Directory\.Create'){throw 'CI/CD adapter exposes local execution or mutation.'}
 $p=Get-Content -Raw $paths.Persistence;@('IAuthorizedGitSourceCommitReceiptReader','ICiCdDeliveryRunReader','IGovernedCiCdWorkflowDefinitionReader','ICiCdEvidenceRecorder','BeginTransactionAsync','IsolationLevel.Serializable','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','evidence://cicd','CommitAsync')|ForEach-Object{if($p-notmatch[regex]::Escape($_)){throw "CI/CD persistence missing: $_"}};if($p-match'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA'){throw 'CI/CD persistence mutates outside append.'}
 $inputs=Get-Content -Raw $paths.Inputs;@('cicd_delivery_run_snapshots','cicd_workflow_definitions','git_operation_id uuid NOT NULL','workflow_sha256_digest text NOT NULL')|ForEach-Object{if($inputs-notmatch[regex]::Escape($_)){throw "CI/CD input migration missing: $_"}};$evidence=Get-Content -Raw $paths.Evidence;@('cicd_evidence','manifest_sha256_digest text NOT NULL','PRIMARY KEY(tenant_id,execution_id)','record_json jsonb NOT NULL')|ForEach-Object{if($evidence-notmatch[regex]::Escape($_)){throw "CI/CD evidence migration missing: $_"}}
 $services=Get-Content -Raw $paths.Services;@('CiCdRuntimeReadiness','SovereignCiCdPolicyGate','PostgreSqlAuthorizedGitReceiptReader','PostgreSqlCiCdRunReader','PostgreSqlCiCdWorkflowReader','SovereignHttpCiCdGateway','DeterministicCiCdResultAuthorizer','PostgreSqlCiCdEvidenceRecorder')|ForEach-Object{if($services-notmatch[regex]::Escape($_)){throw "CI/CD composition missing: $_"}}
 $ready=Get-Content -Raw $paths.Readiness;@('CI/CD OPA policy gate','Authorized Git source-commit receipt reader','CI/CD delivery-run reader','Governed CI/CD workflow-definition reader','Institutional CI/CD gateway','CI/CD result authorizer','CI/CD evidence recorder')|ForEach-Object{if($ready-notmatch[regex]::Escape($_)){throw "CI/CD readiness missing: $_"}}
 $openapi=Get-Content -Raw $paths.OpenApi;@('ciCd','unconfigured','invalid','configured','pipeline','no source mutation, artifact publication, deployment, production effect, or workflow advancement')|ForEach-Object{if($openapi-notmatch[regex]::Escape($_)){throw "CI/CD OpenAPI missing: $_"}}
 $acceptance=Get-Content -Raw $wave15AcceptancePath;@('Status: **Satisfied**','CR-016','all 153 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors')|ForEach-Object{if($acceptance-notmatch[regex]::Escape($_)){throw "Wave 15 acceptance missing: $_"}}
}

$wave16AcceptancePath=Join-Path $repositoryRoot 'docs\operationalization\WAVE_16_ACCEPTANCE.md'
if(Test-Path $wave16AcceptancePath){
 $paths=@{Change=Join-Path $repositoryRoot 'docs\change-control\CR-017-OPERATIONALIZATION-WAVE-16.md';Master=Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md';Envelope=Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs';Engine=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedArtifactPublication.cs';Ops=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\ArtifactOperationalization.cs';Persistence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlArtifactOperationalization.cs';Inputs=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\025_artifact_inputs.sql';Evidence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\026_artifact_evidence.sql';Services=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs';Readiness=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs';Endpoint=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs';OpenApi=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json';Guide=Join-Path $repositoryRoot 'docs\operationalization\WAVE_16_GOVERNED_ARTIFACT.md'};$paths.Values|ForEach-Object{if(-not(Test-Path $_)){throw "Wave 16 artifact missing: $_"}}
 $change=Get-Content -Raw $paths.Change;@('Status: **Approved for Operationalization Wave 16**','Decision: **Approved by the repository owner','No provider','Deployment')|ForEach-Object{if($change-notmatch[regex]::Escape($_)){throw "Wave 16 approval missing: $_"}}
 $master=Get-Content -Raw $paths.Master;@('CR-017','CR-017-OPERATIONALIZATION-WAVE-16.md','one exact immutable coordinate without overwrite')|ForEach-Object{if($master-notmatch[regex]::Escape($_)){throw "Master CR-017 authority missing: $_"}}
 $envelope=Get-Content -Raw $paths.Envelope;@('SovereignArtifactPolicyScope','PipelineManifestSha256Digest','ContentSha256Digest','RegistryRepository','AllowedControls','OverwriteAllowed','DeploymentAllowed','Artifact')|ForEach-Object{if($envelope-notmatch[regex]::Escape($_)){throw "Artifact policy envelope missing: $_"}}
 $engine=Get-Content -Raw $paths.Engine;@('policyGate.EvaluateAsync','ciCdReader.LoadAsync','manifestReader.LoadAsync','runReader.LoadAsync','packageValidator.ValidateAsync','registryGateway.PublishAsync','supplyChainPipeline.VerifyAsync','resultAuthorizer.AuthorizeAsync','evidenceRecorder.RecordAsync','DeliveryStage.CiCd','ExistingCoordinateOverwritten','SupplyChainControl','DeploymentOccurred','ProductionEffectOccurred','CanAdvance','Separately approved Deployment')|ForEach-Object{if($engine-notmatch[regex]::Escape($_)){throw "Operationalized Artifact engine missing: $_"}};if($engine-match'StageCompletion'){throw 'Artifact must not advance workflow.'}
 $ops=Get-Content -Raw $paths.Ops;@('ArtifactRuntimeConfigurationState.Unconfigured','Uri.UriSchemeHttps','SovereignArtifactPolicyGate','internal-service.artifact.publish','envelope.Artifact','Denied Artifact decision returned scope','SovereignHttpArtifactGateway','X-Governed-Operation','ResponseHeadersRead','ReadBoundedAsync','AiPlanningSignatureVerifier.Verify','InstitutionalArtifactSupplyChainVerifier','DeterministicArtifactResultAuthorizer')|ForEach-Object{if($ops-notmatch[regex]::Escape($_)){throw "Artifact operationalization missing: $_"}};if($ops-match'Process\.Start|File\.Write|Directory\.Create'){throw 'Artifact adapter exposes local mutation.'}
 $p=Get-Content -Raw $paths.Persistence;@('IAuthorizedCiCdExecutionReceiptReader','IAuthorizedPipelineOutputManifestReader','IArtifactDeliveryRunReader','IArtifactEvidenceRecorder','BeginTransactionAsync','IsolationLevel.Serializable','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','evidence://artifact','CommitAsync')|ForEach-Object{if($p-notmatch[regex]::Escape($_)){throw "Artifact persistence missing: $_"}};if($p-match'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA'){throw 'Artifact persistence mutates outside append.'}
 $inputs=Get-Content -Raw $paths.Inputs;@('artifact_delivery_run_snapshots','cicd_execution_id uuid NOT NULL','purpose text NOT NULL','PRIMARY KEY(tenant_id,run_id)')|ForEach-Object{if($inputs-notmatch[regex]::Escape($_)){throw "Artifact input migration missing: $_"}};$evidence=Get-Content -Raw $paths.Evidence;@('artifact_evidence','content_sha256_digest text NOT NULL','immutable_registry_reference text NOT NULL','PRIMARY KEY(tenant_id,publication_id)','record_json jsonb NOT NULL')|ForEach-Object{if($evidence-notmatch[regex]::Escape($_)){throw "Artifact evidence migration missing: $_"}}
 $services=Get-Content -Raw $paths.Services;@('ArtifactRuntimeReadiness','SovereignArtifactPolicyGate','PostgreSqlAuthorizedCiCdReceiptReader','PostgreSqlAuthorizedPipelineOutputManifestReader','PostgreSqlArtifactRunReader','SovereignHttpArtifactGateway','InstitutionalArtifactSupplyChainVerifier','DeterministicArtifactResultAuthorizer','PostgreSqlArtifactEvidenceRecorder')|ForEach-Object{if($services-notmatch[regex]::Escape($_)){throw "Artifact composition missing: $_"}}
 $ready=Get-Content -Raw $paths.Readiness;@('Artifact OPA policy gate','Authorized CI/CD execution receipt reader','Authorized pipeline-output manifest reader','Artifact delivery-run reader','Institutional Artifact package validator','Institutional Artifact registry gateway','Artifact supply-chain control verifiers','Artifact result authorizer','Artifact evidence recorder')|ForEach-Object{if($ready-notmatch[regex]::Escape($_)){throw "Artifact readiness missing: $_"}}
 $endpoint=Get-Content -Raw $paths.Endpoint;@('GetService<IArtifactPolicyGate>','GetService<IAuthorizedCiCdExecutionReceiptReader>','GetService<IAuthorizedPipelineOutputManifestReader>','Governed Artifact publication is not operationally ready','ArtifactDependencyUnavailableException','No deployment, production effect, or workflow advancement')|ForEach-Object{if($endpoint-notmatch[regex]::Escape($_)){throw "Artifact endpoint missing: $_"}}
 $openapi=Get-Content -Raw $paths.OpenApi;@('artifact','unconfigured','invalid','configured','immutable Artifact','No deployment, production effect, or workflow advancement')|ForEach-Object{if($openapi-notmatch[regex]::Escape($_)){throw "Artifact OpenAPI missing: $_"}}
 $acceptance=Get-Content -Raw $wave16AcceptancePath;@('Status: **Satisfied**','CR-017','all 153 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors')|ForEach-Object{if($acceptance-notmatch[regex]::Escape($_)){throw "Wave 16 acceptance missing: $_"}}
}

$wave17AcceptancePath=Join-Path $repositoryRoot 'docs\operationalization\WAVE_17_ACCEPTANCE.md'
if(Test-Path $wave17AcceptancePath){
 $paths=@{Change=Join-Path $repositoryRoot 'docs\change-control\CR-018-OPERATIONALIZATION-WAVE-17.md';Master=Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md';Envelope=Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs';Engine=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedSovereignDeployment.cs';Ops=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\DeploymentOperationalization.cs';Persistence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlDeploymentOperationalization.cs';Inputs=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\027_deployment_inputs.sql';Evidence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\028_deployment_evidence.sql';Services=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs';Readiness=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs';Endpoint=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs';OpenApi=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json';Guide=Join-Path $repositoryRoot 'docs\operationalization\WAVE_17_GOVERNED_DEPLOYMENT.md'};$paths.Values|ForEach-Object{if(-not(Test-Path $_)){throw "Wave 17 artifact missing: $_"}}
 $change=Get-Content -Raw $paths.Change;@('Status: **Approved for Operationalization Wave 17**','Decision: **Approved by the repository owner','No provider','OpenTelemetry')|ForEach-Object{if($change-notmatch[regex]::Escape($_)){throw "Wave 17 approval missing: $_"}}
 $master=Get-Content -Raw $paths.Master;@('CR-018','CR-018-OPERATIONALIZATION-WAVE-17.md','one idempotent deployment with runtime, activation, rollback')|ForEach-Object{if($master-notmatch[regex]::Escape($_)){throw "Master CR-018 authority missing: $_"}}
 $envelope=Get-Content -Raw $paths.Envelope;@('SovereignDeploymentPolicyScope','ArtifactContentSha256Digest','DeploymentProfileId','TargetEnvironment','ProductionDeploymentAllowed','HumanApprovalValid','AiAuthorityDetected','Deployment')|ForEach-Object{if($envelope-notmatch[regex]::Escape($_)){throw "Deployment policy envelope missing: $_"}}
 $engine=Get-Content -Raw $paths.Engine;@('policyGate.EvaluateAsync','artifactReceiptReader.LoadAsync','artifactReader.LoadAsync','runReader.LoadAsync','profileReader.LoadAsync','preflightValidator.ValidateAsync','gateway.DeployAsync','resultAuthorizer.AuthorizeAsync','evidenceRecorder.RecordAsync','DeliveryStage.Artifact','GovernedDeploymentTopology.AirGapped','ExternalControlPlaneAllowed','OutboundNetworkDefaultDeny','TelemetryConfigured','AutomaticRegistrationOccurred','EnterpriseModelMutated','CanAdvance','Separately approved OpenTelemetry')|ForEach-Object{if($engine-notmatch[regex]::Escape($_)){throw "Operationalized Deployment engine missing: $_"}};if($engine-match'StageCompletion'){throw 'Deployment must not advance workflow.'}
 $ops=Get-Content -Raw $paths.Ops;@('DeploymentRuntimeConfigurationState.Unconfigured','Uri.UriSchemeHttps','SovereignDeploymentPolicyGate','internal-service.deployment.execute','envelope.Deployment','Denied Deployment decision returned scope','SovereignHttpDeploymentGateway','X-Governed-Operation','ResponseHeadersRead','ReadBoundedAsync','AiPlanningSignatureVerifier.Verify','DeterministicDeploymentResultAuthorizer')|ForEach-Object{if($ops-notmatch[regex]::Escape($_)){throw "Deployment operationalization missing: $_"}};if($ops-match'Process\.Start|File\.Write|Directory\.Create'){throw 'Deployment adapter exposes local mutation.'}
 $p=Get-Content -Raw $paths.Persistence;@('IAuthorizedArtifactPublicationReceiptReader','IAuthorizedDeploymentArtifactReader','IDeploymentDeliveryRunReader','IGovernedSovereignDeploymentProfileReader','IDeploymentEvidenceRecorder','BeginTransactionAsync','IsolationLevel.Serializable','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','evidence://deployment','CommitAsync')|ForEach-Object{if($p-notmatch[regex]::Escape($_)){throw "Deployment persistence missing: $_"}};if($p-match'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA'){throw 'Deployment persistence mutates outside append.'}
 $inputs=Get-Content -Raw $paths.Inputs;@('deployment_delivery_run_snapshots','sovereign_deployment_profiles','artifact_publication_id uuid NOT NULL','profile_sha256_digest text NOT NULL','PRIMARY KEY(tenant_id,profile_id,version)')|ForEach-Object{if($inputs-notmatch[regex]::Escape($_)){throw "Deployment input migration missing: $_"}};$evidence=Get-Content -Raw $paths.Evidence;@('deployment_evidence','artifact_content_sha256_digest text NOT NULL','runtime_identity text NOT NULL','PRIMARY KEY(tenant_id,deployment_id)','record_json jsonb NOT NULL')|ForEach-Object{if($evidence-notmatch[regex]::Escape($_)){throw "Deployment evidence migration missing: $_"}}
 $services=Get-Content -Raw $paths.Services;@('DeploymentRuntimeReadiness','SovereignDeploymentPolicyGate','PostgreSqlAuthorizedArtifactReceiptReader','PostgreSqlAuthorizedDeploymentArtifactReader','PostgreSqlDeploymentRunReader','PostgreSqlSovereignDeploymentProfileReader','SovereignHttpDeploymentGateway','DeterministicDeploymentResultAuthorizer','PostgreSqlDeploymentEvidenceRecorder')|ForEach-Object{if($services-notmatch[regex]::Escape($_)){throw "Deployment composition missing: $_"}}
 $ready=Get-Content -Raw $paths.Readiness;@('Deployment OPA policy gate','Authorized Artifact publication receipt reader','Authorized verified Deployment Artifact reader','Deployment delivery-run reader','Governed sovereign Deployment profile reader','Institutional Deployment preflight validator','Institutional sovereign Deployment gateway','Deployment result authorizer','Deployment evidence recorder')|ForEach-Object{if($ready-notmatch[regex]::Escape($_)){throw "Deployment readiness missing: $_"}}
 $endpoint=Get-Content -Raw $paths.Endpoint;@('GetService<IDeploymentPolicyGate>','GetService<IAuthorizedArtifactPublicationReceiptReader>','GetService<IAuthorizedDeploymentArtifactReader>','Governed Deployment is not operationally ready','DeploymentDependencyUnavailableException','No telemetry, registration, Enterprise Model mutation, or workflow advancement')|ForEach-Object{if($endpoint-notmatch[regex]::Escape($_)){throw "Deployment endpoint missing: $_"}}
 $openapi=Get-Content -Raw $paths.OpenApi;@('deployment','unconfigured','invalid','configured','sovereign environment','No telemetry, registration, Enterprise Model mutation, or workflow advancement')|ForEach-Object{if($openapi-notmatch[regex]::Escape($_)){throw "Deployment OpenAPI missing: $_"}}
 $acceptance=Get-Content -Raw $wave17AcceptancePath;@('Status: **Satisfied**','CR-018','all 153 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors')|ForEach-Object{if($acceptance-notmatch[regex]::Escape($_)){throw "Wave 17 acceptance missing: $_"}}
}

$wave18AcceptancePath=Join-Path $repositoryRoot 'docs\operationalization\WAVE_18_ACCEPTANCE.md'
if(Test-Path $wave18AcceptancePath){
 $paths=@{Change=Join-Path $repositoryRoot 'docs\change-control\CR-019-OPERATIONALIZATION-WAVE-18.md';Master=Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md';Envelope=Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs';Engine=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedOpenTelemetryActivation.cs';Ops=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\OpenTelemetryOperationalization.cs';Persistence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlOpenTelemetryOperationalization.cs';Inputs=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\029_opentelemetry_inputs.sql';Evidence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\030_opentelemetry_evidence.sql';Services=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs';Readiness=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs';Endpoint=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs';OpenApi=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json';Guide=Join-Path $repositoryRoot 'docs\operationalization\WAVE_18_GOVERNED_OPENTELEMETRY.md'};$paths.Values|ForEach-Object{if(-not(Test-Path $_)){throw "Wave 18 artifact missing: $_"}}
 $change=Get-Content -Raw $paths.Change;@('Status: **Approved for Operationalization Wave 18**','Decision: **Approved by the repository owner','No provider','Automatic Registration')|ForEach-Object{if($change-notmatch[regex]::Escape($_)){throw "Wave 18 approval missing: $_"}}
 $master=Get-Content -Raw $paths.Master;@('CR-019','CR-019-OPERATIONALIZATION-WAVE-18.md','required traces, metrics, and logs')|ForEach-Object{if($master-notmatch[regex]::Escape($_)){throw "Master CR-019 authority missing: $_"}}
 $envelope=Get-Content -Raw $paths.Envelope;@('SovereignOpenTelemetryPolicyScope','RuntimeIdentity','TelemetryProfileId','AllowedSignals','RedactionPolicySha256Digest','RegistrationAllowed','EnterpriseModelMutationAllowed','OpenTelemetry')|ForEach-Object{if($envelope-notmatch[regex]::Escape($_)){throw "OpenTelemetry policy envelope missing: $_"}}
 $engine=Get-Content -Raw $paths.Engine;@('policyGate.EvaluateAsync','deploymentReader.LoadAsync','runReader.LoadAsync','profileReader.LoadAsync','redactionVerifier.VerifyAsync','gateway.ActivateAsync','resultAuthorizer.AuthorizeAsync','evidenceRecorder.RecordAsync','DeliveryStage.Deployment','SensitiveNamesDropped','UnknownAttributesRedacted','BaggageClearedAtStart','BaggageClearedAtEnd','Enum.GetValues<GovernedTelemetrySignal>()','AutomaticRegistrationOccurred','EnterpriseModelMutated','CanAdvance','Separately approved Automatic Registration')|ForEach-Object{if($engine-notmatch[regex]::Escape($_)){throw "Operationalized OpenTelemetry engine missing: $_"}};if($engine-match'StageCompletion'){throw 'OpenTelemetry must not advance workflow.'}
 $ops=Get-Content -Raw $paths.Ops;@('OpenTelemetryRuntimeConfigurationState.Unconfigured','Uri.UriSchemeHttps','SovereignOpenTelemetryPolicyGate','internal-service.opentelemetry.activate','envelope.OpenTelemetry','Denied OpenTelemetry decision returned scope','SovereignHttpOpenTelemetryGateway','verify-redaction','X-Governed-Operation','ResponseHeadersRead','ReadBoundedAsync','AiPlanningSignatureVerifier.Verify','DeterministicOpenTelemetryResultAuthorizer')|ForEach-Object{if($ops-notmatch[regex]::Escape($_)){throw "OpenTelemetry operationalization missing: $_"}};if($ops-match'Process\.Start|File\.Write|Directory\.Create'){throw 'OpenTelemetry adapter exposes local mutation.'}
 $p=Get-Content -Raw $paths.Persistence;@('IAuthorizedSovereignDeploymentReceiptReader','IOpenTelemetryDeliveryRunReader','IGovernedOpenTelemetryProfileReader','IOpenTelemetryEvidenceRecorder','BeginTransactionAsync','IsolationLevel.Serializable','ON CONFLICT DO NOTHING','FOR UPDATE','NpgsqlDbType.Jsonb','evidence://opentelemetry','CommitAsync')|ForEach-Object{if($p-notmatch[regex]::Escape($_)){throw "OpenTelemetry persistence missing: $_"}};if($p-match'UPDATE software_factory|DELETE FROM software_factory|CREATE TABLE|CREATE SCHEMA'){throw 'OpenTelemetry persistence mutates outside append.'}
 $inputs=Get-Content -Raw $paths.Inputs;@('opentelemetry_delivery_run_snapshots','opentelemetry_profiles','deployment_id uuid NOT NULL','telemetry_profile_sha256_digest text NOT NULL','PRIMARY KEY(tenant_id,profile_id,version)')|ForEach-Object{if($inputs-notmatch[regex]::Escape($_)){throw "OpenTelemetry input migration missing: $_"}};$evidence=Get-Content -Raw $paths.Evidence;@('opentelemetry_evidence','telemetry_profile_sha256_digest text NOT NULL','PRIMARY KEY(tenant_id,activation_id)','record_json jsonb NOT NULL')|ForEach-Object{if($evidence-notmatch[regex]::Escape($_)){throw "OpenTelemetry evidence migration missing: $_"}}
 $services=Get-Content -Raw $paths.Services;@('OpenTelemetryRuntimeReadiness','SovereignOpenTelemetryPolicyGate','PostgreSqlAuthorizedDeploymentReceiptReader','PostgreSqlOpenTelemetryRunReader','PostgreSqlOpenTelemetryProfileReader','SovereignHttpOpenTelemetryGateway','DeterministicOpenTelemetryResultAuthorizer','PostgreSqlOpenTelemetryEvidenceRecorder')|ForEach-Object{if($services-notmatch[regex]::Escape($_)){throw "OpenTelemetry composition missing: $_"}}
 $ready=Get-Content -Raw $paths.Readiness;@('OpenTelemetry OPA policy gate','Authorized sovereign Deployment receipt reader','OpenTelemetry delivery-run reader','Governed OpenTelemetry profile reader','OpenTelemetry redaction-policy verifier','Institutional OpenTelemetry gateway','OpenTelemetry result authorizer','OpenTelemetry evidence recorder')|ForEach-Object{if($ready-notmatch[regex]::Escape($_)){throw "OpenTelemetry readiness missing: $_"}}
 $endpoint=Get-Content -Raw $paths.Endpoint;@('GetService<IOpenTelemetryPolicyGate>','GetService<IAuthorizedSovereignDeploymentReceiptReader>','GetService<IGovernedOpenTelemetryProfileReader>','Governed OpenTelemetry is not operationally ready','OpenTelemetryDependencyUnavailableException','No Automatic Registration, Enterprise Model mutation, or workflow advancement')|ForEach-Object{if($endpoint-notmatch[regex]::Escape($_)){throw "OpenTelemetry endpoint missing: $_"}}
 $openapi=Get-Content -Raw $paths.OpenApi;@('openTelemetry','unconfigured','invalid','configured','traces, metrics, and logs','No Automatic Registration, Enterprise Model mutation, or workflow advancement')|ForEach-Object{if($openapi-notmatch[regex]::Escape($_)){throw "OpenTelemetry OpenAPI missing: $_"}}
 $acceptance=Get-Content -Raw $wave18AcceptancePath;@('Status: **Satisfied**','CR-019','all 153 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors')|ForEach-Object{if($acceptance-notmatch[regex]::Escape($_)){throw "Wave 18 acceptance missing: $_"}}
}

$wave19AcceptancePath=Join-Path $repositoryRoot 'docs\operationalization\WAVE_19_ACCEPTANCE.md'
if(Test-Path $wave19AcceptancePath){
 $paths=@{Change=Join-Path $repositoryRoot 'docs\change-control\CR-020-OPERATIONALIZATION-WAVE-19.md';Master=Join-Path $repositoryRoot 'docs\PROJECT_MASTER_SPECIFICATION_V2.md';Envelope=Join-Path $repositoryRoot 'backend\Platform.Governance\Policies\ISovereignPolicyEvaluationClient.cs';Engine=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\GovernedAutomaticRegistration.cs';Ops=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\InternalServices\AutomaticRegistrationOperationalization.cs';Persistence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\PostgreSqlAutomaticRegistrationOperationalization.cs';Inputs=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\031_automatic_registration_inputs.sql';Evidence=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\Persistence\Migrations\032_automatic_registration_evidence.sql';Services=Join-Path $repositoryRoot 'backend\Platform.SoftwareFactory\SoftwareFactoryServiceCollectionExtensions.cs';Readiness=Join-Path $repositoryRoot 'backend\Platform.Api\Operations\PlatformRuntimeReadiness.cs';Endpoint=Join-Path $repositoryRoot 'backend\Platform.Api\InternalServices\ServiceStudioEndpoint.cs';OpenApi=Join-Path $repositoryRoot 'backend\Platform.Api\Contracts\openapi.v1.json';Guide=Join-Path $repositoryRoot 'docs\operationalization\WAVE_19_GOVERNED_AUTOMATIC_REGISTRATION.md'};$paths.Values|ForEach-Object{if(-not(Test-Path $_)){throw "Wave 19 artifact missing: $_"}}
 $change=Get-Content -Raw $paths.Change;@('Status: **Approved for Operationalization Wave 19**','Decision: **Approved by the repository owner','No provider','Enterprise Model contextualization')|ForEach-Object{if($change-notmatch[regex]::Escape($_)){throw "Wave 19 approval missing: $_"}}
 $master=Get-Content -Raw $paths.Master;@('CR-020','CR-020-OPERATIONALIZATION-WAVE-19.md','Created, Updated, and Unchanged')|ForEach-Object{if($master-notmatch[regex]::Escape($_)){throw "Master CR-020 authority missing: $_"}}
 $envelope=Get-Content -Raw $paths.Envelope;@('SovereignAutomaticRegistrationPolicyScope','ActivationId','ManifestSha256Digest','ServiceIdentity','RegistrationAllowed','WorkflowAdvancementAllowed','AutomaticRegistration')|ForEach-Object{if($envelope-notmatch[regex]::Escape($_)){throw "Automatic Registration policy envelope missing: $_"}}
 $engine=Get-Content -Raw $paths.Engine;@('policyGate.EvaluateAsync','activationReader.LoadAsync','runReader.LoadAsync','manifestReader.LoadAsync','AutomaticRegistrationEngine(registrationRepository)','resultAuthorizer.AuthorizeAsync','evidenceRecorder.RecordAsync','DeliveryStage.OpenTelemetry','RegistrationDisposition','EnterpriseModelObjectPersisted','WorkflowAdvanced','CanAdvance','Separately approved Enterprise Model contextualization')|ForEach-Object{if($engine-notmatch[regex]::Escape($_)){throw "Operationalized Automatic Registration engine missing: $_"}};if($engine-match'StageCompletion'){throw 'Automatic Registration must not advance workflow.'}
 $ops=Get-Content -Raw $paths.Ops;@('AutomaticRegistrationRuntimeConfigurationState.Unconfigured','SovereignAutomaticRegistrationPolicyGate','internal-service.automatic-registration.execute','envelope.AutomaticRegistration','Denied Automatic Registration decision returned scope','DeterministicAutomaticRegistrationResultAuthorizer','automatic-registration-result')|ForEach-Object{if($ops-notmatch[regex]::Escape($_)){throw "Automatic Registration operationalization missing: $_"}};if($ops-match'Process\.Start|File\.Write|Directory\.Create'){throw 'Automatic Registration adapter exposes local mutation outside its repository.'}
 $p=Get-Content -Raw $paths.Persistence;@('IAuthorizedOpenTelemetryActivationReceiptReader','IAutomaticRegistrationDeliveryRunReader','IGovernedAutomaticRegistrationManifestReader','IAutomaticRegistrationRepository','IAutomaticRegistrationEvidenceRecorder','BeginTransactionAsync','IsolationLevel.Serializable','ON CONFLICT DO NOTHING','FOR UPDATE','RegistrationDisposition.Created','RegistrationDisposition.Updated','RegistrationDisposition.Unchanged','evidence://automatic-registration','CommitAsync')|ForEach-Object{if($p-notmatch[regex]::Escape($_)){throw "Automatic Registration persistence missing: $_"}};if($p-match'DELETE FROM software_factory'){throw 'Automatic Registration persistence exposes deletion.'}
 $inputs=Get-Content -Raw $paths.Inputs;@('automatic_registration_delivery_run_snapshots','automatic_registration_manifests','automatic_registrations','request_fingerprint text NOT NULL','PRIMARY KEY(tenant_id,environment_name,service_identity)')|ForEach-Object{if($inputs-notmatch[regex]::Escape($_)){throw "Automatic Registration input migration missing: $_"}};$evidence=Get-Content -Raw $paths.Evidence;@('automatic_registration_evidence','enterprise_object_id uuid NOT NULL','PRIMARY KEY(tenant_id,registration_id)','record_json jsonb NOT NULL')|ForEach-Object{if($evidence-notmatch[regex]::Escape($_)){throw "Automatic Registration evidence migration missing: $_"}}
 $services=Get-Content -Raw $paths.Services;@('AutomaticRegistrationRuntimeReadiness','SovereignAutomaticRegistrationPolicyGate','PostgreSqlAuthorizedOpenTelemetryActivationReceiptReader','PostgreSqlAutomaticRegistrationRunReader','PostgreSqlAutomaticRegistrationManifestReader','PostgreSqlAutomaticRegistrationRepository','DeterministicAutomaticRegistrationResultAuthorizer','PostgreSqlAutomaticRegistrationEvidenceRecorder')|ForEach-Object{if($services-notmatch[regex]::Escape($_)){throw "Automatic Registration composition missing: $_"}}
 $ready=Get-Content -Raw $paths.Readiness;@('Automatic Registration OPA policy gate','Authorized OpenTelemetry activation receipt reader','Automatic Registration delivery-run reader','Governed Automatic Registration manifest reader','Automatic Registration repository','Automatic Registration result authorizer','Automatic Registration evidence recorder')|ForEach-Object{if($ready-notmatch[regex]::Escape($_)){throw "Automatic Registration readiness missing: $_"}}
 $endpoint=Get-Content -Raw $paths.Endpoint;@('GetService<IAutomaticRegistrationPolicyGate>','GetService<IAuthorizedOpenTelemetryActivationReceiptReader>','GetService<IAutomaticRegistrationRepository>','Governed Automatic Registration is not operationally ready','AutomaticRegistrationDependencyUnavailableException','No workflow advancement or later station is available')|ForEach-Object{if($endpoint-notmatch[regex]::Escape($_)){throw "Automatic Registration endpoint missing: $_"}}
 $openapi=Get-Content -Raw $paths.OpenApi;@('automaticRegistration','unconfigured','invalid','configured','signed manifest','No workflow advancement or later station is available')|ForEach-Object{if($openapi-notmatch[regex]::Escape($_)){throw "Automatic Registration OpenAPI missing: $_"}}
 $acceptance=Get-Content -Raw $wave19AcceptancePath;@('Status: **Satisfied**','CR-020','all 153 dependencies remain disconnected and fail closed','All 15 projects build with zero warnings and zero errors')|ForEach-Object{if($acceptance-notmatch[regex]::Escape($_)){throw "Wave 19 acceptance missing: $_"}}
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
