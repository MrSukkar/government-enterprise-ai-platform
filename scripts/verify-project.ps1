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

if (-not $NoBuild) {
    & dotnet build $solutionPath --no-restore --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed with exit code $LASTEXITCODE."
    }
}

Write-Output ('VERIFIED: Phase {0:D2}, {1} projects, acceptance satisfied.' -f $phaseNumber, $projectCount)
