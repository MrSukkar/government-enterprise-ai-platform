# CR-001 Amendment 03 — Existing Architecture Boundary

Status: **Approved for Operational Increment 06**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-02-EXISTING-SYSTEMS.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next station in the approved vertical slice after Authorized Existing Systems Discovery:

`Existing Systems -> Existing Architecture`

Approved increment name: **Operational Increment 06 — Authorized Existing Architecture Discovery**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request a deterministic, evidence-bearing snapshot of the approved existing architecture associated with a previously released Existing Systems snapshot. Verified OPA policy restricts access to explicit tenant, purpose, classification, environment, system, architecture-source, component, dependency, interface, constraint, and decision-reference scope before any architecture source is accessed. Every released architecture item is structurally validated and re-authorized. The result cannot advance to Approved Packages, AI planning, code generation, workflow execution, or production.

## 2. Impact Analysis

### In scope

- A protected REST contract for Existing Architecture discovery from a previously released, evidence-bearing Existing Systems snapshot.
- Revalidation of authenticated subject, tenant, purpose, classification clearance, explicit architecture-discovery permission, authorization evidence, governed-intent registration identity and version, intent digest, Enterprise Context identity and digest, Existing Systems discovery identity and digest, and their evidence references.
- A deployment-controlled read boundary for the authoritative Existing Systems snapshot; caller-supplied inventory or architecture content is not authoritative.
- A signed, environment-aware OPA policy reference and evidence-bearing decision that defines non-empty explicit system, architecture-source, component, dependency, interface, constraint, decision-reference, tenant, purpose, and maximum-classification scope before any architecture source access.
- Vendor-neutral read contracts under the existing `Platform.Integrations` boundary for deployment-approved enterprise architecture repositories, catalogs, or controlled architecture artifacts.
- Architecture records bound to authorized existing-system identities and limited to approved boundaries, components or modules, dependencies, interfaces, constraints, architecture-decision references, technology-baseline references, source, classification, lifecycle, timestamps, and evidence.
- Structural validation that each architecture item refers only to systems present in the authoritative Existing Systems snapshot and stays within the policy-authorized source, system, item-kind, classification, lifecycle, tenant, purpose, and environment scope.
- Explicit distinction between approved architecture facts and discovered, inferred, unknown, draft, superseded, or unapproved material; only policy-authorized approved facts may be released as the existing architecture baseline.
- Per-item and per-relationship authorization re-check before inclusion in the returned architecture snapshot.
- Conformance checks against the constitutional architecture invariants and approved technology baseline without changing, replacing, or interpreting them as a new architecture authority.
- Deterministic ordering, a SHA-256 architecture digest, and an evidence-bearing discovery receipt bound to the governed intent, Enterprise Context snapshot, Existing Systems snapshot, and OPA decision.
- Explicit `CanAdvance: false`; Approved Packages remains a separately governed future station.
- OpenAPI 3.1, Blazor boundary communication, readiness visibility, acceptance verification, and runtime non-regression checks.
- Fail-closed behavior when Existing Systems snapshot read, signed OPA policy, architecture source, per-result authorization, conformance validation, or required evidence dependencies are unavailable.

### Out of scope

