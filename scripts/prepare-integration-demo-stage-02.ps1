param(
    [switch]$StartInfrastructure
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$runtimeRoot = Join-Path $repositoryRoot '.runtime\integration-demo-stage-02'
$tlsRoot = Join-Path $runtimeRoot 'tls'
$opaRuntimeRoot = Join-Path $runtimeRoot 'opa'
$opaSource = Join-Path $repositoryRoot 'deploy\demo\stage-02\opa'
$composePath = Join-Path $repositoryRoot 'deploy\demo\stage-02\compose.yml'
$opaImage = 'openpolicyagent/opa:1.20.2-static@sha256:bb245e9e36be0d0ed486c240b606c56be7aba96014a4a87895fed4ba7a6dfa8d'
$postgresImage = 'postgres:18.6-bookworm@sha256:1c59e2c3c818eaa0f0628f695b36e7c9e362d6b219b36a54a32df645cbd7e1af'

if ([string]::IsNullOrWhiteSpace($env:GEAIP_DEMO_POSTGRES_PASSWORD) -or
    $env:GEAIP_DEMO_POSTGRES_PASSWORD.Length -lt 16)
{
    throw 'GEAIP_DEMO_POSTGRES_PASSWORD must be supplied outside Git and contain at least 16 characters.'
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
    $keyArguments = @(
        'run', '--rm',
        '--mount', "type=bind,source=$opaRuntimeRoot,target=/keys",
        $postgresImage,
        'bash', '-ec',
        'openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out /keys/private.pem && openssl pkey -in /keys/private.pem -pubout -out /keys/public.pem'
    )
    & docker @keyArguments
    if ($LASTEXITCODE -ne 0) { throw 'OPA signing-key generation failed.' }
}

$permissionArguments = @(
    'run', '--rm',
    '--mount', "type=bind,source=$opaRuntimeRoot,target=/keys",
    $postgresImage,
    'bash', '-ec',
    'chown -R 1000:1000 /keys && chmod 600 /keys/private.pem && chmod 644 /keys/public.pem'
)
& docker @permissionArguments
if ($LASTEXITCODE -ne 0) { throw 'OPA signing-key permission preparation failed.' }

$buildArguments = @(
    'run', '--rm',
    '--mount', "type=bind,source=$opaSource,target=/bundle-source,readonly",
    '--mount', "type=bind,source=$opaRuntimeRoot,target=/runtime",
    $opaImage,
    'build', '--bundle', '/bundle-source', '--output', '/runtime/bundle.tar.gz',
    '--revision', 'geaip-demo-intent-registration-2026.09.18',
    '--signing-key', '/runtime/private.pem', '--signing-alg', 'RS256'
)
& docker @buildArguments
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $bundlePath)) {
    throw 'Signed OPA bundle creation failed.'
}

$verifyArguments = @(
    'run', '--rm',
    '--mount', "type=bind,source=$opaRuntimeRoot,target=/runtime,readonly",
    $opaImage,
    'build', '--bundle', '/runtime/bundle.tar.gz',
    '--output', '/tmp/verified-bundle.tar.gz',
    '--verification-key', '/runtime/public.pem'
)
& docker @verifyArguments
if ($LASTEXITCODE -ne 0) { throw 'Signed OPA bundle verification failed.' }

if ($StartInfrastructure)
{
    & docker compose -f $composePath up -d
    if ($LASTEXITCODE -ne 0) { throw 'Stage 02 infrastructure startup failed.' }
}

Write-Output 'Stage 02 runtime material is prepared outside Git.'
Write-Output "OPA bundle: $bundlePath"
Write-Output 'Set PostgreSqlIntentRegistration__ConnectionString with the runtime password and the generated localhost.pem path before starting the API.'
