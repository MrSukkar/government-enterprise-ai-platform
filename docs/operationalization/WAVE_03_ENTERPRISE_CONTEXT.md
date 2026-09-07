# Operationalization Wave 03 — Authorized Enterprise Context Runtime

Status: **Implemented under approved CR-004**

## Operational boundary

This wave connects only Authorized Enterprise Context Discovery. It reads the exact Wave 02 governed intent, authorizes retrieval through the sovereign signed-policy/OPA boundary, reads only explicitly authorized Enterprise Graph objects, re-authorizes every candidate, and atomically records the released context evidence. It does not connect the Existing Systems stage or its context-snapshot reader.

All exact adapters are composed only when the policy, PostgreSQL, and Neo4j profiles are valid. Authentication remains independently configuration gated. Repository defaults contain no endpoint, connection, credential, certificate, trust material, or institutional data and remain fail closed.

## Neo4j configuration

Configuration section: `Neo4jEnterpriseGraph`.

- `Uri`: an encrypted, certificate-verified `neo4j+s` or `bolt+s` URI with no embedded user information, query, fragment, or nested path.
- `Database`: explicit deployment database.
- `Username` and `Password`: deployment-injected workload credential; the checked-in values are empty.
- `QueryTimeoutSeconds`: positive deployment-selected query bound.
- `MaximumRecords`: positive deployment-selected upper bound that OPA may not exceed.

`configured` readiness means only that the local configuration contract is complete; it does not claim connectivity or deployment acceptance.

## Enterprise Graph read contract

The Wave expects `EnterpriseObject` nodes with these approved context projections:

- `tenantId`, `resourceId`, `classification`, and numeric `classificationRank`;
- `contextContent` and numeric `contextRelevance` between zero and one;
- `source` and a non-empty `evidenceReferences` list.

The fixed Cypher query begins by unwinding OPA-authorized resource identifiers, matches by tenant and exact resource identity, applies the classification ceiling, orders deterministically, and applies the authorized result limit. Every value is a query parameter. No dynamic Cypher, unrestricted traversal, graph write, inference, or retrieve-then-filter path exists.

An authorized deployment operator is responsible for the graph schema, indexes/constraints, least-privilege read role, certificate trust, and data-loading evidence. The application does not create or mutate graph schema.

## Policy and authorization sequence

`Authenticated request -> local RBAC/ABAC -> tenant-scoped PostgreSQL intent read -> exact prerequisite validation -> signed bundle verification -> exact OPA scope -> scope-first Neo4j read -> source-scope validation -> per-result RBAC/ABAC -> deterministic context digest -> atomic PostgreSQL evidence`

OPA `Permit` must return exactly Graph modality, a non-empty unique resource set, a valid classification ceiling, role set, and positive result bound. Denial must not return a scope. Invalid or excessive scope fails before a Neo4j session is opened.

## Evidence persistence

The explicit migration is `backend/Platform.SoftwareFactory/Persistence/Migrations/002_enterprise_context_evidence.sql`. It is never run at application startup. An authorized database operator applies it after Wave 02 migration through deployment change control.

The recorder writes the exact deterministic context record as JSONB together with its SHA-256 digest, context digest, tenant/discovery identity, source evidence, timestamp, and SHA-256-qualified evidence reference in one transaction. Conflicts are returned only when the complete stored digest and receipt match; no update or delete path exists.

Live OPA, PostgreSQL, Neo4j, graph-data, schema, credential, and certificate integration remains deployment-controlled acceptance.