- Approval or implementation of Approved Packages, AI Planning, or any later vertical-slice station.
- Architecture design, redesign, recommendation, optimization, migration planning, impact approval, architecture-decision approval, or automatic conformance remediation.
- Treating repository files, caller input, generated content, discovered topology, inferred dependencies, drafts, or obsolete architecture records as approved institutional architecture merely because they are readable.
- Live calls to operational applications, databases, network devices, infrastructure control planes, cloud accounts, source-control hosts, CI/CD systems, observability backends, or production environments.
- Source-code crawling, binary inspection, network scanning, topology probing, runtime tracing, credential use, secret retrieval, live session use, command execution, agent installation, or external effects.
- Creation, update, registration, deletion, reconciliation, or correction of Enterprise Model objects, Existing Systems records, architecture records, decisions, or relationships.
- AI invocation, prompt construction, model context release, code generation, package selection, workflow advancement, or material action.
- A concrete enterprise-architecture product connector, filesystem provider, Git provider, Neo4j provider, PostgreSQL provider, identity-provider adapter, OPA adapter, evidence-store adapter, external credential, or non-sovereign control-plane dependency.
- Fake or in-memory production adapters, sample institutional architectures, synthetic permits, fabricated dependencies, invented technology decisions, or placeholder evidence accepted as authoritative.
- A new database, graph technology, package, .NET project, service boundary, microservice, architecture modeling language, or conditional technology approval.
- A numerical SLO, architecture-size limit, timeout, retry count, confidence threshold, scoring formula, or performance target before workload benchmarking and policy configuration.
- Changes to the fixed 30-phase roadmap, the Master Specification, or any constitutional invariant before this amendment is approved through Change Control.

### Affected existing boundaries

- `Platform.SoftwareFactory`: owns Create Internal Service orchestration and binds Existing Architecture discovery to the governed intent, Enterprise Context snapshot, and Existing Systems snapshot.
- `Platform.Identity`: supplies the already-authenticated governed request context and access checks; it does not parse credentials in this increment.
- `Platform.Governance`: remains the signed-policy verification and OPA authority.
- `Platform.EnterpriseModel`: remains the contextual source of truth for system identity, relationships, ownership, classification, source, lifecycle, and evidence without mutation.
- `Platform.Integrations`: owns vendor-neutral read contracts for deployment-supplied architecture sources; no concrete source or live connector is included.
- `Platform.Modeling`: retains impact-analysis and simulation responsibilities; it does not become an architecture approval or discovery authority.
- `Platform.SoftwareFactory/VerticalSlice` and the approved Phase 28 architecture artifacts: provide the fixed station order and constitutional conformance constraints; they are not treated as caller-controlled institutional architecture data.
- `Platform.Api`: composes the protected REST boundary without moving domain, policy, source, conformance, or evidence authority into the API layer.
- `Platform.Web`: communicates availability and fail-closed state without claiming that enterprise architecture repositories are connected.
- `Platform.Evidence`: remains the cryptographic evidence authority; no fabricated evidence implementation is introduced.
- OpenAPI and verification scripts: record and prove the protected contract, ordering, scope, and non-regression boundaries.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity provider configuration.
- A sovereign signed-policy verifier and OPA evaluation adapter.
- A deployment-approved authorized Existing Systems snapshot reader.
- An authorized, versioned architecture source containing institutionally approved records bound to Enterprise Model system identities.
- A policy-authorized per-item and per-relationship access evaluator.
- A deployment-approved architecture-conformance validator bound to the Master Specification invariants.
- A sovereign cryptographic evidence implementation when authoritative evidence persistence is required.

Until these dependencies are configured, Existing Architecture discovery must return unavailable or denied without architecture-source access, institutional mutation, or advancement.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- `docs/PROJECT_MASTER_SPECIFICATION_V2.md` remains the only implementation authority; Phase 28 artifacts remain subordinate conformance records.
- The Enterprise Model remains the contextual source of truth; architecture discovery is bound to authorized existing-system identities and cannot mutate institutional state.
- PostgreSQL remains the primary database baseline and Neo4j remains the Enterprise Graph baseline; neither receives a concrete adapter under this amendment.
- OPA remains Policy Authority. The API, architecture source, AI runtime, and LLM cannot grant architecture access, approval, or workflow authority.
- Authorization occurs before any architecture source access and is re-evaluated for every architecture item and relationship before snapshot release.
- `Platform.Integrations` remains a vendor-neutral adapter-contract boundary; no live repository, operational-system connection, credential, or source crawler is introduced.
- The amendment discovers only already approved architecture facts and cannot create, approve, supersede, or reinterpret an architecture decision.
- Evidence remains tenant-scoped, classified, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable through existing abstractions.
- Sovereign and air-gapped operation gains no mandatory external API, SaaS, licensing, telemetry, or control-plane dependency.
- No direct `AI -> Production` path, external effect, or advancement beyond Existing Architecture is introduced.

