# CR-048 — V3-03 Governed API Lifecycle and Catalog

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V3.md`; predecessor acceptance: `docs/v3/V3_02_CONSUMER_CHANNEL_LIFECYCLE_ACCEPTANCE.md`.

## Scope

Add product-neutral API publication and lifecycle contracts, strict OpenAPI 3.1 validation, deterministic document and request fingerprints, exact OPA binding before mutation, atomic catalog-plus-Evidence persistence boundaries, and independent result-release authorization.

## Hard gates

- **Security:** strict validation and exact OPA authorization occur before mutation; publication results require independent authorization before release.
- **Compliance:** identity, tenant, purpose, environment, contract version, document digest, operations, policy, lifecycle, and Evidence remain bound.
- **Sovereignty:** validation is local and deterministic; no provider, external control plane, credential, trust material, or live adapter is selected.
- **HA/DR:** optimistic versioning, atomic Evidence, immutable digests, and forward-only lifecycle are explicit; no numerical objective is invented.

## Boundary

Only inert contracts, ports, deterministic validation, and orchestration are added. Repository defaults provide no policy, catalog, or release adapter and remain unavailable and fail closed. No endpoint, gateway runtime, named product, institutional data, public deployment, Production effect, AI authority, Phase 31, credential, or trust material is introduced.

## Acceptance

Verify valid OpenAPI 3.1 publication; reject incompatible version, duplicate operation identifiers, OPA denial, release denial, and lifecycle rollback; run the official verifier and full CI.
