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

    $readinessBody = $readiness.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($readinessBody.status -ne 'not-ready' -or
        $readinessBody.failClosed -ne $true -or
        [int]$readinessBody.missingDependencyCount -ne 16) {
        throw 'Readiness response did not disclose the expected 16 fail-closed runtime dependencies.'
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
        [int]$actualDeliveryStages.Count -ne $expectedDeliveryStages.Count -or
        ($actualDeliveryStages -join ',') -ne ($expectedDeliveryStages -join ',')) {
        throw 'Create Internal Service Workspace foundation is unavailable or incomplete.'
    }

    $openApiBody = $openApi.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($openApiBody.openapi -ne '3.1.0' -or
        -not $openApiBody.paths.'/health/ready' -or
        -not $openApiBody.paths.'/api/v1/internal-services/foundation') {
        throw 'Runtime OpenAPI response does not contain the approved readiness and product-foundation endpoints.'
    }

    $portalBody = $developerPortal.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    @('Platform Developer Console', 'NOT READY - FAIL CLOSED', 'Runtime boundary readiness') | ForEach-Object {
        if ($portalBody -notmatch [regex]::Escape($_)) {
            throw "Developer portal is missing required content '$($_)'."
        }
    }

    Write-Output 'RUNTIME VERIFIED: Development API live, readiness fail-closed, OpenAPI, developer portal, and Create Internal Service available.'
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
