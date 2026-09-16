# CR-023 — Integration Demo Governed Intent

Status: **Approved by repository owner on 2026-09-13**

## Request

Enable the first Integration Demo station for the approved **Create Internal Service** product so a synthetic Developer can authenticate and submit a governed intent through the browser to the existing protected API.

## Approved selections

- Keycloak `26.7.3`, pinned to the official container-image digest, is the OIDC provider for the local Integration Demo only.
- `Microsoft.AspNetCore.Components.WebAssembly.Authentication` `10.0.11` is the approved Blazor WebAssembly OIDC client package.
- Authorization Code with PKCE is required; the browser client is public and has no client secret.
- All demo browser, API, and authority endpoints use trusted localhost HTTPS.
- The only demo persona in this increment is a synthetic Developer scoped to tenant `demo-permit-authority`, purpose `internal-service-delivery-demonstration`, clearance `Internal`, and permission `developer.internal-service.create`.
- A protected read-only identity-context endpoint may translate the already validated bearer principal into the existing `GovernedExperienceContext`. It grants no new permission and performs no institutional mutation.
- Exact-origin CORS may be enabled for the local Integration Demo frontend only.

## Boundary

This change enables authenticated Intent **validation** only. It creates no registration, database mutation, OPA decision, workflow advancement, Enterprise Context access, AI invocation, code generation, sandbox execution, Git change, deployment, or production effect.

OPA and PostgreSQL Intent registration remain the next separately verified step inside the same Governed Intent station. Production identity-provider selection remains an institutional deployment decision.

## Security conditions

- Repository defaults remain unconfigured and fail closed.
- No user password, administrator credential, token, private key, exported certificate, or connection string is committed.
- Redirect URIs and CORS origins are exact localhost HTTPS values.
- Token issuer, audience, lifetime, signature, and governed claims remain validated by the existing backend identity boundary.
- Missing, expired, malformed, incorrectly scoped, or insufficient tokens are rejected.
- The frontend never treats navigation visibility as API authorization.

## Acceptance

- A synthetic Developer can complete OIDC sign-in and sign-out.
- The frontend receives only a server-established governed experience context.
- The existing Intent submission endpoint returns a deterministic validation receipt for an authorized exact request.
- Anonymous access receives a bearer challenge and mismatched authorization evidence is denied.
- No Intent is persisted by validation.
- Repository-default verification remains unchanged and successful.
