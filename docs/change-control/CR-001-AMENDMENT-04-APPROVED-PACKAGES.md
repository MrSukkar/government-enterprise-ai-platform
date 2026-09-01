# CR-001 Amendment 04 — Approved Packages Boundary

Status: **Approved for Operational Increment 07**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-03-EXISTING-ARCHITECTURE.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next station in the approved vertical slice after Authorized Existing Architecture Discovery:

`Existing Architecture -> Approved Packages`

Approved increment name: **Operational Increment 07 — Governed Approved Packages Selection**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can submit an explicit set of exact, immutable package coordinates for a previously released Existing Architecture snapshot and receive a deterministic, evidence-bearing selection of the coordinates that satisfy institutional approval, tenant, environment, sovereign-registry, provenance, SBOM, signature, policy, and supply-chain controls. OPA must authorize the exact candidate scope before any institutional package-registry read. Every returned record is structurally validated, evaluated by the existing package-eligibility rules, supply-chain verified, and re-authorized before release. The result cannot download, restore, install, resolve, execute, or publish a package and cannot advance to AI Planning, code generation, workflow execution, or production.

## 2. Impact Analysis

### In scope

- A protected REST contract for Approved Packages selection from a previously released, evidence-bearing Existing Architecture snapshot.
- Revalidation of authenticated subject, tenant, purpose, classification clearance, explicit package-selection permission, authorization evidence, governed-intent registration identity and version, intent digest, Enterprise Context identity and digest, Existing Systems identity and digest, Existing Architecture identity and digest, and all prerequisite evidence.
- A deployment-controlled read boundary for the authoritative Existing Architecture snapshot; caller-supplied architecture content is not authoritative.
- Caller-proposed package candidates limited to exact `PackageCoordinate` values: package kind, name, exact version, and algorithm-qualified immutable content digest. Version ranges, floating tags, aliases, and digest-free candidates are invalid.
- A signed, environment-aware OPA policy reference and evidence-bearing decision defining a non-empty explicit set of allowed exact package coordinates, package kinds, tenant, purpose, environment, classification, and result-count scope before any package-registry read.
- A read-only, deployment-controlled institutional package-registry boundary that looks up only exact OPA-authorized coordinates. The selection operation cannot invoke registry mutation.
- Coverage of only the package kinds already approved by Phase 09: NuGet, frontend dependency, container image, AI model, policy bundle, and sandbox image.
- Structural revalidation of each returned institutional package record, including exact coordinate, provenance, publisher, license expression, tenant scope, environment scope, approval history, sovereign-registry availability, SBOM reference, signature reference, and decision timestamps.
- Use of the existing `IPackageEligibilityEvaluator` rules so coordinate mismatch, tenant denial, environment denial, missing sovereign copy, non-approved status, suspension, revocation, rejection, pending approval, or expired approval fails closed.
- A deployment-controlled supply-chain assurance boundary that verifies the exact content digest and evidence references for provenance, SBOM, signature, and sovereign registry without downloading or executing package content.
- A requirement that the current institutional approval is `Approved`, unexpired, evidence-bearing, and structurally bound to the selected package record.
- Per-package authorization re-check after eligibility and supply-chain verification and before inclusion in the returned package-selection snapshot.
- Rejection of duplicate coordinates, registry mismatches, unexpected records, incomplete assurance, out-of-scope records, or partial substitution; one exact requested coordinate cannot authorize another version or digest.
- Deterministic ordering, a SHA-256 package-selection digest, and a cryptographic evidence receipt bound to the governed intent, Enterprise Context, Existing Systems, Existing Architecture, OPA decision, eligibility decisions, and supply-chain verification evidence.
- Explicit `CanAdvance: false`; AI Planning remains a separately governed future station.
- OpenAPI 3.1, Blazor boundary communication, readiness visibility, acceptance verification, and runtime non-regression checks.
- Fail-closed behavior when Existing Architecture snapshot read, signed OPA policy, institutional registry read, eligibility evaluation, supply-chain assurance, per-result authorization, or required evidence dependencies are unavailable.

### Out of scope

