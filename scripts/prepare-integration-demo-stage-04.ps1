param([switch]$StartInfrastructure)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$runtime=Join-Path $root '.runtime\integration-demo-stage-04'
$tls=Join-Path $runtime 'tls'; $opaRuntime=Join-Path $runtime 'opa'
$opaSource=Join-Path $root 'deploy\demo\stage-04\opa'
$compose=Join-Path $root 'deploy\demo\stage-04\compose.yml'
$seed=Join-Path $root 'deploy\demo\stage-04\neo4j\seed.cypher'
$opaImage='openpolicyagent/opa:1.20.2-static@sha256:bb245e9e36be0d0ed486c240b606c56be7aba96014a4a87895fed4ba7a6dfa8d'
$helperImage='postgres:18.6-bookworm@sha256:1c59e2c3c818eaa0f0628f695b36e7c9e362d6b219b36a54a32df645cbd7e1af'
if([string]::IsNullOrWhiteSpace($env:GEAIP_DEMO_POSTGRES_PASSWORD)-or $env:GEAIP_DEMO_POSTGRES_PASSWORD.Length -lt 16){throw 'GEAIP_DEMO_POSTGRES_PASSWORD must be supplied outside Git and contain at least 16 characters.'}
if([string]::IsNullOrWhiteSpace($env:GEAIP_DEMO_NEO4J_PASSWORD)-or $env:GEAIP_DEMO_NEO4J_PASSWORD.Length -lt 16){throw 'GEAIP_DEMO_NEO4J_PASSWORD must be supplied outside Git and contain at least 16 characters.'}
New-Item -ItemType Directory -Force $tls,$opaRuntime|Out-Null
& dotnet dev-certs https --trust --export-path (Join-Path $tls 'localhost.pem') --format Pem --no-password
if($LASTEXITCODE -ne 0 -or -not(Test-Path (Join-Path $tls 'localhost.key'))){throw 'Trusted localhost PEM certificate export failed.'}
if(-not(Test-Path (Join-Path $opaRuntime 'private.pem'))){& docker run --rm --mount "type=bind,source=$opaRuntime,target=/keys" $helperImage bash -ec 'openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072 -out /keys/private.pem && openssl pkey -in /keys/private.pem -pubout -out /keys/public.pem';if($LASTEXITCODE -ne 0){throw 'OPA signing-key generation failed.'}}
& docker run --rm --mount "type=bind,source=$opaRuntime,target=/keys" $helperImage bash -ec 'chown -R 1000:1000 /keys && chmod 600 /keys/private.pem && chmod 644 /keys/public.pem';if($LASTEXITCODE -ne 0){throw 'OPA signing-key permission preparation failed.'}
& docker run --rm --mount "type=bind,source=$opaSource,target=/bundle-source,readonly" --mount "type=bind,source=$opaRuntime,target=/runtime" $opaImage build --bundle /bundle-source --output /runtime/bundle.tar.gz --revision geaip-demo-stage-04-2026.09.18 --signing-key /runtime/private.pem --signing-alg RS256;if($LASTEXITCODE -ne 0){throw 'Signed OPA bundle creation failed.'}
& docker run --rm --mount "type=bind,source=$opaRuntime,target=/runtime,readonly" $opaImage build --bundle /runtime/bundle.tar.gz --output /tmp/verified.tar.gz --verification-key /runtime/public.pem;if($LASTEXITCODE -ne 0){throw 'Signed OPA bundle verification failed.'}
if($StartInfrastructure){& docker compose -f $compose up -d;if($LASTEXITCODE -ne 0){throw 'Stage 04 infrastructure startup failed.'};$ok=$false;foreach($attempt in 1..30){Start-Sleep -Seconds 2;Get-Content -Raw $seed|docker compose -f $compose exec -T neo4j cypher-shell -a neo4j+ssc://localhost:7687 -u neo4j -p $env:GEAIP_DEMO_NEO4J_PASSWORD --format plain;if($LASTEXITCODE -eq 0){$ok=$true;break}};if(-not $ok){throw 'Synthetic Enterprise Graph seeding failed.'}}
Write-Output 'Stage 04 runtime material is prepared outside Git.'
