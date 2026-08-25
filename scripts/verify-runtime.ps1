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
$apiProcess = $null
$client = $null

try {
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Development', 'Process')

    $apiProcess = Start-Process -FilePath $apiExecutable -ArgumentList @('--urls', $baseAddress) -WorkingDirectory (Split-Path -Parent $apiExecutable) -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath -PassThru

    $client = [Net.Http.HttpClient]::new()
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
        throw 'Development API did not become live within 30 seconds.'
    }

    $readiness = $client.GetAsync("$baseAddress/health/ready").GetAwaiter().GetResult()
    $openApi = $client.GetAsync("$baseAddress/openapi/v1.json").GetAwaiter().GetResult()
    $developerPortal = $client.GetAsync("$baseAddress/developers").GetAwaiter().GetResult()

    if ([int]$readiness.StatusCode -ne 503) {
        throw "Expected fail-closed readiness status 503, received $([int]$readiness.StatusCode)."
    }
    if ([int]$openApi.StatusCode -ne 200) {
        throw "Expected OpenAPI status 200, received $([int]$openApi.StatusCode)."
    }
    if ([int]$developerPortal.StatusCode -ne 200) {
        throw "Expected developer portal status 200, received $([int]$developerPortal.StatusCode)."
    }

    $readinessBody = $readiness.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($readinessBody.status -ne 'not-ready' -or
        $readinessBody.failClosed -ne $true -or
        [int]$readinessBody.missingDependencyCount -ne 16) {
        throw 'Readiness response did not disclose the expected 16 fail-closed runtime dependencies.'
    }

    $openApiBody = $openApi.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($openApiBody.openapi -ne '3.1.0' -or
        -not $openApiBody.paths.'/health/ready') {
        throw 'Runtime OpenAPI response does not contain the approved readiness endpoint.'
    }

    $portalBody = $developerPortal.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    @('Platform Developer Console', 'NOT READY - FAIL CLOSED', 'Runtime boundary readiness') | ForEach-Object {
        if ($portalBody -notmatch [regex]::Escape($_)) {
            throw "Developer portal is missing required content '$($_)'."
        }
    }

    Write-Output 'RUNTIME VERIFIED: Development API live, readiness fail-closed, OpenAPI and developer portal available.'
}
finally {
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', $previousEnvironment, 'Process')

    if ($null -ne $client) {
        $client.Dispose()
    }

    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }

    Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
}
