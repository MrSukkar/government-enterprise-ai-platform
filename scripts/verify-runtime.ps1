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

    $readinessBody = $readiness.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($readinessBody.status -ne 'not-ready' -or
        $readinessBody.failClosed -ne $true -or
        [int]$readinessBody.missingDependencyCount -ne 27) {
        throw 'Readiness response did not disclose the expected 27 fail-closed runtime dependencies.'
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
        $internalServiceBody.increment -ne 'Operational Increment 05 - Authorized Existing Systems Discovery' -or
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
        -not $openApiBody.paths.'/api/v1/internal-services/intents/{registrationId}/enterprise-context/{contextDiscoveryId}/existing-systems') {
        throw 'Runtime OpenAPI response does not contain the approved readiness, intent, context, and Existing Systems endpoints.'
    }

    $portalBody = $developerPortal.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    @('Platform Developer Console', 'NOT READY - FAIL CLOSED', 'Runtime boundary readiness') | ForEach-Object {
        if ($portalBody -notmatch [regex]::Escape($_)) {
            throw "Developer portal is missing required content '$($_)'."
        }
    }

    Write-Output 'RUNTIME VERIFIED: Development API live, 27 runtime dependencies fail-closed, and authorized Existing Systems discovery remains protected without live adapters.'
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
