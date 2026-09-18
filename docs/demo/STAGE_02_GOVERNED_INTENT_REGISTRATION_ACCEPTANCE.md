# Integration Demo Stage 02 — Governed Intent Registration Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-024-INTEGRATION-DEMO-INTENT-REGISTRATION.md`

Preceding gate: `docs/demo/STAGE_01_GOVERNED_INTENT_ACCEPTANCE.md`

## Accepted scope

Stage 02 connects the authenticated and validated synthetic Intent to an exact signed OPA bundle and atomic PostgreSQL persistence. It does not advance workflow or authorize Enterprise Context discovery.

## Runtime topology

| Boundary | Local endpoint | Accepted control |
|---|---|---|
| Keycloak | `https://localhost:8443` | Exact issuer, audience, tenant, clearance, permission, and authorization evidence |
| OPA 1.20.2 | `https://localhost:8181` | Pinned image, TLS, verified signed bundle, exact bundle identity and digest |
| PostgreSQL 18.6 | `localhost:5433` | Pinned image, TLS `VerifyFull`, initializer-owned migration, atomic registration |
| Platform API | `https://localhost:7200` | Local RBAC/ABAC before OPA; persistence only after permit |
| Platform Web | `https://localhost:7071` | Registration action and immutable receipt display |

Runtime certificates, private signing keys, passwords, tokens, logs, generated bundles, and database volumes remain outside Git under ignored runtime storage.

## Verification record

| Check | Result |
|---|---|
| Signed bundle build and verification | Pass — exact bundle digest `041941cce6753212eb98bd3f0ef73b0772d14df0be6a6dea141c18b6041ac4bd` |
| OPA permit through the standard `/v1/data` envelope | Pass — exact tenant, purpose, classification, environment, action, bundle, and signature binding |
| First authorized registration | Pass — HTTP `201`, `Created`, persisted version `0`, `canAdvance: false` |
| Exact authorized replay | Pass — HTTP `200`, `Unchanged`, original immutable registration evidence returned |
| Policy denial | Pass — HTTP `403`, `Deny`, `isPersisted: false` |
| Database mutation count | Pass — one row after create; replay and denial produced no additional row |
| Repository-default posture | Pass — OPA, PostgreSQL, and all other adapters remain unconfigured and fail closed outside `IntegrationDemo` |
| Solution build | Pass — all 15 projects, 0 warnings, 0 errors |

The live proof used only a disposable synthetic identity and synthetic permit-renewal data. It demonstrated that renewed policy authorization may create fresh decision evidence while idempotency remains bound to the immutable Intent and signed bundle.

## Required boundary assertions

- Local identity authorization and signed-bundle verification precede persistence.
- OPA denial cannot create or alter a registration.
- Intent and registration evidence commit atomically.
- Exact replay returns the unchanged stored record; immutable-field mismatch remains a conflict.
- `canAdvance` remains `false`; Stage 03 is a separate approval gate.
- No AI invocation, production effect, workflow advancement, new service boundary, architectural deviation, or Phase 31 exists.

## Next gate

Stage 03 is Authorized Enterprise Context discovery and requires separate change approval. Stage 02 does not authorize that adapter or any later station.
