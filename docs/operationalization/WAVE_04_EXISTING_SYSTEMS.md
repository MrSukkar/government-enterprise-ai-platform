# Operationalization Wave 04 — Authorized Existing Systems Runtime

Status: **Implemented under approved CR-005**

## Operational boundary

This wave connects only Authorized Existing Systems Discovery. It cryptographically revalidates the exact tenant-scoped Enterprise Context evidence produced by Wave 03, obtains an action-specific signed-policy/OPA scope before inventory access, reads only exact authorized systems and relationships from the Enterprise Graph, re-authorizes every result through the existing RBAC/ABAC boundary, and atomically records the deterministic inventory evidence. It does not connect the Existing Systems snapshot reader consumed by Existing Architecture.

All five exact adapters are composed only when the policy, PostgreSQL, and Neo4j profiles are valid. Repository defaults contain no endpoint, connection, credential, certificate, trust material, or institutional data and remain fail closed.

## Prerequisite integrity

`PostgreSqlAuthorizedEnterpriseContextSnapshotReader` uses the exact tenant and context-discovery key with fixed parameterized SQL and a bounded command. Before release it revalidates the stored record JSON, stored SHA-256 digest, SHA-256-qualified evidence reference, registration identity/version, tenant, intent and context digests, policy binding, evidence arrays, recorded time, and the deterministic Enterprise Context payload digest.

## Policy and authorization sequence

`Authenticated request -> local RBAC/ABAC -> exact PostgreSQL context read and proof validation -> signed bundle verification -> action-specific Existing Systems OPA scope -> scope-first Neo4j read -> structural validation -> per-system/per-relationship RBAC/ABAC -> deterministic inventory digest -> atomic PostgreSQL evidence`

OPA permit must return only the distinct Existing Systems scope: maximum classification, non-empty exact system identifiers, non-empty relationship types, source kind `enterprise-graph`, non-empty required roles, and a positive bounded result count. A denied decision cannot carry scope, and an Existing Systems decision cannot substitute the Intent or Enterprise Context scope.

## Enterprise Graph read contract

The adapter reuses the Wave 03 encrypted read-only driver and profile. Its fixed Cypher begins from the exact OPA-authorized system identifiers and tenant, applies classification and source bounds, and restricts relationships to authorized target systems and types before returning records. All values are parameters. Only approved Enterprise Object and relationship projections are mapped; credential, live-session, executable-command, or external-effect flags cause the entire result to fail.

The connected source kind is exactly `enterprise-graph`. No dynamic Cypher, unrestricted traversal, Graph mutation, network probe, live connector, vector/lexical retrieval, inference, or AI-generated query is available.

## Evidence persistence

The explicit migration is `backend/Platform.SoftwareFactory/Persistence/Migrations/003_existing_systems_evidence.sql`. It is never run at application startup. An authorized database operator applies it after the Wave 03 migration through deployment change control.

The recorder writes the exact deterministic inventory record as JSONB together with its SHA-256 digest, inventory digest, tenant/context/registration identities, source and authorization evidence, timestamp, and SHA-256-qualified evidence reference in one explicit transaction. Conflicts are accepted only when the complete stored identity and digest match. No update, delete, or automatic retry after ambiguous mutation exists.

Live OPA, PostgreSQL, Neo4j, schema, graph-data, credential, certificate, and organizational policy integration remains deployment-controlled acceptance. Existing Architecture and every later station remain disconnected.