## 4. Decision

Approve one additional CR-001 product increment limited to **Authorized Existing Architecture Discovery**.

Approval would authorize implementation, verification, source control, and governed GitHub synchronization for this increment only. It would not authorize live connectors, external credentials, public deployment, institutional mutation, architecture redesign or approval, Approved Packages selection, AI execution, or any later station.

Decision: **Approved by the repository owner in the active delivery record**.

## 5. Master Specification Update

Append the following paragraph to the CR-001 business implementation addendum in `docs/PROJECT_MASTER_SPECIFICATION_V2.md`:

> Operational Increment 06 is **Authorized Existing Architecture Discovery**. It defines a protected, OPA-scoped architecture request bound to a previously released, evidence-bearing Existing Systems snapshot. Verified policy establishes explicit system, architecture-source, component, dependency, interface, constraint, decision-reference, tenant, purpose, and classification scope before any architecture source access. Every released architecture item is bound to an authorized existing system, structurally validated, checked for constitutional conformance, and re-authorized before release into a deterministic, evidence-bearing snapshot. Missing Existing Systems read, sovereign policy, approved architecture source, result-authorization, conformance, or evidence adapters fail closed without source access or institutional mutation. The result cannot advance to Approved Packages, AI planning, code generation, workflow execution, or material action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 06 — Authorized Existing Architecture Discovery.
- Authorization: implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved in the active Codex delivery record on 2026-09-01.**

## Acceptance criteria

1. Discovery requires an authenticated subject, matching tenant, sufficient classification clearance, explicit Existing Architecture discovery permission, purpose, authorization evidence, and matching governed-intent, Enterprise Context, and Existing Systems identities, versions, digests, and evidence.
2. The authoritative Existing Systems snapshot is loaded through a deployment-controlled read boundary and structurally revalidated before policy evaluation; caller-supplied inventory or architecture content cannot become authoritative.
3. A verified signed-policy reference and evidence-bearing OPA permit define non-empty explicit system, architecture-source, item-kind, relationship, tenant, purpose, environment, and maximum-classification scope before any architecture source is called.
4. OPA denial, signature failure, mismatched decision, missing scope, unavailable authorized source, invalid prerequisite snapshot, or unavailable conformance dependency cannot call an architecture source.
5. Architecture sources receive only the authorized scope and return no credentials, secrets, executable commands, live sessions, unapproved generated content, or external effects.
6. Every returned item is bound to an authorized Existing Systems identity, has an approved and versioned source state, and matches the authorized type, source, lifecycle, environment, classification, tenant, purpose, and policy scope.
7. Every dependency, interface, constraint, and decision reference stays within authorized endpoints and scope, is evidence-bearing, and cannot be elevated from draft, discovered, inferred, unknown, superseded, or unapproved state into an approved fact.
8. Any out-of-scope, structurally invalid, constitutionally conflicting, or unapproved item causes a hard failure; every accepted item and relationship is re-authorized before release.
9. The returned snapshot is deterministically ordered, bound to registration, context, Existing Systems, and policy, carries a SHA-256 digest and non-placeholder evidence references, and records `CanAdvance: false`.
10. Missing Existing Systems reader, OPA, architecture source, per-result authorization, conformance, or evidence adapters remain visible and fail closed without source access or institutional mutation.
11. No architecture creation or redesign, decision approval, Enterprise Model mutation, Approved Packages step, source crawl, live connector, credential, network probe, AI call, workflow advancement, material action, deployment, new package, new project, or architectural deviation is introduced.
12. OpenAPI 3.1 and the Blazor experience expose the boundary without claiming architecture-source connectivity, institutional approval, or operational readiness.
13. All prior phase and Increment 01–05 acceptance gates remain satisfied.
14. `scripts/verify-project.ps1` succeeds with all 15 projects at zero warnings and zero errors, and runtime verification proves protected and fail-closed behavior.
