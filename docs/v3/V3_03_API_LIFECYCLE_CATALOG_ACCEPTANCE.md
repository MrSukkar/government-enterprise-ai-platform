# V3-03 — Governed API Lifecycle and Catalog Acceptance

Status: **Satisfied**

Change Control: `docs/change-control/CR-048-V3-03-API-LIFECYCLE-CATALOG.md`.

## Accepted scope

- Publication begins only from a validated Proposed API constitutional contract and an exact OpenAPI 3.1 document.
- Validation requires API metadata, paths, operations, and unique operation identifiers and binds the document SHA-256 digest.
- Exact action, request fingerprint, document digest, tenant, purpose, and environment are authorized before persistence.
- Catalog mutation and Evidence share one atomic repository boundary; release is independently authorized afterward.
- Lifecycle permits only Proposed→Published, Published→Deprecated, and Deprecated→Retired.
- Invalid documents, OPA denial, release denial, and backward lifecycle transitions fail closed.
- No configured adapter, endpoint, product, external control plane, institutional data, credential, trust material, Production action, Phase 31, or AI authority exists.

## Verification record

- Contract verifier: valid publication accepted; incompatible OpenAPI, duplicate operation identifiers, OPA denial, release denial, and lifecycle rollback rejected.
- Official verifier: passed on 2026-09-24.
- Build: 15 projects, 0 warnings, 0 errors.
- Repository-default runtime: all 153 dependencies remain unconfigured and fail closed.

## Decision

V3-04 remains prohibited until V3-03 is merged fast-forward and final CI on `main` is green.
