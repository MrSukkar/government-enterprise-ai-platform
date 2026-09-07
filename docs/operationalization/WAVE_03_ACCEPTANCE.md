# Operationalization Wave 03 Acceptance

Status: **Satisfied**

Change control: `docs/change-control/CR-004-OPERATIONALIZATION-WAVE-03.md`

## Acceptance evidence

- CR-004 is explicitly approved by the repository owner.
- The official Neo4j .NET driver is pinned and locked at `6.3.0`; no OGM, vector, embedding, pgvector, or Qdrant package is introduced.
- Repository-default Neo4j configuration is empty, creates no driver, contains no credential, and reports `unconfigured`.
- Valid Neo4j configuration requires certificate-verified encrypted URI, explicit database and identity, deployment-injected credential, positive query timeout, and positive maximum record bound.
- The Wave 02 PostgreSQL adapter now supplies a separate tenant-scoped, read-only registered-intent contract without weakening atomic registration.
- Signed bundle verification precedes typed Enterprise Context OPA evaluation.
- OPA response scope is explicit and bounded; only Graph modality is accepted, and denial cannot carry retrieval scope.
- The fixed read-only Cypher query begins from authorized resource identifiers and includes tenant, classification, and result bounds as parameters before retrieval.
- Neo4j receives no write, dynamic Cypher, unrestricted traversal, vector, lexical, inference, or AI-generated query capability.
- Every graph record is structurally validated by the source and scope validated plus re-authorized by the existing Knowledge boundary.
- Neo4j unavailability is converted to the existing generic Enterprise Context dependency failure.
- Context evidence uses an explicit non-startup PostgreSQL migration, immutable tenant-scoped rows, fixed parameterized SQL, explicit transactions, exact conflict validation, and SHA-256-qualified evidence references.
- The exact registered-intent reader, Enterprise Context policy gate, Graph source, and evidence recorder are registered only under complete policy/PostgreSQL/Neo4j profiles.
- Readiness exposes only Neo4j and combined Enterprise Context states, never configuration values.
- Repository-default runtime keeps all 142 disconnected institutional dependencies fail closed.
- Live provider, OPA, PostgreSQL, Neo4j, schemas, graph data, credentials, PKI, and trust integration remains deployment-controlled acceptance.
- Existing 30 phase, 22 operational-increment, and Waves 01–02 gates remain unchanged.
- All 15 projects build with zero warnings and zero errors.

## Verification result

`scripts/verify-project.ps1` verifies the historical acceptance chain, approved and locked driver, prerequisite/policy/source/evidence ordering, scope-first parameterized Cypher, PostgreSQL evidence atomicity, safe composition, build, runtime posture, readiness, and OpenAPI contract.
