# Integration Demo Runtime

This directory contains versioned, non-secret configuration for the local Integration Demo. It is not a production deployment profile.

## Stage 01 topology

| Service | Address | Purpose |
|---|---|---|
| Keycloak | `https://localhost:8443` | Synthetic OIDC authority |
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

The current accepted presenter workflow is documented in `docs/demo/NATIONAL_PERMIT_RENEWAL_SCENARIO.md`. Stage 01 acceptance evidence is recorded in `docs/demo/STAGE_01_GOVERNED_INTENT_ACCEPTANCE.md`.

## Runtime behavior

The demo-specific API settings load only when `ASPNETCORE_ENVIRONMENT=IntegrationDemo`. Normal repository defaults remain unconfigured and fail closed. Stopping Keycloak or presenting an invalid token prevents governed context establishment and Intent submission.

Do not reuse this realm, its synthetic identities, or localhost certificates for Pilot or Production. Those environments require institutionally selected identity, trust, secret-management, and deployment profiles.
