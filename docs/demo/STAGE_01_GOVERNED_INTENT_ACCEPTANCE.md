# Integration Demo Stage 01 — Governed Intent Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-023-INTEGRATION-DEMO-GOVERNED-INTENT.md`

## Accepted scope

Stage 01 connects a synthetic browser identity to the existing protected Governed Intent validation boundary. It is intentionally limited to authentication, server-established experience context, and non-persisting validation.

## Runtime topology

| Boundary | Local endpoint | Control |
|---|---|---|
| Blazor WebAssembly | `https://localhost:7071` | Public OIDC client; Authorization Code with PKCE |
| ASP.NET Core API | `https://localhost:7200` | JWT issuer, audience, lifetime, signature, and governed-claim validation |
| Keycloak | `https://localhost:8443` | Synthetic `geaip-demo` realm; no repository-stored credentials |

The Keycloak image is pinned by CR-023. Certificates, administrator credentials, user passwords, tokens, and logs remain ignored local runtime material.

## Verification record

| Check | Result |
|---|---|
| Frontend loads without a Blazor runtime error | Pass |
| Authentication JavaScript is served from the pinned Microsoft package | Pass |
| Anonymous `GET /api/v1/identity/context` | Pass — `401` |
| OpenAPI 3.1 documents the protected identity-context boundary | Pass |
| Realm and OpenAPI JSON parse successfully | Pass |
| Repository verification | Pass — 15 projects, 0 warnings, 0 errors |
| Repository-default runtime posture | Pass — all 153 dependencies fail closed |
| Interactive OIDC sign-in | Pass — Authorization Code with PKCE completed for the synthetic Developer |
| Server-established governed context | Pass — the corrected access token includes `sub`; authenticated context returned `200` with the exact tenant, purpose, permission, and authorization evidence |
| Authorized Intent validation receipt | Pass — two exact authenticated submissions returned `200`, `validated-not-persisted`, `isPersisted: false`, `canExecute: false`, and the same SHA-256 digest |
| Sign-out relocks submission | Pass — logout returned the browser to the locked shell and removed submission authority |

The runtime verification used a disposable synthetic probe identity only to repeat the authenticated API assertions after the browser-tool policy stopped further localhost navigation. The identity was deleted immediately after the check, and Direct Access Grants were restored to disabled. No test identity, password, token, certificate, or runtime log is retained in Git.

## Required receipt assertions

The interactive success path is accepted only when the returned receipt has all of the following properties:

- the response subject and tenant originate from the validated bearer principal;
- `status` is `validated-not-persisted`;
- `isPersisted` is `false`;
- the intent digest is deterministic for the exact normalized request;
- no OPA decision, database mutation, workflow advancement, AI invocation, or production effect occurs.

## Next gate

Stage 02 requires a separate approved change to connect a signed OPA policy bundle and PostgreSQL atomic Intent registration. Stage 01 does not authorize those dependencies.