- Approval or implementation of AI Planning, Code Generation, or any later vertical-slice station.
- Package discovery without exact coordinates, recommendation, ranking, substitution, version-range resolution, floating-version resolution, transitive-dependency resolution, lock-file generation, or automatic package choice.
- Downloading, restoring, installing, unpacking, mounting, loading, importing, executing, publishing, mirroring, copying, caching, or deleting any package or artifact.
- Calls to public package registries, public container registries, model hubs, SaaS catalogs, external license services, vulnerability services, or non-sovereign control planes.
- Creation, update, approval, rejection, suspension, revocation, expiration change, or deletion of an institutional package record or approval.
- Treating caller input, repository dependency declarations, local caches, generated lock files, or readable package metadata as institutional approval.
- License-policy creation, legal determination, vulnerability-risk acceptance, cryptographic-key choice, signature issuance, SBOM generation, attestation issuance, or registry trust-policy approval.
- Live dependency scanning, binary analysis, source-code crawling, credential use, secret retrieval, live session use, command execution, network access, or external effects.
- Enterprise Model, Existing Systems, Existing Architecture, package-registry, policy, evidence, source-control, or institutional-state mutation.
- AI invocation, prompt construction, model-context release, code generation, workflow advancement, sandbox execution, or material action.
- A concrete PostgreSQL provider, package-registry provider, NuGet provider, npm provider, container-registry provider, model-registry provider, identity adapter, OPA adapter, evidence-store adapter, external credential, or mandatory external dependency.
- Fake or in-memory production adapters, sample institutional packages, synthetic permits, fabricated approvals, placeholder provenance, placeholder SBOMs, placeholder signatures, or placeholder evidence accepted as authoritative.
- A new database, registry technology, package, .NET project, service boundary, microservice, package kind, dependency resolver, or conditional technology approval.
- A numerical SLO, package-count limit, timeout, retry count, vulnerability threshold, risk score, license allowlist, or performance target before workload benchmarking and institutional policy configuration.
- Changes to the fixed 30-phase roadmap, the Master Specification, or any constitutional invariant before this amendment is approved through Change Control.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: owns Create Internal Service orchestration and binds package selection to the governed intent, Enterprise Context, Existing Systems, and Existing Architecture snapshots.
- `Platform.SoftwareFactory/Packages`: remains the institutional package registry, immutable coordinate, approval-history, sovereign-copy, and eligibility authority. The selection path is read-only and cannot call registry save operations.
- `Platform.SoftwareFactory/SupplyChain`: supplies vendor-neutral verification of provenance, SBOM, signature, digest, and sovereign registry evidence; it does not download, execute, or approve packages.
- `Platform.Identity`: supplies the already-authenticated governed request context and access checks; it does not parse credentials in this increment.
- `Platform.Governance`: remains the signed-policy verification and OPA authority.
- `Platform.EnterpriseModel`: remains the contextual source of truth for the service scope without mutation.
- `Platform.Api`: composes the protected REST boundary without moving package, approval, policy, supply-chain, or evidence authority into the API layer.
- `Platform.Web`: communicates package-selection availability and fail-closed state without claiming package-registry connectivity or approval authority.
- `Platform.Evidence`: remains the cryptographic evidence authority; no fabricated evidence implementation is introduced.
- OpenAPI and verification scripts: record and prove the protected contract, exact-coordinate scope, ordering, and non-regression boundaries.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity provider configuration.
- A sovereign signed-policy verifier and OPA evaluation adapter.
- A deployment-approved authorized Existing Architecture snapshot reader.
- A read-only adapter to the institutionally governed package registry containing exact immutable coordinates and current approval history.
- Deployment-approved provenance, SBOM, signature, digest, and sovereign-registry assurance verifiers.
- A policy-authorized per-package result evaluator.
- A sovereign cryptographic evidence implementation when authoritative evidence persistence is required.

Until these dependencies are configured, Approved Packages selection must return unavailable or denied without registry access, package transfer, institutional mutation, or advancement.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- The Enterprise Model remains the contextual source of truth; package selection is bound to the authorized architecture snapshot and cannot mutate institutional state.
- PostgreSQL remains the primary database baseline; no concrete package-registry persistence adapter or schema is introduced.
- The existing institutional package model and eligibility evaluator remain authoritative for exact-coordinate, tenant, environment, approval, expiry, and sovereign-copy decisions.
- OPA remains Policy Authority. The API, registry, supply-chain verifier, AI runtime, and LLM cannot grant package approval, access, or workflow authority.
- Authorization occurs before any institutional registry read and is re-evaluated for every package before snapshot release.
- Supply-chain evidence is verified through vendor-neutral abstractions and cannot be replaced by caller assertions or placeholder references.
- No package content is downloaded, installed, restored, resolved, executed, or sent to an AI context.
- Evidence remains tenant-scoped, classified, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable through existing abstractions.
- Sovereign and air-gapped operation gains no mandatory external registry, API, SaaS, licensing, telemetry, or control-plane dependency.
- No direct `AI -> Production` path, external effect, or advancement beyond Approved Packages is introduced.

