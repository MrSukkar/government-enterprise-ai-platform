# Integration Demo Runtime

This directory contains versioned, non-secret configuration for the local Integration Demo. It is not a production deployment profile.

## Stage 05 topology

| Service | Address | Purpose |
|---|---|---|
| Keycloak | `https://localhost:8443` | Synthetic OIDC authority |
| OPA | `https://localhost:8181` | Verified signed policy bundle, Intent registration, and Enterprise Context decision |
| PostgreSQL | `localhost:5433` | TLS atomic Intent and Enterprise Context evidence persistence |
| Neo4j | `neo4j+s://localhost:7687` | TLS synthetic, read-only Enterprise Graph |
| Platform API | `https://localhost:7200` | Protected governed boundaries and developer portal |
| Platform Web | `https://localhost:7071` | Blazor WebAssembly demonstration UI |

## Security boundary

- `keycloak/geaip-demo-realm.json` contains only public-client and synthetic-claim configuration.
- The browser client uses Authorization Code with PKCE and has no client secret.
- User passwords, administrator passwords, tokens, certificate passwords, private keys, exported certificates, and runtime logs must remain outside Git.
- The realm is bound to the exact frontend HTTPS redirect URI and web origin.
- The API accepts only tokens with the configured issuer and `platform-api` audience.

## Local prerequisites

1. Docker Desktop with Linux containers.
2. .NET 10 SDK.
3. A trusted ASP.NET Core localhost development certificate.
4. Local runtime secrets supplied outside the repository.

Set `GEAIP_DEMO_POSTGRES_PASSWORD` and `GEAIP_DEMO_NEO4J_PASSWORD` to local runtime secrets, then run `scripts/prepare-integration-demo-stage-05.ps1 -StartInfrastructure`. The script creates ignored localhost TLS and OPA signing material, builds the signed bundle, starts only the pinned Stage 05 services, and seeds synthetic graph fixtures. PostgreSQL migrations are initializer-owned; the API never performs migrations.

The current accepted presenter workflow is documented in `docs/demo/NATIONAL_PERMIT_RENEWAL_SCENARIO.md`. Stage 05 acceptance evidence is recorded in `docs/demo/STAGE_05_EXISTING_ARCHITECTURE_ACCEPTANCE.md`.

## Runtime behavior

The demo-specific API settings load only when `ASPNETCORE_ENVIRONMENT=IntegrationDemo`. Normal repository defaults remain unconfigured and fail closed. Stopping Keycloak, OPA, PostgreSQL, or Neo4j, presenting an invalid token, failing signed-bundle verification, or receiving policy denial prevents registration or context release. Authorized context remains read-only and non-advancing.

Do not reuse this realm, its synthetic identities, or localhost certificates for Pilot or Production. Those environments require institutionally selected identity, trust, secret-management, and deployment profiles.
