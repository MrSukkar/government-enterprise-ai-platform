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
    @('Intent', 'EnterpriseContext', 'ExistingArchitecture', 'ApprovedPackages', 'AiPlanning', 'CodeGeneration', 'StaticValidation', 'SecurityValidation', 'Sandbox', 'Tests', 'HumanReview', 'Git', 'CiCd', 'Artifact', 'Deployment', 'Registration', 'Observability', 'Evidence') | ForEach-Object {
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

if (-not $NoBuild) {
    & dotnet build $solutionPath --no-restore --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed with exit code $LASTEXITCODE."
    }
}

Write-Output ('VERIFIED: Phase {0:D2}, {1} projects, acceptance satisfied.' -f $phaseNumber, $projectCount)
