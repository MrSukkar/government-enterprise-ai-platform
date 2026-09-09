[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$apiExecutable = Join-Path $repositoryRoot 'backend\Platform.Api\bin\Debug\net10.0\Platform.Api.exe'

if (-not (Test-Path -LiteralPath $apiExecutable)) {
    throw "Missing built API executable: $apiExecutable"
}

Add-Type -AssemblyName System.Net.Http

$portProbe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$portProbe.Start()
$port = ([Net.IPEndPoint]$portProbe.LocalEndpoint).Port
$portProbe.Stop()

$baseAddress = "http://127.0.0.1:$port"
$logId = [Guid]::NewGuid().ToString('N')
$stdoutPath = Join-Path ([IO.Path]::GetTempPath()) "platform-api-$logId.stdout.log"
$stderrPath = Join-Path ([IO.Path]::GetTempPath()) "platform-api-$logId.stderr.log"
$previousEnvironment = [Environment]::GetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Process')
$processEnvironment = [Environment]::GetEnvironmentVariables('Process')
$uppercasePathEntry = $processEnvironment.GetEnumerator() |
    Where-Object { $_.Key -ceq 'PATH' } |
    Select-Object -First 1
$apiProcess = $null
$httpHandler = $null
$client = $null

try {
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Development', 'Process')
    if ($null -ne $uppercasePathEntry) {
        [Environment]::SetEnvironmentVariable('PATH', $null, 'Process')
    }

    try {
        $apiProcess = Start-Process -FilePath $apiExecutable -ArgumentList @('--urls', $baseAddress) -WorkingDirectory (Split-Path -Parent $apiExecutable) -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath -PassThru
    }
    finally {
        if ($null -ne $uppercasePathEntry) {
            [Environment]::SetEnvironmentVariable('PATH', [string]$uppercasePathEntry.Value, 'Process')
        }
    }

    $httpHandler = [Net.Http.HttpClientHandler]::new()
    $httpHandler.UseProxy = $false
    $client = [Net.Http.HttpClient]::new($httpHandler)
    $client.Timeout = [TimeSpan]::FromSeconds(2)
    $deadline = (Get-Date).AddSeconds(30)
    $liveness = $null

    do {
        if ($apiProcess.HasExited) {
            $stdout = Get-Content -LiteralPath $stdoutPath -Raw -ErrorAction SilentlyContinue
            $stderr = Get-Content -LiteralPath $stderrPath -Raw -ErrorAction SilentlyContinue
            $separator = [Environment]::NewLine
            throw "Development API exited before becoming live.$separator$stdout$separator$stderr"
        }

        try {
            $liveness = $client.GetAsync("$baseAddress/health").GetAwaiter().GetResult()
        }
        catch {
            $liveness = $null
        }

        if ($null -eq $liveness -or [int]$liveness.StatusCode -ne 200) {
            Start-Sleep -Milliseconds 250
        }
    }
    while (($null -eq $liveness -or [int]$liveness.StatusCode -ne 200) -and (Get-Date) -lt $deadline)

    if ($null -eq $liveness -or [int]$liveness.StatusCode -ne 200) {
        $stdout = Get-Content -LiteralPath $stdoutPath -Raw -ErrorAction SilentlyContinue
        $stderr = Get-Content -LiteralPath $stderrPath -Raw -ErrorAction SilentlyContinue
        $separator = [Environment]::NewLine
        throw "Development API did not become live within 30 seconds.$separator$stdout$separator$stderr"
    }

    $readiness = $client.GetAsync("$baseAddress/health/ready").GetAwaiter().GetResult()
    $openApi = $client.GetAsync("$baseAddress/openapi/v1.json").GetAwaiter().GetResult()
    $developerPortal = $client.GetAsync("$baseAddress/developers").GetAwaiter().GetResult()
    $internalService = $client.GetAsync("$baseAddress/api/v1/internal-services/foundation").GetAwaiter().GetResult()
    $intentContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $intentSubmission = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents",
        $intentContent).GetAwaiter().GetResult()
    $registrationContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $intentRegistration = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/register",
        $registrationContent).GetAwaiter().GetResult()
    $contextContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $enterpriseContext = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context",
        $contextContent).GetAwaiter().GetResult()
    $systemsContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $existingSystems = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems",
        $systemsContent).GetAwaiter().GetResult()
    $architectureContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $existingArchitecture = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture",
        $architectureContent).GetAwaiter().GetResult()
    $packagesContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $approvedPackages = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture/00000000-0000-0000-0000-000000000004/approved-packages",
        $packagesContent).GetAwaiter().GetResult()
    $planningContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $aiPlanning = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture/00000000-0000-0000-0000-000000000004/approved-packages/00000000-0000-0000-0000-000000000005/ai-planning",
        $planningContent).GetAwaiter().GetResult()
    $generationContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $codeGeneration = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture/00000000-0000-0000-0000-000000000004/approved-packages/00000000-0000-0000-0000-000000000005/ai-planning/00000000-0000-0000-0000-000000000006/code-generation",
        $generationContent).GetAwaiter().GetResult()
    $staticValidationContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $staticValidation = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture/00000000-0000-0000-0000-000000000004/approved-packages/00000000-0000-0000-0000-000000000005/ai-planning/00000000-0000-0000-0000-000000000006/code-generation/00000000-0000-0000-0000-000000000007/static-validation",
        $staticValidationContent).GetAwaiter().GetResult()
    $securityValidationContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $securityValidation = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture/00000000-0000-0000-0000-000000000004/approved-packages/00000000-0000-0000-0000-000000000005/ai-planning/00000000-0000-0000-0000-000000000006/code-generation/00000000-0000-0000-0000-000000000007/static-validation/00000000-0000-0000-0000-000000000008/security-validation",
        $securityValidationContent).GetAwaiter().GetResult()
    $sandboxContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $sandbox = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture/00000000-0000-0000-0000-000000000004/approved-packages/00000000-0000-0000-0000-000000000005/ai-planning/00000000-0000-0000-0000-000000000006/code-generation/00000000-0000-0000-0000-000000000007/static-validation/00000000-0000-0000-0000-000000000008/security-validation/00000000-0000-0000-0000-000000000009/sandbox",
        $sandboxContent).GetAwaiter().GetResult()
    $testsContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $tests = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture/00000000-0000-0000-0000-000000000004/approved-packages/00000000-0000-0000-0000-000000000005/ai-planning/00000000-0000-0000-0000-000000000006/code-generation/00000000-0000-0000-0000-000000000007/static-validation/00000000-0000-0000-0000-000000000008/security-validation/00000000-0000-0000-0000-000000000009/sandbox/00000000-0000-0000-0000-00000000000a/tests",
        $testsContent).GetAwaiter().GetResult()
    $humanReviewContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $humanReview = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture/00000000-0000-0000-0000-000000000004/approved-packages/00000000-0000-0000-0000-000000000005/ai-planning/00000000-0000-0000-0000-000000000006/code-generation/00000000-0000-0000-0000-000000000007/static-validation/00000000-0000-0000-0000-000000000008/security-validation/00000000-0000-0000-0000-000000000009/sandbox/00000000-0000-0000-0000-00000000000a/tests/00000000-0000-0000-0000-00000000000b/human-review",
        $humanReviewContent).GetAwaiter().GetResult()
    $gitContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $gitCommit = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/intents/00000000-0000-0000-0000-000000000001/enterprise-context/00000000-0000-0000-0000-000000000002/existing-systems/00000000-0000-0000-0000-000000000003/existing-architecture/00000000-0000-0000-0000-000000000004/approved-packages/00000000-0000-0000-0000-000000000005/ai-planning/00000000-0000-0000-0000-000000000006/code-generation/00000000-0000-0000-0000-000000000007/static-validation/00000000-0000-0000-0000-000000000008/security-validation/00000000-0000-0000-0000-000000000009/sandbox/00000000-0000-0000-0000-00000000000a/tests/00000000-0000-0000-0000-00000000000b/human-review/00000000-0000-0000-0000-00000000000c/git",
        $gitContent).GetAwaiter().GetResult()
    $ciCdContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $ciCd = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/git/00000000-0000-0000-0000-00000000000d/cicd",
        $ciCdContent).GetAwaiter().GetResult()
    $artifactContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $artifact = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/cicd/00000000-0000-0000-0000-00000000000e/artifact",
        $artifactContent).GetAwaiter().GetResult()
    $deploymentContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $deployment = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/artifacts/00000000-0000-0000-0000-00000000000f/deployment",
        $deploymentContent).GetAwaiter().GetResult()
    $openTelemetryContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $openTelemetry = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/deployments/00000000-0000-0000-0000-000000000010/opentelemetry",
        $openTelemetryContent).GetAwaiter().GetResult()
    $automaticRegistrationContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $automaticRegistration = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/opentelemetry/00000000-0000-0000-0000-000000000011/automatic-registration",
        $automaticRegistrationContent).GetAwaiter().GetResult()
    $enterpriseModelContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $enterpriseModel = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/registrations/00000000-0000-0000-0000-000000000012/enterprise-model",
        $enterpriseModelContent).GetAwaiter().GetResult()
    $evidenceCompletionContent = [Net.Http.StringContent]::new('{}', [Text.Encoding]::UTF8, 'application/json')
    $evidenceCompletion = $client.PostAsync(
        "$baseAddress/api/v1/internal-services/enterprise-model/00000000-0000-0000-0000-000000000013/evidence",
        $evidenceCompletionContent).GetAwaiter().GetResult()

    if ([int]$readiness.StatusCode -ne 503) {
        throw "Expected fail-closed readiness status 503, received $([int]$readiness.StatusCode)."
    }
    if ([int]$openApi.StatusCode -ne 200) {
        throw "Expected OpenAPI status 200, received $([int]$openApi.StatusCode)."
    }
    if ([int]$developerPortal.StatusCode -ne 200) {
        throw "Expected developer portal status 200, received $([int]$developerPortal.StatusCode)."
    }
    if ([int]$internalService.StatusCode -ne 200) {
        throw "Expected Create Internal Service status 200, received $([int]$internalService.StatusCode)."
    }
    if ([int]$intentSubmission.StatusCode -ne 401 -or
        $intentSubmission.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous governed intent submission did not fail closed with a bearer challenge.'
    }
    if ([int]$intentRegistration.StatusCode -ne 401 -or
        $intentRegistration.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous governed intent registration did not fail closed with a bearer challenge.'
    }
    if ([int]$enterpriseContext.StatusCode -ne 401 -or
        $enterpriseContext.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Enterprise Context discovery did not fail closed with a bearer challenge.'
    }
    if ([int]$existingSystems.StatusCode -ne 401 -or
        $existingSystems.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Existing Systems discovery did not fail closed with a bearer challenge.'
    }
    if ([int]$existingArchitecture.StatusCode -ne 401 -or
        $existingArchitecture.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Existing Architecture discovery did not fail closed with a bearer challenge.'
    }
    if ([int]$approvedPackages.StatusCode -ne 401 -or
        $approvedPackages.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Approved Packages selection did not fail closed with a bearer challenge.'
    }
    if ([int]$aiPlanning.StatusCode -ne 401 -or $aiPlanning.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous AI Planning did not fail closed with a bearer challenge.'
    }
    if ([int]$codeGeneration.StatusCode -ne 401 -or $codeGeneration.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Code Generation did not fail closed with a bearer challenge.'
    }
    if ([int]$staticValidation.StatusCode -ne 401 -or $staticValidation.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Static Validation did not fail closed with a bearer challenge.'
    }
    if ([int]$securityValidation.StatusCode -ne 401 -or $securityValidation.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Security Validation did not fail closed with a bearer challenge.'
    }
    if ([int]$sandbox.StatusCode -ne 401 -or $sandbox.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Sandbox execution did not fail closed with a bearer challenge.'
    }
    if ([int]$tests.StatusCode -ne 401 -or $tests.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Tests execution did not fail closed with a bearer challenge.'
    }
    if ([int]$humanReview.StatusCode -ne 401 -or $humanReview.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Human Review did not fail closed with a bearer challenge.'
    }
    if ([int]$gitCommit.StatusCode -ne 401 -or $gitCommit.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Git source commit did not fail closed with a bearer challenge.'
    }
    if ([int]$ciCd.StatusCode -ne 401 -or $ciCd.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous CI/CD execution did not fail closed with a bearer challenge.'
    }
    if ([int]$artifact.StatusCode -ne 401 -or $artifact.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Artifact publication did not fail closed with a bearer challenge.'
    }
    if ([int]$deployment.StatusCode -ne 401 -or $deployment.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Deployment did not fail closed with a bearer challenge.'
    }
    if ([int]$openTelemetry.StatusCode -ne 401 -or $openTelemetry.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous OpenTelemetry activation did not fail closed with a bearer challenge.'
    }
    if ([int]$automaticRegistration.StatusCode -ne 401 -or $automaticRegistration.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Automatic Registration did not fail closed with a bearer challenge.'
    }
    if ([int]$enterpriseModel.StatusCode -ne 401 -or $enterpriseModel.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Enterprise Model contextualization did not fail closed with a bearer challenge.'
    }
    if ([int]$evidenceCompletion.StatusCode -ne 401 -or $evidenceCompletion.Headers.WwwAuthenticate.Scheme -notcontains 'Bearer') {
        throw 'Anonymous Evidence completion did not fail closed with a bearer challenge.'
    }

    $readinessBody = $readiness.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($readinessBody.status -ne 'not-ready' -or
        $readinessBody.failClosed -ne $true -or
        [int]$readinessBody.missingDependencyCount -ne 153) {
        throw 'Readiness response did not disclose the expected 153 fail-closed runtime dependencies.'
    }
    if ($readinessBody.controlPlanes.identity -ne 'unconfigured' -or
        $readinessBody.controlPlanes.policy -ne 'unconfigured' -or
        $readinessBody.controlPlanes.postgresqlIntentRegistration -ne 'unconfigured' -or
        $readinessBody.controlPlanes.neo4jEnterpriseGraph -ne 'unconfigured' -or
        $readinessBody.controlPlanes.enterpriseContext -ne 'unconfigured' -or
        $readinessBody.controlPlanes.existingSystems -ne 'unconfigured' -or
        $readinessBody.controlPlanes.existingArchitecture -ne 'unconfigured' -or
        $readinessBody.controlPlanes.approvedPackages -ne 'unconfigured' -or
        $readinessBody.controlPlanes.aiPlanning -ne 'unconfigured' -or
        $readinessBody.controlPlanes.codeGeneration -ne 'unconfigured' -or
        $readinessBody.controlPlanes.staticValidation -ne 'unconfigured' -or
        $readinessBody.controlPlanes.securityValidation -ne 'unconfigured' -or
        $readinessBody.controlPlanes.sandbox -ne 'unconfigured' -or $readinessBody.controlPlanes.tests -ne 'unconfigured' -or
        $readinessBody.controlPlanes.humanReview -ne 'unconfigured' -or $readinessBody.controlPlanes.git -ne 'unconfigured') {
        throw 'Repository-default identity, policy, PostgreSQL, Neo4j, Enterprise Context, Existing Systems, Existing Architecture, Approved Packages, AI Planning, Code Generation, Static Validation, Security Validation, and Sandbox control planes did not remain explicitly unconfigured.'
    }

    $internalServiceBody = $internalService.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    $expectedDeliveryStages = @(
        'Intent', 'EnterpriseContext', 'ExistingSystems', 'ExistingArchitecture',
        'ApprovedPackages', 'AiPlanning', 'CodeGeneration', 'StaticValidation',
        'SecurityValidation', 'Sandbox', 'Tests', 'HumanReview', 'Git', 'CiCd',
        'Artifact', 'Deployment', 'OpenTelemetry', 'AutomaticRegistration',
        'EnterpriseModel', 'Evidence'
    )
    $actualDeliveryStages = @($internalServiceBody.deliveryStages | ForEach-Object { $_.key })
    if ($internalServiceBody.productId -ne 'sovereign-internal-services' -or
        $internalServiceBody.increment -ne 'Operational Increment 22 - Governed Evidence Completion' -or
        [int]$actualDeliveryStages.Count -ne $expectedDeliveryStages.Count -or
        ($actualDeliveryStages -join ',') -ne ($expectedDeliveryStages -join ',')) {
        throw 'Create Internal Service Workspace foundation is unavailable or incomplete.'
    }

    $openApiBody = $openApi.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($openApiBody.openapi -ne '3.1.0' -or
        -not $openApiBody.paths.'/health/ready' -or
        -not $openApiBody.paths.'/api/v1/internal-services/foundation' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents/register' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents/{registrationId}/enterprise-context' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems/{systemsDiscoveryId}/existing-architecture' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems/{systemsDiscoveryId}/existing-architecture/{architectureDiscoveryId}/approved-packages' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems/{systemsDiscoveryId}/existing-architecture/{architectureDiscoveryId}/approved-packages/{packageSelectionId}/ai-planning' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems/{systemsDiscoveryId}/existing-architecture/{architectureDiscoveryId}/approved-packages/{packageSelectionId}/ai-planning/{planningId}/code-generation' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems/{systemsDiscoveryId}/existing-architecture/{architectureDiscoveryId}/approved-packages/{packageSelectionId}/ai-planning/{planningId}/code-generation/{generationId}/static-validation' -or
        -not $openApiBody.paths.'/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems/{systemsDiscoveryId}/existing-architecture/{architectureDiscoveryId}/approved-packages/{packageSelectionId}/ai-planning/{planningId}/code-generation/{generationId}/static-validation/{staticValidationId}/security-validation/{securityValidationId}/sandbox/{sandboxExecutionId}/tests/{testsExecutionId}/human-review/{reviewId}/git' -or
        -not $openApiBody.paths.'/api/v1/internal-services/git/{gitOperationId}/cicd' -or
        -not $openApiBody.paths.'/api/v1/internal-services/cicd/{ciCdExecutionId}/artifact' -or
        -not $openApiBody.paths.'/api/v1/internal-services/artifacts/{artifactPublicationId}/deployment' -or
        -not $openApiBody.paths.'/api/v1/internal-services/deployments/{deploymentId}/opentelemetry' -or
        -not $openApiBody.paths.'/api/v1/internal-services/opentelemetry/{activationId}/automatic-registration' -or
        -not $openApiBody.paths.'/api/v1/internal-services/registrations/{registrationId}/enterprise-model' -or
        -not $openApiBody.paths.'/api/v1/internal-services/enterprise-model/{contextualizationId}/evidence') {
        throw 'Runtime OpenAPI response does not contain the approved complete path through Evidence.'
    }

    $portalBody = $developerPortal.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    @('Platform Developer Console', 'NOT READY - FAIL CLOSED', 'Runtime boundary readiness') | ForEach-Object {
        if ($portalBody -notmatch [regex]::Escape($_)) {
            throw "Developer portal is missing required content '$($_)'."
        }
    }

Write-Output 'RUNTIME VERIFIED: Development API live; all control planes through Git remain safely unconfigured; 153 runtime dependencies fail closed; the approved Create Internal Service contract remains complete through Evidence.'
}
finally {
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', $previousEnvironment, 'Process')

    if ($null -ne $client) {
        $client.Dispose()
    }

    if ($null -ne $httpHandler) {
        $httpHandler.Dispose()
    }

    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }

    Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
}
