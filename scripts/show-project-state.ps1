[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$statePath = Join-Path $repositoryRoot 'project-os\project-state.json'
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json

[pscustomobject]@{
    Project = $state.project
    CurrentPhase = ('{0:D2} — {1}' -f [int]$state.currentPhase.number, $state.currentPhase.name)
    PhaseStatus = $state.currentPhase.status
    NextPhase = ('{0:D2} — {1}' -f [int]$state.nextPermittedPhase.number, $state.nextPermittedPhase.name)
    LastBuild = ('{0} projects, {1} warnings, {2} errors' -f $state.lastVerified.projects, $state.lastVerified.buildWarnings, $state.lastVerified.buildErrors)
    Branch = $state.sourceOfTruth.branch
}

