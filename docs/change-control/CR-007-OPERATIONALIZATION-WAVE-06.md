# CR-007 — Operationalization Wave 06: Governed Approved Packages Runtime

Status: **Approved for Operationalization Wave 06**

Preceding completed authority: `docs/change-control/CR-006-OPERATIONALIZATION-WAVE-05.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Operationalize only the Approved Packages station of the approved Create Internal Service path by connecting it to the exact evidence-bearing Existing Architecture snapshot produced by Wave 05, the sovereign signed-bundle/OPA boundary, a PostgreSQL-backed read-only institutional package catalog, deterministic eligibility, cryptographically verified supply-chain attestations, per-package RBAC/ABAC re-authorization, and immutable PostgreSQL selection evidence.

Wave name: **Operationalization Wave 06 — Governed Approved Packages Runtime**.

### Outcome

When—and only when—deployment-approved identity, policy, PostgreSQL, package-attestation trust, and evidence profiles are complete, an authorized caller can request an explicit set of exact immutable package coordinates. The platform revalidates the authoritative architecture snapshot, obtains an exact action-specific OPA scope before catalog access, reads only the authorized coordinates, verifies current institutional approval and sovereign availability, validates signed digest-bound provenance/SBOM/registry attestations without transferring package content, re-authorizes every package, and atomically records deterministic selection evidence.

Any missing configuration, prerequisite mismatch, policy denial, non-exact coordinate, absent or substituted catalog record, expired approval, tenant/environment mismatch, unavailable sovereign copy, invalid signature or attestation binding, authorization rejection, malformed data, or persistence ambiguity fails closed and releases no partial selection.

## 2. Current-state evidence

- All 30 phases, CR-001 Operational Increments 01–22, and Operationalization Waves 01–05 are complete and verified.
- Wave 05 atomically persists the authoritative Existing Architecture evidence record but deliberately leaves `IAuthorizedExistingArchitectureSnapshotReader` disconnected.
- `IApprovedPackagesPolicyGate`, `IInstitutionalPackageRegistryReader`, `IApprovedPackageSupplyChainVerifier`, `IApprovedPackageResultAuthorizer`, and `IApprovedPackagesEvidenceRecorder` remain disconnected.
- `GovernedApprovedPackagesSelectionEngine` already enforces prerequisite binding, exact immutable coordinates, policy-before-registry ordering, current institutional eligibility, supply-chain assurance, per-result authorization, deterministic hashing, evidence receipt validation, and `CanAdvance: false`.
- PostgreSQL is the approved primary transactional and evidence database. No package-registry vendor, public registry, transfer path, or new package is required.
- Repository defaults expose 142 disconnected institutional dependencies and remain fail closed.

## 3. Impact Analysis

### In scope

- Implement `IAuthorizedExistingArchitectureSnapshotReader` over the Wave 05 PostgreSQL evidence table using exact tenant/discovery/purpose binding, fixed parameterized SQL, bounded commands, stored JSON SHA-256 verification, evidence-reference verification, and architecture digest recomputation.
- Extend the sovereign typed OPA response envelope with an action-specific Approved Packages scope containing exact immutable package coordinates, classification ceiling, required roles, and exact maximum results. Every other OPA action adapter rejects this scope.
- Implement `IApprovedPackagesPolicyGate` for the exact action `internal-service.approved-packages.select`, with signed-bundle verification before OPA and exact revalidation of request, prerequisite digests, environment, decision, bundle, scope, evidence, and time.
- Add a deployment-controlled PostgreSQL institutional package catalog migration and implement `IInstitutionalPackageRegistryReader` as an exact-coordinate, tenant/environment-aware, read-only adapter. The runtime performs no catalog DDL, mutation, discovery, search, ranking, dependency resolution, or package transfer.
- Extend the institutional package record with a typed immutable supply-chain attestation containing the exact coordinate digest, provenance digest, SBOM digest, sovereign-registry digest, signature envelope, issued time, expiry, and evidence references.
- Implement `IApprovedPackageSupplyChainVerifier` with built-in .NET cryptography against deployment-pinned public-key trust configuration. Verification covers the canonical attestation digest and exact package coordinate/provenance/SBOM/sovereign-registry bindings; it never downloads or executes package content.
- Implement `IApprovedPackageResultAuthorizer` through the existing deterministic RBAC/ABAC evaluator using the authenticated identity and exact OPA-required roles and coordinate scope.
- Add an explicit PostgreSQL migration and `IApprovedPackagesEvidenceRecorder` for immutable, idempotent, tenant-scoped selection snapshots and SHA-256-qualified evidence references. Writes use explicit transactions and never retry an ambiguous mutation.
- Register the six runtime contracts only under complete valid policy, PostgreSQL, and package-attestation trust configuration. `IAuthorizedApprovedPackagesSnapshotReader` remains disconnected.
- Add non-sensitive readiness, OpenAPI/UI wording, operational guidance, acceptance evidence, deterministic verifier checks, and atomic project-state updates after successful verification.

### Out of scope

- Public NuGet/npm/container/model registry calls; registry credentials; package download, restore, install, unpack, mount, mirror, cache, copy, execution, publication, deletion, or dependency resolution.
- Package discovery without exact coordinates, recommendation, ranking, substitution, version ranges, floating tags, aliases, transitive dependency selection, or lock-file generation.
- Institutional catalog or approval mutation, package approval/revocation, license-policy creation, legal decision, vulnerability-risk acceptance, trust-key selection, certificate issuance, SBOM/provenance generation, or attestation issuance.
- Caller assertions, repository files, local caches, generated locks, placeholder metadata, unsigned records, or readable public metadata becoming institutional authority.
- A new database, registry vendor SDK, package manager SDK, cloud service, SaaS control plane, project, service boundary, queue, cache, or package dependency.
- AI Planning, Code Generation, later stations, workflow advancement, AI invocation, external effect, or production action.
- External provisioning, live infrastructure/schema application, institutional data import, credentials, trust keys, public deployment, or numerical SLO.

### Affected boundaries

- `Platform.SoftwareFactory/InternalService` owns orchestration, prerequisite verification, action-specific policy, supply-chain assurance, result authorization, and evidence semantics.
- `Platform.SoftwareFactory/Packages` remains the immutable package-coordinate, institutional catalog, approval-history, sovereign-copy, and eligibility boundary.
- `Platform.Governance` owns only the bounded typed policy transport extension; OPA remains policy authority.
- `Platform.Identity` remains RBAC/ABAC authority and does not process registry credentials.
- `Platform.Api` conditionally composes valid adapters and exposes non-sensitive readiness.
- PostgreSQL stores deployment-governed package metadata and immutable selection evidence; it does not store, resolve, or execute package binaries.

## 4. Data and security review

1. Exact Existing Architecture JSON, record digest, evidence reference, architecture digest, prerequisite identities, tenant, purpose, evidence, and time are revalidated before OPA.
2. Signed-bundle verification and exact OPA coordinate scope precede every catalog read; denial carries no operational scope.
3. Catalog SQL is fixed, parameterized, bounded, tenant/environment scoped, and read only.
4. Only exact versions with algorithm-qualified immutable content digests are accepted. One package cannot authorize another coordinate, version, kind, or digest.
5. Current `Approved`, unexpired, evidence-bearing institutional approval and sovereign availability are mandatory.
6. Supply-chain verification uses canonical digest-bound signed attestations and deployment-pinned trust; references alone never establish assurance.
7. Package bytes, credentials, commands, live sessions, remote calls, public registries, and external effects are absent from the runtime path.
8. Every selected package is independently RBAC/ABAC-authorized with authenticated identity, required roles, exact coordinate, tenant, purpose, environment, and classification.
9. Selection evidence is deterministic, append-only, tenant scoped, idempotent, transactionally committed, and cryptographically addressable.
10. Connection values, trust material, raw policy inputs, catalog internals, and unauthorized records are not logged or exposed through readiness or API output.

## 5. Proposed implementation sequence

1. Add the cryptographically validating PostgreSQL Existing Architecture snapshot reader.
2. Add the distinct typed Approved Packages OPA scope and action-specific gate.
3. Add typed signed supply-chain attestation data and deployment-pinned trust options.
4. Add the read-only exact-coordinate PostgreSQL institutional catalog adapter.
5. Implement cryptographic supply-chain verification and deterministic RBAC/ABAC result authorization.
6. Add immutable Approved Packages evidence migration and recorder.
7. Compose the six contracts only under complete valid configuration.
8. Update readiness, OpenAPI, UI, operational documentation, acceptance verification, and project state.
9. Run the complete verifier, then commit and synchronize only verified source.

## 6. Architectural Review

Finding: **Conforms without architectural deviation, subject to explicit approval and implementation verification**.

- PostgreSQL, .NET cryptography, OPA, RBAC/ABAC, evidence, and the modular-monolith boundaries are already approved.
- The Enterprise Model and the released architecture snapshot remain contextual authority; the package catalog cannot change architecture or policy.
- No concrete public registry, new package, external control plane, package transfer, AI call, or workflow authority is introduced.
- Air-gapped operation is preserved through deployment-supplied local PostgreSQL, OPA, identity, PKI/trust, and institutional metadata.
- `AI -> Production` remains impossible.

## 7. Package decision

No new package is requested. Wave 06 reuses locked `Npgsql` `10.0.3` and built-in .NET cryptography. No NuGet/npm/container/model registry SDK, dependency resolver, vulnerability service, ORM, or cloud SDK is proposed.

## 8. Decision requested

Approve **CR-007 — Operationalization Wave 06: Governed Approved Packages Runtime** for bounded implementation, local verification, source control, and GitHub synchronization only.

Decision: **Approved by the repository owner on 2026-09-08 for bounded Wave 06 implementation, local verification, source control, and GitHub synchronization.**

Approval would not authorize external credentials, live schema application, institutional catalog import, trust-key selection, public registry access, package transfer/execution, approval mutation, AI Planning, public deployment, or production action.

## Acceptance gate

1. Repository-default and incomplete profiles remain unconfigured and fail closed; the disconnected-dependency count remains 142.
2. The exact tenant-scoped Existing Architecture record and cryptographic/storage/payload bindings are revalidated before OPA.
3. Signed bundle verification and exact action-specific OPA scope precede catalog reads; denial carries no scope.
4. Catalog access is exact-coordinate, fixed, parameterized, bounded, tenant/environment scoped, and read only.
5. Only current approved, unexpired, sovereign, evidence-bearing exact records are eligible.
6. Canonical supply-chain attestations cryptographically bind exact coordinate/content, provenance, SBOM, sovereign registry, issuer, time, and evidence to deployment-pinned trust without package transfer.
7. Every package is re-authorized using authenticated identity and OPA-bound roles/scope before release.
8. Missing, duplicate, substituted, unexpected, invalid, expired, unsigned, untrusted, out-of-scope, or assurance-failing data releases no partial selection.
9. Selection evidence is deterministic, immutable, tenant scoped, idempotent, atomic, and SHA-256 qualified.
10. No secret, connection detail, trust material, raw policy input, unauthorized catalog record, package byte, command, session, mutation, or external effect is disclosed or committed.
11. `IAuthorizedApprovedPackagesSnapshotReader`, AI Planning, Code Generation, all later stations, package transfer, AI invocation, workflow advancement, and production action remain unavailable.
12. All preceding gates remain satisfied; all 15 projects build with zero warnings and zero errors; runtime verification proves the safe default posture.
