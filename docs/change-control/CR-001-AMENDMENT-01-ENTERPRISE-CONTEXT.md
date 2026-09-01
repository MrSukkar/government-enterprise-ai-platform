# CR-001 Amendment 01 — Enterprise Context Boundary

Status: **Approved for Operational Increment 04**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next station in the approved vertical slice after governed intent registration:

`Intent -> Enterprise Context`

Approved increment name: **Operational Increment 04 — Authorized Enterprise Context Discovery**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request an evidence-bearing Enterprise Context snapshot for a previously registered governed intent. Discovery is restricted to an explicit tenant-, purpose-, classification-, environment-, and resource-scoped policy decision. The result cannot advance to Existing Systems, AI planning, code generation, workflow execution, or production.

## 2. Impact Analysis

### In scope

- A protected REST contract for Enterprise Context discovery from a registered governed intent.
- Revalidation of authenticated subject, tenant, purpose, classification clearance, explicit discovery permission, authorization evidence, registration identity, intent digest, and expected registration version.
- A signed, environment-aware OPA policy reference and an evidence-bearing decision that defines the allowed Enterprise Context resource scope before any source access.
- Retrieval through vendor-neutral boundaries already established by `Platform.Knowledge`; no unrestricted source query is permitted.
- Structural rejection of any source result outside the authorized tenant, resource, modality, or classification scope.
- Per-result authorization re-check before a candidate can enter the returned context snapshot.
- A deterministic context digest and evidence-bearing discovery receipt bound to the registered intent and policy decision.
- Explicit `CanAdvance: false`; Existing Systems remains a separately governed future station.
- OpenAPI 3.1, Blazor boundary communication, readiness visibility, acceptance verification, and runtime non-regression checks.
- Fail-closed behavior when registered-intent read, signed OPA policy, Enterprise Model/knowledge retrieval, or required evidence dependencies are unavailable.

### Out of scope

- Approval or implementation of Existing Systems, Existing Architecture, Approved Packages, AI Planning, or any later vertical-slice station.
- AI invocation, prompt construction, model context release, code generation, workflow advancement, or material action.
- Enterprise Model mutation, automatic registration, production deployment, or external side effects.
- Unrestricted graph, vector, lexical, database, or cross-tenant search.
- A concrete Neo4j, PostgreSQL, vector-store, identity-provider, OPA, evidence-store, or credential adapter.
- Fake or in-memory production adapters, sample institutional facts, synthetic policy permits, or placeholder evidence accepted as authoritative.
- A new database, graph technology, package, .NET project, service boundary, microservice, or external control-plane dependency.
- A numerical SLO, result limit, timeout, or performance target before workload benchmarking and policy configuration.
- Changes to the fixed 30-phase roadmap or any constitutional invariant.

### Affected existing boundaries

- `Platform.SoftwareFactory`: owns the Create Internal Service product contract and binds discovery to the governed intent registration.
- `Platform.Identity`: supplies the already-authenticated governed request context and access checks; it does not parse credentials in this increment.
- `Platform.Governance`: remains the OPA policy authority and signed-policy verification boundary.
- `Platform.Knowledge`: supplies authorized, explicitly scoped retrieval and per-result authorization re-check behavior.
- `Platform.EnterpriseModel`: remains the contextual source of truth through vendor-neutral read boundaries; no mutation is authorized.
- `Platform.Api`: composes the protected REST boundary without moving domain authority into the API layer.
- `Platform.Web`: communicates availability and fail-closed state without claiming operational readiness.
- `Platform.Evidence`: remains the cryptographic evidence authority; no fabricated evidence implementation is introduced.
- OpenAPI and verification scripts: record and prove the protected contract and non-regression boundaries.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity provider configuration.
- A sovereign signed-policy verifier and OPA evaluation adapter.
- A deployment-approved registered-intent read repository.
- Authorized Enterprise Model/knowledge retrieval source adapters, including the Neo4j baseline when configured.
- A sovereign cryptographic evidence implementation when authoritative evidence persistence is required.

Until these dependencies are configured, discovery must return an unavailable or denied result without source access, institutional mutation, or advancement.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- The Enterprise Model remains the contextual source of truth.
- PostgreSQL remains the primary database baseline and Neo4j remains the Enterprise Graph baseline; neither receives a concrete adapter under this amendment.
- Retrieval follows the constitutional order: identity, purpose, policy, authorized scope, source retrieval, fusion/reranking, authorization re-check, then context release.
- OPA remains Policy Authority. AI, the API, retrieval sources, and the LLM cannot grant discovery authority.
- Authorization occurs before source access and is re-evaluated before each candidate enters the context snapshot.
- Module boundaries remain explicit; no direct vendor dependency is introduced into the domain or Software Factory contract.
- Evidence remains tenant-scoped, classified, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable through existing abstractions.
- Sovereign and air-gapped operation gains no mandatory external control-plane dependency.
- No direct `AI -> Production` path or workflow authority is introduced.

## 4. Decision

Approve one additional CR-001 product increment limited to **Authorized Enterprise Context Discovery**.

Approval would authorize implementation, verification, source control, and governed GitHub synchronization for this increment only. It would not authorize deployment credentials, concrete production adapters, public deployment, Enterprise Model mutation, AI execution, Existing Systems discovery, or any later station.

Decision: **Approved by the repository owner in the active delivery record**.

## 5. Master Specification Update

Append the following paragraph to the CR-001 business implementation addendum in `docs/PROJECT_MASTER_SPECIFICATION_V2.md`:

> Operational Increment 04 is **Authorized Enterprise Context Discovery**. It defines a protected, OPA-scoped discovery request for a previously registered governed intent. Authorization is required before any Enterprise Model or knowledge source access and is re-checked for every candidate before release into a deterministic, evidence-bearing context snapshot. Missing registered-intent, sovereign policy, retrieval, or evidence adapters fail closed without source access or institutional mutation. The result cannot advance to Existing Systems, AI planning, code generation, workflow execution, or material action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 04 — Authorized Enterprise Context Discovery.
- Authorization: implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved in the active Codex delivery record on 2026-09-01.**

## Acceptance criteria

1. Discovery requires an authenticated subject, matching tenant, sufficient classification clearance, explicit discovery permission, purpose, authorization evidence, and a registered-intent identity, digest, and expected version.
2. The registered intent is structurally revalidated and must be persisted, policy-permitted, evidence-bearing, tenant-matching, and ineligible for mutation by the discovery operation.
3. A verified signed-policy reference and an evidence-bearing OPA permit define a non-empty explicit resource and modality scope before any retrieval source is called.
4. OPA denial, signature failure, mismatched policy data, missing scope, or unavailable prerequisite cannot call a retrieval source.
5. Retrieval sources receive only the authorized tenant, purpose, classification, resource, modality, and policy-bounded result scope.
6. Any out-of-scope source result causes a hard failure; each remaining candidate is re-authorized before context release.
7. The returned snapshot is deterministic, bound to the registration and policy decision, carries a SHA-256 digest and non-placeholder evidence references, and records `CanAdvance: false`.
8. Missing registered-intent read, OPA, retrieval, or evidence adapters remain visible and fail closed without institutional mutation.
9. No Enterprise Model mutation, Existing Systems step, AI call, workflow advancement, material action, deployment, new package, new project, or architectural deviation is introduced.
10. OpenAPI 3.1 and the Blazor experience expose the boundary without claiming operational readiness.
11. All prior phase and Increment 01–03 acceptance gates remain satisfied.
12. `scripts/verify-project.ps1` succeeds with all 15 projects at zero warnings and zero errors, and runtime verification proves protected and fail-closed behavior.
