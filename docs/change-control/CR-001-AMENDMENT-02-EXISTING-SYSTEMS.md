# CR-001 Amendment 02 — Existing Systems Boundary

Status: **Approved for Operational Increment 05**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-01-ENTERPRISE-CONTEXT.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next station in the approved vertical slice after Authorized Enterprise Context Discovery:

`Enterprise Context -> Existing Systems`

Approved increment name: **Operational Increment 05 — Authorized Existing Systems Discovery**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request an evidence-bearing inventory snapshot of existing enterprise systems and their authorized relationships for a previously released Enterprise Context snapshot. Discovery is restricted to explicit tenant, purpose, classification, environment, system-object, relationship, and source scope established by verified OPA policy. The result cannot advance to Existing Architecture, Approved Packages, AI planning, code generation, workflow execution, or production.

## 2. Impact Analysis

### In scope

- A protected REST contract for Existing Systems discovery from a previously released, evidence-bearing Enterprise Context snapshot.
- Revalidation of authenticated subject, tenant, purpose, classification clearance, explicit discovery permission, authorization evidence, governed intent registration identity and version, intent digest, context discovery identity, context digest, and context evidence.
- A deployment-controlled read boundary for the authorized Enterprise Context snapshot; caller-supplied context content is not authoritative.
- A signed, environment-aware OPA policy reference and evidence-bearing decision that defines explicit allowed enterprise-system object identities, relationship types, source kinds, and maximum classification before any system inventory source access.
- Vendor-neutral Existing Systems inventory source contracts under the existing `Platform.Integrations` boundary.
- Existing-system candidates shaped by the approved Enterprise Object fields: identity, type, state, owner, classification, relationships, policies, permitted actions, source, confidence, evidence, lifecycle, and timestamps.
- Structural rejection of any system or relationship outside the authorized tenant, object, relationship, source, classification, purpose, or policy scope.
- Per-system and per-relationship authorization re-check before inclusion in the returned inventory snapshot.
- Preservation of the approved relationship knowledge states: `CONFIRMED`, `DISCOVERED`, `INFERRED`, and `UNKNOWN`; confirmed relationships require evidence.
- Deterministic ordering, a SHA-256 inventory digest, and an evidence-bearing discovery receipt bound to the registered intent, Enterprise Context snapshot, and OPA decision.
- Explicit `CanAdvance: false`; Existing Architecture remains a separately governed future station.
- OpenAPI 3.1, Blazor boundary communication, readiness visibility, acceptance verification, and runtime non-regression checks.
- Fail-closed behavior when Enterprise Context snapshot read, signed OPA policy, system inventory source, per-result authorization, or required evidence dependencies are unavailable.

### Out of scope

- Approval or implementation of Existing Architecture, Approved Packages, AI Planning, or any later vertical-slice station.
- Live calls to operational systems, application APIs, databases, registries, CMDB products, service catalogs, infrastructure control planes, or network devices.
- Network scanning, topology probing, credential use, secret retrieval, agent installation, telemetry collection, or runtime health inspection.
- Creation, update, registration, deletion, reconciliation, or correction of Enterprise Model objects or relationships.
- Treating inferred, discovered, or unknown relationships as confirmed facts.
- AI invocation, prompt construction, model context release, code generation, workflow advancement, or material action.
- A concrete connector, Neo4j provider, PostgreSQL provider, identity-provider adapter, OPA adapter, evidence-store adapter, external credential, or non-sovereign control-plane dependency.
- Fake or in-memory production adapters, sample institutional systems, synthetic policy permits, fabricated relationships, or placeholder evidence accepted as authoritative.
- A new database, graph technology, package, .NET project, service boundary, microservice, or conditional technology approval.
- A numerical SLO, inventory size, timeout, retry count, confidence threshold, or performance target before workload benchmarking and policy configuration.
- Changes to the fixed 30-phase roadmap or any constitutional invariant.

### Affected existing boundaries

- `Platform.SoftwareFactory`: owns the Create Internal Service product orchestration and binds Existing Systems discovery to the governed intent and authorized Enterprise Context snapshot.
- `Platform.Identity`: supplies the already-authenticated governed request context and access checks; it does not parse credentials in this increment.
- `Platform.Governance`: remains the signed-policy verification and OPA authority.
- `Platform.Knowledge`: supplies the evidence-bearing authorized Enterprise Context boundary consumed by this station; it does not authorize unrestricted retrieval.
- `Platform.EnterpriseModel`: remains the contextual source of truth and supplies the approved Enterprise Object and relationship semantics without mutation.
- `Platform.Integrations`: owns vendor-neutral contracts for deployment-supplied system inventory sources; no concrete connector is included.
- `Platform.Api`: composes the protected REST boundary without moving domain, policy, or inventory authority into the API layer.
- `Platform.Web`: communicates availability and fail-closed state without claiming that institutional systems are connected.
- `Platform.Evidence`: remains the cryptographic evidence authority; no fabricated evidence implementation is introduced.
- OpenAPI and verification scripts: record and prove the protected contract, ordering, scope, and non-regression boundaries.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity provider configuration.
- A sovereign signed-policy verifier and OPA evaluation adapter.
- A deployment-approved authorized Enterprise Context snapshot reader.
- Authorized Enterprise Model and system inventory source adapters under the Integrations boundary.
- A policy-authorized per-system and per-relationship access evaluator.
- A sovereign cryptographic evidence implementation when authoritative evidence persistence is required.