## 4. Decision

Approve one additional CR-001 product increment limited to **Governed Approved Packages Selection**.

Approval would authorize implementation, verification, source control, and governed GitHub synchronization for this increment only. It would not authorize package-registry mutation, live registries, package transfer or execution, external credentials, public deployment, institutional approval decisions, AI Planning, or any later station.

Decision: **Approved by the repository owner in the active delivery record**.

## 5. Master Specification Update

Append the following paragraph to the CR-001 business implementation addendum in `docs/PROJECT_MASTER_SPECIFICATION_V2.md`:

> Operational Increment 07 is **Governed Approved Packages Selection**. It defines a protected, OPA-scoped exact-package request bound to a previously released, evidence-bearing Existing Architecture snapshot. Verified policy establishes an explicit set of immutable package coordinates, kinds, tenant, purpose, environment, classification, and result scope before any institutional registry read. Every registry record must exactly match the authorized coordinate and pass institutional approval, expiry, tenant, environment, sovereign-copy, provenance, SBOM, signature, supply-chain, and per-result authorization checks before release into a deterministic, evidence-bearing snapshot. Missing Existing Architecture read, sovereign policy, institutional registry, eligibility, supply-chain assurance, result-authorization, or evidence adapters fail closed without registry access, package transfer, or institutional mutation. The result cannot advance to AI Planning, code generation, workflow execution, or material action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 07 — Governed Approved Packages Selection.
- Authorization: implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved in the active Codex delivery record on 2026-09-01.**

## Acceptance criteria

1. Selection requires an authenticated subject, matching tenant, sufficient classification clearance, explicit Approved Packages permission, purpose, authorization evidence, and matching governed-intent, Enterprise Context, Existing Systems, and Existing Architecture identities, versions, digests, and evidence.
2. The authoritative Existing Architecture snapshot is loaded through a deployment-controlled read boundary and structurally revalidated before policy evaluation; caller-supplied architecture content cannot become authoritative.
3. Every requested candidate uses an exact package kind, name, version, and algorithm-qualified immutable content digest; duplicates, ranges, floating tags, aliases, or missing digests are rejected before policy evaluation.
4. A verified signed-policy reference and evidence-bearing OPA permit define a non-empty explicit set of exact package coordinates, kinds, tenant, purpose, environment, classification, and result scope before any institutional package-registry read.
5. OPA denial, signature failure, mismatched decision, missing scope, invalid architecture snapshot, unavailable exact coordinate, or unavailable assurance dependency cannot release or substitute a package.
6. The registry is queried only for exact OPA-authorized coordinates and the selection path cannot invoke registry save, approval, update, or delete operations.
7. Every returned package exactly matches its requested and OPA-authorized coordinate and passes structural validation for provenance, publisher, license, tenant, environment, approval history, sovereign-copy availability, SBOM, signature, and timestamps.
8. Existing eligibility evaluation denies coordinate mismatch, tenant or environment mismatch, unavailable sovereign copy, missing approval, rejected, pending, suspended, revoked, or expired approval.
9. Each accepted package has a current `Approved`, unexpired, evidence-bearing decision and deployment-verified provenance, SBOM, signature, immutable digest, and sovereign-registry evidence.
10. Any missing, duplicate, unexpected, out-of-scope, partially substituted, structurally invalid, unapproved, or assurance-failing package causes a hard failure; every accepted package is re-authorized before release.
11. The returned snapshot is deterministically ordered, bound to registration, Enterprise Context, Existing Systems, Existing Architecture, and policy, carries a SHA-256 selection digest and non-placeholder evidence references, and records `CanAdvance: false`.
12. Missing architecture reader, OPA, institutional registry, eligibility evaluator, supply-chain assurance, per-result authorization, or evidence adapters remain visible and fail closed without registry access, package transfer, or institutional mutation.
13. No package discovery without exact coordinates, recommendation, download, restore, installation, resolution, execution, publication, registry mutation, institutional approval decision, AI call, workflow advancement, material action, deployment, new package, new project, or architectural deviation is introduced.
14. OpenAPI 3.1 and the Blazor experience expose the boundary without claiming registry connectivity, package availability, or approval authority.
15. All prior phase and Increment 01–06 acceptance gates remain satisfied.
16. `scripts/verify-project.ps1` succeeds with all 15 projects at zero warnings and zero errors, and runtime verification proves protected and fail-closed behavior.
