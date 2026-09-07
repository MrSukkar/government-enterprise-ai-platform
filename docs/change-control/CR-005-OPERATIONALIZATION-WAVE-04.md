# CR-005 — Operationalization Wave 04: Authorized Existing Systems Runtime

Status: **Approved for Operationalization Wave 04**

Preceding completed authority: `docs/change-control/CR-004-OPERATIONALIZATION-WAVE-03.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Operationalize the third station of the approved Create Internal Service path by connecting Authorized Existing Systems Discovery to the exact evidence-bearing Enterprise Context snapshot produced by Wave 03, the sovereign OPA control plane, the approved Neo4j Enterprise Graph, and atomic PostgreSQL evidence persistence.

Wave name: **Operationalization Wave 04 — Authorized Existing Systems Runtime**.

### Outcome

When—and only when—deployment-approved identity, policy, PostgreSQL, and Neo4j profiles are valid, an authorized caller can request the existing enterprise systems and relationships associated with an exact released Enterprise Context snapshot. The platform reads and cryptographically revalidates that tenant-scoped prerequisite, obtains signed-bundle-bound OPA scope before inventory access, executes a fixed parameterized scope-first Graph read through the vendor-neutral Existing Systems source contract, structurally validates and re-authorizes every system and relationship, and atomically records the released deterministic inventory evidence.

Missing or invalid configuration, prerequisite mismatch, policy denial, empty or excessive scope, unavailable source, out-of-scope object or relationship, authorization rejection, malformed Graph data, or evidence failure prevents inventory release and fails closed.

## 2. Current-state evidence

- All 30 phases, CR-001 Operational Increments 01–22, and Operationalization Waves 01–03 are complete and verified.
- Wave 03 atomically persists the authoritative Enterprise Context evidence record in PostgreSQL but deliberately leaves `IAuthorizedEnterpriseContextSnapshotReader` disconnected.
- `IExistingSystemsPolicyGate`, `IExistingSystemInventorySource`, `IExistingSystemResultAuthorizer`, and `IExistingSystemsEvidenceRecorder` remain disconnected.
- `AuthorizedExistingSystemsDiscoveryEngine` already enforces exact intent/context identity, version, digest, tenant, purpose, classification, evidence, policy-before-source ordering, explicit system/relationship/source scope, structural validation, per-system and per-relationship authorization, deterministic hashing, evidence receipt validation, and `CanAdvance: false`.
- `Platform.Integrations` already owns the vendor-neutral `IExistingSystemInventorySource` contract and the bounded scope/candidate contracts.
- Neo4j is the approved Enterprise Graph baseline and Wave 03 already provides a configuration-gated encrypted read-only driver. PostgreSQL remains the approved primary transactional and evidence store.
- Repository-default runtime exposes 142 disconnected institutional dependencies and remains fail closed.

## 3. Impact Analysis

### In scope

- Implement `IAuthorizedEnterpriseContextSnapshotReader` over the Wave 03 PostgreSQL evidence table using an exact tenant/discovery key, fixed parameterized SQL, bounded commands, stored-record SHA-256 verification, evidence-reference digest verification, and strict deserialization/revalidation of the complete authoritative context record.
- Extend the sovereign typed OPA response envelope with a distinct Existing Systems scope containing maximum classification, exact allowed system identifiers, exact allowed relationship types, the exact allowed source kind, required roles, and maximum result count. Intent and Enterprise Context adapters must reject a scope belonging to another action.
- Implement `IExistingSystemsPolicyGate` with signed bundle verification before the exact `internal-service.existing-systems.discover` OPA evaluation and strict revalidation of every identity, prerequisite, bundle, environment, outcome, scope, evidence, and time field.
- Reuse the approved `Neo4j.Driver` `6.3.0`, the Wave 03 encrypted Neo4j profile, and the single configured driver. No new package or technology decision is introduced.
- Implement one read-only Neo4j adapter for the vendor-neutral `IExistingSystemInventorySource` contract with the exact source kind `enterprise-graph`.
- Use fixed parameterized Cypher beginning from the exact OPA-authorized system identifiers and tenant, bounded by classification and maximum results before records are returned. Relationship traversal is restricted to authorized relationship types and authorized target system identifiers. No unrestricted Graph scan or retrieve-then-filter path is permitted.
- Map only the approved Enterprise Object and relationship fields. Reject missing, duplicate, malformed, secret-bearing, live-session-bearing, command-bearing, effect-bearing, out-of-tenant, out-of-classification, out-of-source, out-of-object, out-of-relationship, or excessive results before release.
- Implement `IExistingSystemResultAuthorizer` with the existing deterministic RBAC/ABAC evaluator, bound to the authenticated identity, purpose, OPA-authorized roles/scope, fixed per-system or per-relationship read action, classification, tenant, and non-placeholder evidence. Denial or mismatch fails the entire discovery.
- Add a versioned PostgreSQL migration and `IExistingSystemsEvidenceRecorder` adapter for immutable, idempotent, tenant-scoped inventory snapshots and SHA-256-qualified evidence references. Evidence writes use explicit transactions and never use automatic retry after an ambiguous mutation outcome.
- Register the five exact Existing Systems runtime contracts only when policy, PostgreSQL, and Neo4j profiles are complete and valid. The later `IAuthorizedExistingSystemsSnapshotReader` remains disconnected.
- Add non-sensitive combined Existing Systems readiness without revealing endpoints, connection strings, database names, users, credentials, queries, Graph values, or trust material.
- Add deterministic verification for prerequisite integrity, policy-before-source ordering, scope-first Graph access, fixed parameterization, tenant isolation, relationship restrictions, result re-authorization, evidence atomicity, dependency unavailability, and repository-default fail-closed behavior.
- Update OpenAPI descriptions, UI status, operational guidance, acceptance evidence, and both project status records only after successful implementation verification.

### Out of scope

- Provisioning Neo4j or PostgreSQL, applying schemas to an external environment, changing Graph indexes or constraints, importing institutional inventory, acquiring credentials, choosing a secret manager, or selecting certificates.
- Any CMDB, service-catalog, operational-system, cloud, infrastructure, database, registry, telemetry, network-device, agent, or SaaS connector.
- Network scanning, topology probing, health inspection, active discovery, credential use, secret retrieval, remote command, live session, external effect, or retrying an ambiguous mutation.
- Graph writes, Enterprise Model creation/update/deletion/reconciliation, relationship correction, inferred-fact promotion, autonomous discovery, unrestricted traversal, dynamic Cypher, or AI-generated query.
- A second inventory source kind, a second database/graph technology, vector or lexical retrieval, pgvector, Qdrant, embeddings, AI invocation, or any conditional technology decision.
- `IAuthorizedExistingSystemsSnapshotReader`, Existing Architecture discovery, Approved Packages, AI Planning, later vertical-slice adapters, workflow advancement, or production action.
- Returning raw Graph properties outside the approved Enterprise Object/relationship contract or storing raw policy input, credentials, secrets, or connection details in evidence.
- Public deployment, external account creation, network mutation, a new service boundary, phase, project, database decision, queue, cache, mandatory external control plane, or numerical SLO.

### Affected boundaries

- `Platform.SoftwareFactory` owns the authoritative context reader, Existing Systems policy adapter, result-authorization adapter, evidence adapter, and orchestration semantics.
- `Platform.Integrations` continues to own the vendor-neutral Existing Systems inventory source contract.
- `Platform.Knowledge` owns the read-only Neo4j implementation of that contract and reuses the Wave 03 driver/profile without creating a second Graph connection authority.
- `Platform.Identity` retains deterministic RBAC/ABAC result re-authorization and gains no credential-processing behavior.
- `Platform.Governance` owns only the bounded typed-policy transport extension; OPA remains policy authority.
- `Platform.Api` conditionally composes valid adapters and exposes non-sensitive readiness.
- PostgreSQL stores the authoritative prerequisite and immutable inventory evidence; Neo4j remains a read-only Enterprise Graph source. Neither gains policy, workflow, or autonomous mutation authority.

## 4. Data and security review

Required invariants:

1. The Enterprise Context read is tenant/discovery scoped and its stored JSON, record digest, evidence-reference digest, receipt identity, registration identity/version, intent/context digests, policy outcome, released state, evidence, and timestamps are verified before OPA evaluation.
2. Signed policy-bundle verification and an exact OPA permit establishing non-empty bounded Existing Systems scope precede any inventory session or query.
3. Existing Systems policy scope is action-specific and cannot be substituted with Intent or Enterprise Context scope. Denial carries no operational scope.
4. Cypher text is fixed; tenant, exact system identifiers, classification ceiling, relationship types, source kind, and result limit are parameters. Caller-controlled values are never concatenated into Cypher.
5. Graph reads begin from exact authorized system identifiers and tenant; relationships are released only when source system, target system, and relationship type are all in the OPA-authorized scope.
6. Only source kind `enterprise-graph` is connected. Missing, duplicate, unexpected, or unavailable source registration fails before access.
7. Returned systems and relationships outside tenant, object, relationship, source, lifecycle, classification, result-count, or structural scope fail the entire request; they are not silently filtered into an apparently successful snapshot.
8. Every system and relationship is independently re-authorized through the existing identity access boundary using the authenticated identity and OPA-authorized scope before release. Confirmed relationships require evidence and no knowledge state is promoted.
9. The exact deterministic inventory digest, authorized systems/relationships, prerequisite/policy/result evidence, and immutable evidence reference are committed atomically and idempotently under tenant scope.
10. PostgreSQL and Neo4j credentials, connection strings, endpoints, query parameters, raw policy inputs, raw unauthorized Graph properties, and secrets are not logged or exposed through readiness or evidence.
11. Graph access is read-only. Evidence persistence uses explicit transaction semantics without driver-managed mutation retries; cancellation or ambiguous outcomes cannot return success or duplicate mutation.

## 5. Proposed implementation sequence

1. Add the tenant-scoped cryptographically validating PostgreSQL Enterprise Context snapshot reader.
2. Add the distinct typed Existing Systems OPA scope without weakening Intent or Enterprise Context scope validation.
3. Implement the signed-policy/OPA Existing Systems gate.
4. Extend result-authorization input with the exact authenticated identity and OPA-authorized role/scope binding required by the existing RBAC/ABAC evaluator.
5. Implement the deterministic per-system/per-relationship result authorizer.
6. Implement the fixed scope-first Neo4j `enterprise-graph` inventory source using the existing driver/profile and approved package version.
7. Add the explicit inventory-evidence migration and atomic PostgreSQL recorder.
8. Compose the five exact contracts only under complete valid policy/database/graph profiles.
9. Update readiness, OpenAPI, UI, operational documentation, acceptance evidence, verifier, and project state.
10. Run the full project verifier and commit/push only verified source.

## 6. Architectural Review

Finding: **Conforms without architectural deviation, subject to explicit approval and implementation verification**.

- PostgreSQL, Neo4j, OPA, OIDC/OAuth2, RBAC/ABAC, REST/OpenAPI 3.1, Blazor WebAssembly, and the Modular Monolith are already approved.
- The Enterprise Model remains the contextual source of truth; the inventory adapter only reads the Graph projection and cannot mutate or correct it.
- The authorization sequence remains exact: authenticated identity and purpose, authoritative prerequisite read, signed OPA scope, source access, structural validation, per-result RBAC/ABAC authorization, then immutable evidence.
- OPA remains policy authority; Neo4j remains a scoped read source; neither the API nor the inventory source gains workflow or policy authority.
- No concrete operational-system connector, external API, new package family, conditional technology, database, project, service boundary, or mandatory external control plane is introduced.
- Sovereign, on-premises, and air-gapped operation remains possible with deployment-supplied local identity, policy, PostgreSQL, Neo4j, PKI, and trust configuration.
- There remains no direct `AI -> Production` path, material action, external effect, or advancement beyond Existing Systems.

## 7. Package decision

No new package is requested. Wave 04 reuses the already approved and locked `Npgsql` `10.0.3` and `Neo4j.Driver` `6.3.0`. No ORM, Object Graph Mapper, CMDB SDK, cloud SDK, discovery agent, vector package, embedding package, pgvector, or Qdrant package is proposed.

## 8. Decision requested

Approve **CR-005 — Operationalization Wave 04: Authorized Existing Systems Runtime** for bounded implementation, local verification, source control, and GitHub synchronization only.

Decision: **Approved by the repository owner on 2026-09-07 for bounded Wave 04 implementation, local verification, source control, and GitHub synchronization.**

This approval authorizes the bounded repository implementation, local verification, source control, and GitHub synchronization only. It does not authorize external credentials, database/graph provisioning, schema application, institutional data import, live connectors, network mutation, public deployment, certificate/trust selection, or organization-specific policy content.

## Acceptance gate

1. Repository-default and incomplete profiles remain unconfigured and fail closed without PostgreSQL, OPA, or Neo4j access; the default disconnected-dependency count remains 142.
2. The tenant-scoped Enterprise Context record and its cryptographic/storage bindings are revalidated before OPA; OPA permit and exact Existing Systems scope occur before inventory access.
3. OPA permit establishes non-empty exact system identifiers, relationship types, source kind `enterprise-graph`, classification ceiling, required roles, and bounded results; denial carries no operational scope.
4. The Neo4j query is fixed, read-only, parameterized, tenant scoped, exact-system scoped, relationship scoped, source scoped, classification bounded, and result bounded before retrieval.
5. Every Enterprise Object and relationship is structurally checked; confirmed relationships require evidence; any invalid or out-of-scope result fails the request.
6. Every system and relationship is re-authorized using the authenticated identity and OPA-bound scope before release; denial or mismatch fails the complete snapshot.
7. Inventory evidence is deterministic, immutable, tenant scoped, idempotent, and atomically recorded with a SHA-256-qualified evidence reference bound to the exact context and policy decision.
8. Unavailability, timeout, cancellation, denial, mismatch, malformed data, excessive scope, or ambiguous persistence cannot return a released inventory.
9. No secret, credential, connection detail, raw query parameter, raw policy input, unauthorized Graph property, command, session, or external effect is committed, logged, or disclosed.
10. `IAuthorizedExistingSystemsSnapshotReader`, Existing Architecture, every later station, Enterprise Model mutation, live connector, network probe, AI call, workflow advancement, and production action remain unavailable.
11. Existing 30 phase, 22 increment, and Waves 01–03 gates remain satisfied.
12. All 15 projects build with zero warnings and zero errors, and runtime verification proves safe behavior in the unconfigured repository profile.
