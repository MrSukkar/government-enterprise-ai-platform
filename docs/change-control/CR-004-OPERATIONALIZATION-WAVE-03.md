# CR-004 — Operationalization Wave 03: Authorized Enterprise Context Runtime

Status: **Approved for Operationalization Wave 03**

Preceding completed authority: `docs/change-control/CR-003-OPERATIONALIZATION-WAVE-02.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Operationalize the second station of the approved Create Internal Service path by connecting Authorized Enterprise Context Discovery to the Wave 02 governed-intent record, the sovereign OPA control plane, the approved Neo4j Enterprise Graph baseline, and atomic PostgreSQL evidence persistence.

Wave name: **Operationalization Wave 03 — Authorized Enterprise Context Runtime**.

### Outcome

When—and only when—deployment-approved identity, policy, PostgreSQL, and Neo4j profiles are valid, an authorized caller can request Enterprise Context for an exact registered intent. The platform reads that tenant-scoped prerequisite, obtains signed-bundle-bound OPA scope before graph access, executes a parameterized scope-first graph query, re-authorizes every candidate through the existing Knowledge boundary, and atomically records the released deterministic snapshot evidence.

Missing or invalid configuration, prerequisite mismatch, policy denial, empty or excessive scope, unavailable source, out-of-scope result, authorization rejection, malformed graph data, or evidence failure prevents context release and fails closed.

## 2. Current-state evidence

- All 30 phases, CR-001 Operational Increments 01–22, and Operationalization Waves 01–02 are complete and verified.
- Wave 02 supplies the atomic PostgreSQL governed-intent record but deliberately leaves `IGovernedIntentRegistrationReader` disconnected.
- `IEnterpriseContextPolicyGate`, `IKnowledgeRetrievalSource`, and `IEnterpriseContextEvidenceRecorder` remain disconnected.
- `AuthorizedEnterpriseContextDiscoveryEngine` already enforces exact registration version/digest/tenant/purpose/classification, signed policy identity, explicit resource/modality/result scope, policy-before-retrieval ordering, per-candidate re-authorization, deterministic context hashing, evidence receipt validation, and `CanAdvance: false`.
- `AuthorizedKnowledgeRetriever` already authorizes before source access, sends only the authorized scope to selected sources, rejects out-of-scope source results, deterministically fuses results, and re-authorizes every candidate before release.
- Neo4j is the approved Enterprise Graph and retrieval baseline; pgvector and Qdrant remain conditional and unapproved.
- Repository-default runtime exposes 142 disconnected institutional dependencies and remains fail closed.

## 3. Impact Analysis

### In scope

- Extend the Wave 02 PostgreSQL repository with read-only `IGovernedIntentRegistrationReader` behavior using the same tenant-scoped schema and bounded parameterized commands.
- Extend the sovereign typed OPA response boundary with an explicit, bounded Enterprise Context scope: maximum classification, exact allowed resource identifiers, Graph modality, required roles, and maximum result count.
- Implement `IEnterpriseContextPolicyGate` with signed bundle verification before exact OPA evaluation and strict revalidation of every returned scope field.
- Add the official `Neo4j.Driver` package pinned and locked at `6.3.0`, which supports .NET 10 and current supported Neo4j server generations.
- A configuration-gated sovereign Neo4j profile requiring encrypted certificate-verified transport, explicit database and workload identity, deployment-injected credential, positive query timeout, and positive maximum record bound.
- Implement only the Graph `IKnowledgeRetrievalSource`; Vector and Lexical sources remain disconnected.
- Use a fixed, read-only, parameterized Cypher query that includes tenant, exact authorized resource identifiers, classification ceiling, and record limit before data is returned. No unrestricted graph scan or “retrieve then filter” path is permitted.
- Validate every graph record’s resource identity, tenant, classification, modality, relevance, content, source, and evidence before returning it to the existing re-authorization layer.
- Add a versioned PostgreSQL migration and `IEnterpriseContextEvidenceRecorder` adapter for immutable, idempotent, tenant-scoped context snapshots and SHA-256-qualified evidence references.
- Register the four exact runtime contracts only when policy, PostgreSQL, and Neo4j profiles are all valid.
- Readiness disclosure for Neo4j and Enterprise Context persistence configuration states without revealing endpoints, database names, users, credentials, queries, or trust material.
- Deterministic contract/profile verification for policy-before-source ordering, scope-first query construction, parameterization, tenant isolation, bounded results, out-of-scope rejection, evidence atomicity, unavailability, and repository-default fail-closed behavior.
- Updated OpenAPI descriptions, UI status, operational guidance, acceptance evidence, and project state.

### Out of scope

- Provisioning Neo4j or PostgreSQL, applying schema to an external environment, creating graph indexes/constraints, importing institutional data, acquiring credentials, choosing a secret manager, or selecting certificates.
- Graph writes, Enterprise Model mutation, inferred relationships, autonomous discovery, unrestricted traversal, dynamic Cypher, or query generation by AI.
- Vector or lexical retrieval implementations, pgvector, Qdrant, embeddings, reranker changes, or any conditional technology decision.
- `IAuthorizedEnterpriseContextSnapshotReader`, Existing Systems discovery, architecture discovery, AI planning, later vertical-slice adapters, workflow advancement, or production action.
- Returning raw graph properties outside the approved context item contract or storing raw OPA input/credentials in evidence.
- Public deployment, external account creation, network mutation, a new service boundary, phase, database decision, queue, cache, mandatory SaaS dependency, or numerical SLO.

### Affected boundaries

- `Platform.SoftwareFactory` owns the registered-intent reader, Enterprise Context policy adapter, context evidence adapter, and orchestration semantics.
- `Platform.Knowledge` owns the Neo4j Graph retrieval adapter and retains pre-access and per-result authorization.
- `Platform.Governance` owns the bounded sovereign typed-policy transport extension only.
- `Platform.Api` composes valid adapters and exposes non-sensitive readiness.
- Neo4j is a read-only Enterprise Graph source in this wave and gains no policy, workflow, or mutation authority.

## 4. Data and security review

Required invariants:

1. The governed-intent read is tenant scoped and must exactly match the request before policy evaluation.
2. Signed policy-bundle verification and an exact OPA permit establishing non-empty bounded scope precede any Neo4j session or query.
3. Cypher text is fixed; tenant, resource identifiers, classification ceiling, and limit are parameters. User-controlled identifiers or values are never concatenated into Cypher.
4. The query starts from the exact authorized resource identifiers and tenant; it cannot scan the graph and filter later.
5. Only `RetrievalModality.Graph` is connected, and a policy requesting unavailable modalities fails before source access.
6. Returned records outside tenant, resource, classification, modality, record-count, or structural scope fail the entire request rather than being silently released.
7. The existing RBAC/ABAC evaluator re-authorizes every candidate before context release.
8. The exact deterministic context digest, authorized items, policy evidence, and immutable evidence reference are committed atomically and idempotently under tenant scope.
9. PostgreSQL and Neo4j credentials, connection strings, endpoints, query parameters, raw governed content, and sensitive policy input are not logged or exposed through readiness.
10. Driver-managed automatic transaction retries are not used for evidence writes; ambiguous persistence outcomes cannot produce a success receipt or duplicate mutation.

## 5. Proposed implementation sequence

1. Define an empty, validated Neo4j operational profile and non-sensitive readiness state.
2. Add and lock `Neo4j.Driver` 6.3.0 without adding vector, ORM, or graph-mapping packages.
3. Extend the bounded typed OPA result with the exact Enterprise Context scope contract.
4. Implement the Enterprise Context signed-policy/OPA adapter.
5. Extend PostgreSQL governed-intent persistence with the tenant-scoped prerequisite reader.
6. Implement the fixed scope-first Neo4j Graph retrieval source.
7. Add the explicit context-evidence migration and atomic PostgreSQL recorder.
8. Compose the four exact contracts only under complete valid policy/database/graph profiles.
9. Update readiness, OpenAPI, UI, documentation, acceptance, and project state.
10. Run the full project verifier and commit/push only verified source.

## 6. Architectural Review

Finding: **Conforms without architectural deviation, subject to explicit approval and implementation verification**.

- Neo4j is already the approved Enterprise Graph and retrieval baseline.
- The existing authorization sequence is preserved: identity and purpose, OPA scope, source access, result validation, per-result authorization, then context evidence.
- OPA remains policy authority only; Neo4j remains a scoped read source; neither gains workflow authority.
- PostgreSQL remains the primary transactional/evidence store, and no conditional vector technology is selected.
- The change activates existing contracts within the Modular Monolith and remains sovereign/on-premises/air-gapped compatible.

## 7. Package decision proposed

Approve the official `Neo4j.Driver` package at version `6.3.0`. No Object Graph Mapper, vector driver, embedding package, pgvector, or Qdrant package is proposed.

## 8. Decision requested

Approve **CR-004 — Operationalization Wave 03: Authorized Enterprise Context Runtime** for bounded implementation, local verification, source control, and GitHub synchronization only.

Decision: **Approved by the repository owner for Operationalization Wave 03**.

Repository-owner decision: **Approved on 2026-09-07**.

No external credential, database/graph provisioning, schema application, institutional data import, public deployment, certificate/trust selection, or organization-specific policy content is authorized.

## Acceptance gate

1. Repository-default and incomplete profiles remain unconfigured and fail closed without PostgreSQL or Neo4j access.
2. Exact tenant-scoped registered-intent validation occurs before OPA; OPA scope occurs before Graph access.
3. OPA permit must establish exact non-empty resource scope, Graph modality, classification ceiling, role requirements, and bounded results.
4. The Neo4j query is fixed, read-only, parameterized, tenant scoped, resource scoped, classification bounded, and result bounded before retrieval.
5. Every result is structurally checked and re-authorized; one out-of-scope record fails the request.
6. Context evidence is deterministic, immutable, tenant scoped, idempotent, and atomically recorded with a SHA-256-qualified evidence reference.
7. Unavailability, timeout, denial, mismatch, malformed data, excessive scope, or ambiguous persistence cannot return a released context.
8. No secret, connection detail, raw parameter, raw policy input, or unauthorized graph property is committed, logged, or disclosed.
9. Existing 30 phase, 22 increment, and Waves 01–02 gates remain satisfied.
10. All 15 projects build with zero warnings and zero errors.
11. Runtime verification proves safe behavior in the unconfigured repository profile.
