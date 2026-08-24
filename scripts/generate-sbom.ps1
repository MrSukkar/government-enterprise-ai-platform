[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$scriptRepositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = $scriptRepositoryRoot
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $scriptRepositoryRoot 'artifacts\sbom.cdx.json'
}
$components = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)

Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -Filter '*.csproj' |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    ForEach-Object {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
        $key = "application:$projectName"
        $components[$key] = [ordered]@{
            type = 'application'
            name = $projectName
            version = '0.0.0-source'
            'bom-ref' = "pkg:generic/$projectName@source"
        }
    }

Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -Filter 'project.assets.json' |
    Where-Object { $_.FullName -match '\\obj\\' } |
    ForEach-Object {
        $assets = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
        $assets.libraries.PSObject.Properties |
            Where-Object { $_.Value.type -eq 'package' } |
            ForEach-Object {
                $parts = $_.Name -split '/', 2
                $name = $parts[0]
                $version = $parts[1]
                $key = "library:$name@$version"
                $components[$key] = [ordered]@{
                    type = 'library'
                    name = $name
                    version = $version
                    'bom-ref' = "pkg:nuget/$name@$version"
                    purl = "pkg:nuget/$name@$version"
                }
            }
    }

$document = [ordered]@{
    bomFormat = 'CycloneDX'
    specVersion = '1.6'
    serialNumber = "urn:uuid:$([guid]::NewGuid())"
    version = 1
    metadata = [ordered]@{
        timestamp = [DateTimeOffset]::UtcNow.ToString('O')
        component = [ordered]@{
            type = 'application'
            name = 'Government Enterprise AI Platform'
            version = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { 'local-source' }
        }
    }
    components = @($components.Values | Sort-Object { $_.type }, { $_.name }, { $_.version })
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$document | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Output "SBOM: $($components.Count) components -> $OutputPath"
