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