Until these dependencies are configured, Existing Systems discovery must return unavailable or denied without inventory source access, institutional mutation, or advancement.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- The Enterprise Model remains the contextual source of truth; discovery cannot mutate it.
- Existing systems are represented through approved Enterprise Object and relationship semantics rather than a competing inventory model.
- PostgreSQL remains the primary database baseline and Neo4j remains the Enterprise Graph baseline; neither receives a concrete adapter under this amendment.
- OPA remains Policy Authority. The API, integration source, AI runtime, and LLM cannot grant inventory access or workflow authority.
- Authorization occurs before any inventory source access and is re-evaluated for every system and relationship before snapshot release.
- `Platform.Integrations` remains a vendor-neutral adapter-contract boundary; no live operational-system connection or credential is introduced.
- Evidence remains tenant-scoped, classified, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable through existing abstractions.
- Sovereign and air-gapped operation gains no mandatory external API, SaaS, licensing, telemetry, or control-plane dependency.
- No direct `AI -> Production` path, external effect, or advancement beyond Existing Systems is introduced.

## 4. Decision

Approve one additional CR-001 product increment limited to **Authorized Existing Systems Discovery**.

Approval would authorize implementation, verification, source control, and governed GitHub synchronization for this increment only. It would not authorize live connectors, external credentials, public deployment, Enterprise Model mutation, Existing Architecture discovery, AI execution, or any later station.

Decision: **Approved by the repository owner in the active delivery record**.

## 5. Master Specification Update

Append the following paragraph to the CR-001 business implementation addendum in `docs/PROJECT_MASTER_SPECIFICATION_V2.md`:

> Operational Increment 05 is **Authorized Existing Systems Discovery**. It defines a protected, OPA-scoped inventory request bound to a previously released, evidence-bearing Enterprise Context snapshot. Verified policy establishes explicit system-object, relationship, source, tenant, purpose, and classification scope before any inventory source access. Every returned system and relationship is structurally validated and re-authorized before release into a deterministic, evidence-bearing snapshot. Missing context-read, sovereign policy, inventory-source, authorization, or evidence adapters fail closed without source access or institutional mutation. The result cannot advance to Existing Architecture, AI planning, code generation, workflow execution, or material action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 05 — Authorized Existing Systems Discovery.
- Authorization: implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved in the active Codex delivery record on 2026-09-01.**

## Acceptance criteria

1. Discovery requires an authenticated subject, matching tenant, sufficient classification clearance, explicit Existing Systems discovery permission, purpose, authorization evidence, and matching governed-intent and Enterprise Context identities, versions, digests, and evidence.
2. The authoritative Enterprise Context snapshot is loaded through a deployment-controlled read boundary and structurally revalidated before policy evaluation; caller-supplied context content cannot become authoritative.
3. A verified signed-policy reference and evidence-bearing OPA permit define non-empty explicit system-object, relationship-type, source-kind, tenant, purpose, and classification scope before any inventory source is called.
4. OPA denial, signature failure, mismatched decision, missing scope, unavailable authorized source kind, or invalid context cannot call an inventory source.
5. Inventory sources receive only the authorized scope and return no credentials, secrets, executable commands, live sessions, or external effects.
6. Every returned system matches the approved Enterprise Object shape and authorized tenant, object, source, lifecycle, classification, and policy scope.
7. Every relationship uses an approved knowledge state, stays inside authorized object and relationship scope, and carries evidence when confirmed.
8. Any out-of-scope or structurally invalid system or relationship causes a hard failure; every accepted system and relationship is re-authorized before release.
9. The returned snapshot is deterministically ordered, bound to registration, context, and policy, carries a SHA-256 digest and non-placeholder evidence references, and records `CanAdvance: false`.
10. Missing context-reader, OPA, inventory-source, per-result authorization, or evidence adapters remain visible and fail closed without source access or institutional mutation.
11. No Enterprise Model mutation, Existing Architecture step, live connector, credential, network probe, AI call, workflow advancement, material action, deployment, new package, new project, or architectural deviation is introduced.
12. OpenAPI 3.1 and the Blazor experience expose the boundary without claiming operational-system connectivity or readiness.
13. All prior phase and Increment 01–04 acceptance gates remain satisfied.
14. `scripts/verify-project.ps1` succeeds with all 15 projects at zero warnings and zero errors, and runtime verification proves protected and fail-closed behavior.
