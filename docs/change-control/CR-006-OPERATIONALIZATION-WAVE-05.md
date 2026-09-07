# CR-006 — Operationalization Wave 05: Authorized Existing Architecture Runtime

Status: **Approved for Operationalization Wave 05**

Preceding completed authority: `docs/change-control/CR-005-OPERATIONALIZATION-WAVE-04.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Operationalize the fourth station of the approved Create Internal Service path by connecting Authorized Existing Architecture Discovery to the exact evidence-bearing Existing Systems snapshot produced by Wave 04, the sovereign OPA control plane, the approved Neo4j Enterprise Graph, deterministic constitutional conformance, and atomic PostgreSQL evidence persistence.

Wave name: **Operationalization Wave 05 — Authorized Existing Architecture Runtime**.

### Outcome

When—and only when—deployment-approved identity, policy, PostgreSQL, and Neo4j profiles are valid, an authorized caller can request the approved architecture facts associated with an exact released Existing Systems snapshot. The platform cryptographically revalidates that tenant-scoped prerequisite, obtains an action-specific signed-bundle-bound OPA scope before source access, validates the scope against the Master Specification, executes one fixed parameterized scope-first Graph read, rejects unapproved or constitutionally conflicting content, re-authorizes every item and relationship, and atomically records the deterministic architecture evidence.

Missing configuration, prerequisite mismatch, policy denial, invalid constitutional scope, unavailable source, unapproved state, out-of-scope item or relationship, authorization rejection, malformed Graph data, or evidence failure prevents architecture release and fails closed.

## 2. Current-state evidence

- All 30 phases, CR-001 Operational Increments 01–22, and Operationalization Waves 01–04 are complete and verified.
- Wave 04 atomically persists the authoritative Existing Systems evidence record in PostgreSQL but deliberately leaves `IAuthorizedExistingSystemsSnapshotReader` disconnected.
- `IExistingArchitecturePolicyGate`, `IExistingArchitectureSource`, `IExistingArchitectureConformanceValidator`, `IExistingArchitectureResultAuthorizer`, and `IExistingArchitectureEvidenceRecorder` remain disconnected.
- `AuthorizedExistingArchitectureDiscoveryEngine` already enforces exact prerequisite identities and digests, policy-before-source ordering, explicit system/source/item/relationship scope, scope and item conformance, approved-state validation, per-result authorization, deterministic hashing, evidence receipt validation, and `CanAdvance: false`.
- `Platform.Integrations` already owns the vendor-neutral `IExistingArchitectureSource` contract and typed bounded candidate contracts.
- Neo4j is the approved Enterprise Graph baseline and Waves 03–04 provide a configuration-gated encrypted read-only driver. PostgreSQL remains the approved primary transactional and evidence store.
- Repository-default runtime exposes 142 disconnected institutional dependencies and remains fail closed.

## 3. Impact Analysis

### In scope

- Implement `IAuthorizedExistingSystemsSnapshotReader` over the Wave 04 PostgreSQL evidence table using the exact tenant/discovery key, fixed parameterized SQL, bounded commands, stored-record SHA-256 verification, evidence-reference digest verification, inventory digest recomputation, and strict deserialization/revalidation of the complete authoritative inventory record.
- Extend the sovereign typed OPA response envelope with a distinct Existing Architecture scope containing maximum classification, exact system identifiers, exact item kinds, exact relationship types, the exact source kind, required roles, and maximum result count. Other action adapters must reject this scope.
- Implement `IExistingArchitecturePolicyGate` with signed bundle verification before the exact `internal-service.existing-architecture.discover` OPA evaluation and strict revalidation of every identity, prerequisite digest, bundle, environment, outcome, scope, evidence, and time field.
- Reuse the approved `Neo4j.Driver` `6.3.0`, the encrypted Wave 03 Graph profile, and the single configured driver. No new package or technology decision is introduced.
- Implement one read-only Neo4j adapter for `IExistingArchitectureSource` with the exact source kind `enterprise-graph`.
- Use fixed parameterized Cypher beginning from exact OPA-authorized Existing Systems identities and tenant. Apply environment, classification, approved-state, lifecycle, item-kind, relationship-type, source-kind, and result bounds before records are returned. No unrestricted traversal, dynamic Cypher, file crawl, or retrieve-then-filter path is permitted.
- Map only the approved architecture fields. Reject missing, duplicate, malformed, draft, discovered, inferred, unknown, superseded, generated, credential-bearing, live-session-bearing, command-bearing, effect-bearing, out-of-tenant, out-of-system, out-of-environment, out-of-classification, out-of-source, out-of-kind, out-of-relationship, or excessive results.
- Implement `IExistingArchitectureConformanceValidator` as a deterministic validator bound to `docs/PROJECT_MASTER_SPECIFICATION_V2.md`. It validates exact scope and typed facts against the fixed modular-monolith, .NET 10/ASP.NET Core, Blazor WebAssembly, REST/OpenAPI 3.1, PostgreSQL, Neo4j, OPA, OpenTelemetry, sovereign operation, evidence, retrieval-authorization, and no-direct-`AI -> Production` invariants. It cannot approve architecture, infer compliance, redesign, remediate, or weaken a constitutional invariant.
- Extend result-authorization input with the authenticated identity and exact OPA-authorized role/scope binding; implement `IExistingArchitectureResultAuthorizer` through the existing deterministic RBAC/ABAC evaluator.
- Add a versioned PostgreSQL migration and `IExistingArchitectureEvidenceRecorder` adapter for immutable, idempotent, tenant-scoped architecture snapshots and SHA-256-qualified evidence references. Writes use explicit transactions and never automatically retry an ambiguous mutation.
- Register the six exact runtime contracts only when policy, PostgreSQL, and Neo4j profiles are complete and valid. `IAuthorizedExistingArchitectureSnapshotReader` remains disconnected.
- Add non-sensitive Existing Architecture readiness without revealing endpoints, connection strings, database names, users, credentials, queries, Graph values, or trust material.
- Add deterministic verification for prerequisite integrity, action-specific policy scope, policy-before-source ordering, conformance-before-source and per-item conformance, scope-first Graph access, fixed parameterization, tenant isolation, approved-state enforcement, result re-authorization, evidence atomicity, dependency unavailability, and repository-default fail-closed behavior.
- Update OpenAPI descriptions, UI status, operational guidance, acceptance evidence, and both project status records only after successful implementation verification.

### Out of scope

- Provisioning Neo4j or PostgreSQL, applying schemas externally, changing Graph indexes or constraints, importing institutional architecture, acquiring credentials, choosing a secret manager, selecting certificates, or writing organization-specific policy.
- Any architecture repository, modeling-suite, CMDB, source-control, filesystem, cloud, operational-system, telemetry, infrastructure, network-device, agent, or SaaS connector.
- Source or binary crawling, network scanning, topology probing, runtime tracing, credential use, secret retrieval, remote command, live session, external effect, or ambiguous mutation retry.
- Architecture creation, redesign, recommendation, optimization, migration planning, decision approval, conformance remediation, Graph writes, Enterprise Model mutation, reconciliation, or relationship correction.
- Treating repository files, generated content, inferred topology, drafts, or obsolete records as approved institutional architecture merely because they are readable.
- A second architecture source kind, database, Graph technology, modeling language, package family, vector/lexical retrieval, pgvector, Qdrant, embeddings, or AI invocation.
- `IAuthorizedExistingArchitectureSnapshotReader`, Approved Packages, AI Planning, later stations, workflow advancement, or production action.
- Public deployment, external account creation, network mutation, a new service boundary, phase, project, queue, cache, mandatory external control plane, or numerical SLO.

### Affected boundaries

- `Platform.SoftwareFactory` owns the prerequisite reader, policy adapter, deterministic conformance and result-authorization adapters, evidence adapter, and orchestration semantics.
- `Platform.Integrations` continues to own the vendor-neutral Existing Architecture source contract.
- `Platform.Knowledge` owns the read-only Neo4j implementation and reuses the existing driver/profile without creating a second Graph authority.
- `Platform.Identity` retains deterministic RBAC/ABAC and gains no credential-processing behavior.
- `Platform.Governance` owns only the bounded typed-policy transport extension; OPA remains policy authority.
- `Platform.Api` conditionally composes valid adapters and exposes non-sensitive readiness.
- PostgreSQL stores the authoritative prerequisite and immutable architecture evidence; Neo4j remains a read-only Enterprise Graph projection. Neither gains policy, workflow, conformance, or mutation authority.

## 4. Data and security review

1. The Existing Systems record is tenant/discovery scoped and its JSON, record digest, evidence-reference digest, prerequisite identities, inventory digest, authorized systems/relationships, evidence, released state, and timestamps are verified before OPA.
2. Signed policy-bundle verification and an exact OPA permit establishing non-empty bounded Existing Architecture scope precede conformance validation and source access.
3. Existing Architecture scope is action-specific and cannot be substituted with Intent, Enterprise Context, or Existing Systems scope. Denial carries no operational scope.
4. Constitutional scope validation succeeds before source access; each returned item is independently checked for structural and constitutional conformance before authorization and release.
5. Cypher is fixed and read-only. Tenant, exact system identifiers, classification ceiling, environment, approved state, lifecycle, item kinds, relationship types, source kind, and result limit are parameters.
6. Relationships are released only when both systems and the relationship type are in the OPA-authorized Existing Systems and architecture scopes.
7. Only already approved, versioned architecture facts may be released. No status is inferred, promoted, corrected, or approved by the adapter or validator.
8. Every architecture item and relationship is independently re-authorized using the authenticated identity and OPA-bound roles and scope.
9. The exact architecture digest, prerequisite/policy/conformance/result evidence, and immutable evidence reference are committed atomically and idempotently under tenant scope.
10. Credentials, connection strings, endpoints, query parameters, raw policy inputs, raw unauthorized Graph properties, secrets, commands, sessions, and external effects are not logged or exposed.
11. Graph access is read-only. Evidence persistence uses explicit transactions without driver-managed mutation retries; cancellation or ambiguous outcomes cannot return success or duplicate mutation.

## 5. Proposed implementation sequence

1. Add the cryptographically validating PostgreSQL Existing Systems snapshot reader.
2. Add the distinct typed Existing Architecture OPA scope and reject cross-action scope substitution.
3. Implement the signed-policy/OPA Existing Architecture gate.
4. Implement deterministic Master Specification scope/item conformance.
5. Extend and implement exact RBAC/ABAC result authorization.
6. Implement the fixed scope-first Neo4j `enterprise-graph` architecture source using the existing driver/profile.
7. Add the architecture-evidence migration and atomic PostgreSQL recorder.
8. Compose the six contracts only under complete valid policy/database/graph profiles.
9. Update readiness, OpenAPI, UI, operational documentation, acceptance evidence, verifier, and project state.
10. Run the full verifier and commit/push only verified source.

## 6. Architectural Review

Finding: **Conforms without architectural deviation, subject to explicit approval and implementation verification**.

- Every selected technology and module boundary is already approved.
- The Enterprise Model remains the contextual source of truth; the Graph adapter reads only approved projections and cannot mutate or approve them.
- OPA remains policy authority, while the Master Specification remains architecture authority. The conformance validator is deterministic enforcement only.
- Authorization remains before source access and is repeated for every item and relationship.
- No concrete external connector, new package, conditional technology, database, project, service boundary, or mandatory external control plane is introduced.
- Sovereign, on-premises, and air-gapped operation remains possible with deployment-supplied local identity, policy, PostgreSQL, Neo4j, PKI, and trust configuration.
- No AI call, direct `AI -> Production` path, external effect, material action, or advancement beyond Existing Architecture is introduced.

## 7. Package decision

No new package is requested. Wave 05 reuses locked `Npgsql` `10.0.3` and `Neo4j.Driver` `6.3.0`. No ORM, OGM, architecture-tool SDK, cloud SDK, crawler, agent, vector package, embedding package, pgvector, or Qdrant package is proposed.

## 8. Decision requested

Approve **CR-006 — Operationalization Wave 05: Authorized Existing Architecture Runtime** for bounded implementation, local verification, source control, and GitHub synchronization only.

Decision: **Approved by the repository owner on 2026-09-07 for bounded Wave 05 implementation, local verification, source control, and GitHub synchronization.**

This approval authorizes the bounded repository implementation, local verification, source control, and GitHub synchronization only. It does not authorize external credentials, database/graph provisioning, schema application, institutional data import, live connectors, network mutation, public deployment, architecture approval/redesign, certificate/trust selection, or organization-specific policy content.

## Acceptance gate

1. Repository-default and incomplete profiles remain unconfigured and fail closed; the disconnected-dependency count remains 142.
2. The tenant-scoped Existing Systems record and its cryptographic/storage/payload bindings are revalidated before OPA.
3. Signed bundle verification and exact action-specific OPA scope precede constitutional conformance and architecture-source access; denial carries no scope.
4. Constitutional scope conformance is bound to the Master Specification before source access; every item is conformant before authorization and release.
5. The Neo4j query is fixed, read-only, parameterized, tenant/system/environment/classification/approved-state/lifecycle/item-kind/relationship/source/result scoped before retrieval.
6. Every item and relationship is structurally validated, bound to an authorized Existing System, approved and versioned, and any invalid or out-of-scope result fails the request.
7. Every item and relationship is re-authorized using the authenticated identity and OPA-bound roles/scope before release.
8. Architecture evidence is deterministic, immutable, tenant scoped, idempotent, and atomically recorded with a SHA-256-qualified reference bound to exact prerequisites, policy, conformance, and results.
9. Unavailability, timeout, cancellation, denial, mismatch, malformed data, excessive scope, constitutional conflict, or ambiguous persistence cannot return released architecture.
10. No secret, credential, connection detail, raw query parameter, raw policy input, unauthorized property, command, session, mutation, or external effect is committed, logged, or disclosed.
11. `IAuthorizedExistingArchitectureSnapshotReader`, Approved Packages, every later station, live connector, Graph mutation, AI call, workflow advancement, and production action remain unavailable.
12. Existing 30 phase, 22 increment, and Waves 01–04 gates remain satisfied.
13. All 15 projects build with zero warnings and zero errors, and runtime verification proves the unconfigured fail-closed posture.
