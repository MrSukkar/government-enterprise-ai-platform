param(
    [switch]$StartInfrastructure
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$runtimeRoot = Join-Path $repositoryRoot '.runtime\integration-demo-stage-03'
$tlsRoot = Join-Path $runtimeRoot 'tls'
$opaRuntimeRoot = Join-Path $runtimeRoot 'opa'
$opaSource = Join-Path $repositoryRoot 'deploy\demo\stage-03\opa'
$seedPath = Join-Path $repositoryRoot 'deploy\demo\stage-03\neo4j\seed.cypher'
$composePath = Join-Path $repositoryRoot 'deploy\demo\stage-03\compose.yml'
$opaImage = 'openpolicyagent/opa:1.20.2-static@sha256:bb245e9e36be0d0ed486c240b606c56be7aba96014a4a87895fed4ba7a6dfa8d'
$postgresImage = 'postgres:18.6-bookworm@sha256:1c59e2c3c818eaa0f0628f695b36e7c9e362d6b219b36a54a32df645cbd7e1af'

if ([string]::IsNullOrWhiteSpace($env:GEAIP_DEMO_POSTGRES_PASSWORD) -or
    $env:GEAIP_DEMO_POSTGRES_PASSWORD.Length -lt 16)
{
    throw 'GEAIP_DEMO_POSTGRES_PASSWORD must be supplied outside Git and contain at least 16 characters.'
}
if ([string]::IsNullOrWhiteSpace($env:GEAIP_DEMO_NEO4J_PASSWORD) -or
    $env:GEAIP_DEMO_NEO4J_PASSWORD.Length -lt 16)
{
    throw 'GEAIP_DEMO_NEO4J_PASSWORD must be supplied outside Git and contain at least 16 characters.'
}

New-Item -ItemType Directory -Force $tlsRoot, $opaRuntimeRoot | Out-Null

$certificatePath = Join-Path $tlsRoot 'localhost.pem'
& dotnet dev-certs https --trust --export-path $certificatePath --format Pem --no-password
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $certificatePath) -or
    -not (Test-Path (Join-Path $tlsRoot 'localhost.key')))
{
    throw 'Trusted localhost PEM certificate export failed.'
}

$privateKey = Join-Path $opaRuntimeRoot 'private.pem'
$publicKey = Join-Path $opaRuntimeRoot 'public.pem'
$bundlePath = Join-Path $opaRuntimeRoot 'bundle.tar.gz'

if (-not (Test-Path $privateKey) -or -not (Test-Path $publicKey))
{
    & docker run --rm --mount "type=bind,source=$opaRuntimeRoot,target=/keys" $postgresImage bash -ec 'openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out /keys/private.pem && openssl pkey -in /keys/private.pem -pubout -out /keys/public.pem'
    if ($LASTEXITCODE -ne 0) { throw 'OPA signing-key generation failed.' }
}

& docker run --rm --mount "type=bind,source=$opaRuntimeRoot,target=/keys" $postgresImage bash -ec 'chown -R 1000:1000 /keys && chmod 600 /keys/private.pem && chmod 644 /keys/public.pem'
if ($LASTEXITCODE -ne 0) { throw 'OPA signing-key permission preparation failed.' }

& docker run --rm --mount "type=bind,source=$opaSource,target=/bundle-source,readonly" --mount "type=bind,source=$opaRuntimeRoot,target=/runtime" $opaImage build --bundle /bundle-source --output /runtime/bundle.tar.gz --revision geaip-demo-stage-03-2026.09.18 --signing-key /runtime/private.pem --signing-alg RS256
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $bundlePath)) { throw 'Signed OPA bundle creation failed.' }

& docker run --rm --mount "type=bind,source=$opaRuntimeRoot,target=/runtime,readonly" $opaImage build --bundle /runtime/bundle.tar.gz --output /tmp/verified-bundle.tar.gz --verification-key /runtime/public.pem
if ($LASTEXITCODE -ne 0) { throw 'Signed OPA bundle verification failed.' }

if ($StartInfrastructure)
{
    & docker compose -f $composePath up -d
    if ($LASTEXITCODE -ne 0) { throw 'Stage 03 infrastructure startup failed.' }

    $seeded = $false
    foreach ($attempt in 1..30)
    {
        Start-Sleep -Seconds 2
        Get-Content -Raw $seedPath | docker compose -f $composePath exec -T neo4j cypher-shell -a neo4j+ssc://localhost:7687 -u neo4j -p $env:GEAIP_DEMO_NEO4J_PASSWORD --format plain
        if ($LASTEXITCODE -eq 0) { $seeded = $true; break }
    }
    if (-not $seeded) { throw 'Synthetic Enterprise Graph seeding failed.' }
}

Write-Output 'Stage 03 runtime material is prepared outside Git.'
Write-Output "OPA bundle: $bundlePath"
Write-Output 'Set PostgreSqlIntentRegistration__ConnectionString and Neo4jEnterpriseGraph__Username/Password with runtime secrets before starting the API.'
