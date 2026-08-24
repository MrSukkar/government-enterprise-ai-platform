# Phase 06 — OpenAPI Contract

Status: **Implemented**  
Depends on: **Phase 05 — Identity & Access**

## Contract authority

`backend/Platform.Api/Contracts/openapi.v1.json` is the machine-readable OpenAPI 3.1 contract for the accepted API surface. It is embedded in the API assembly and served unchanged from `/openapi/v1.json`, so the reviewed source and runtime artifact cannot silently diverge.

## Boundary rules

1. Business endpoints use a major-version path prefix: `/api/v{major}/...`.
2. Every operation has a stable, unique `operationId`.
3. Authenticated access is the contract default; anonymous operations must explicitly set an empty security requirement.
4. Errors use Problem Details and may disclose only context-safe trace identifiers.
5. Request and response schemas are explicit; undocumented data is not an API contract.
6. An endpoint is added only in its approved capability phase.
7. Breaking changes require a new major contract and Change Control where architectural impact exists.
8. The contract does not replace server-side identity, purpose, policy, tenant, classification, or resource authorization.

## Current surface

- `GET /health` — anonymous technical liveness.
- `GET /openapi/v1.json` — anonymous approved contract discovery.

No business operation is introduced in this phase.

