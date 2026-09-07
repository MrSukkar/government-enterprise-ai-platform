# Operationalization Wave 04 Acceptance

Status: **Satisfied**

Change control: `docs/change-control/CR-005-OPERATIONALIZATION-WAVE-04.md`

## Acceptance evidence

- CR-005 is explicitly approved by the repository owner and recorded in the Master Specification.
- No new package or technology is introduced; locked `Npgsql` `10.0.3` and `Neo4j.Driver` `6.3.0` are reused.
- Repository-default policy, PostgreSQL, and Neo4j profiles remain empty and all 142 institutional runtime dependencies remain disconnected and fail closed.
- The tenant-scoped Enterprise Context reader uses fixed parameterized SQL and validates the complete stored record, record SHA-256, evidence-reference digest, payload digest, identities, scope, evidence, and time before OPA.
- Signed bundle verification precedes the exact `internal-service.existing-systems.discover` OPA evaluation.
- The typed Existing Systems scope is distinct from Intent and Enterprise Context scopes and requires exact system identifiers, relationship types, source kind `enterprise-graph`, classification ceiling, required roles, and bounded results. Denial carries no scope.
- The fixed read-only Cypher begins from exact authorized system identifiers and tenant and applies classification, source, relationship, target-system, and result bounds as parameters before records are returned.
- Only approved Enterprise Object and relationship fields are mapped; malformed, duplicate, secret-bearing, session-bearing, command-bearing, effect-bearing, excessive, or out-of-scope results fail the complete request.
- Every system and relationship is re-authorized by the existing deterministic RBAC/ABAC evaluator using the authenticated identity and OPA-bound roles and scope.
- Inventory evidence uses an explicit non-startup PostgreSQL migration, immutable tenant-scoped rows, fixed parameterized SQL, explicit transactions, exact conflict validation, and SHA-256-qualified evidence references.
- The exact context reader, Existing Systems policy gate, Graph source, result authorizer, and evidence recorder are registered only under complete policy/PostgreSQL/Neo4j profiles.
- Readiness exposes only combined Existing Systems state and never configuration values.
- `IAuthorizedExistingSystemsSnapshotReader`, Existing Architecture, live connectors, Graph mutation, network probes, later stations, workflow advancement, and production action remain unavailable.
- Existing 30 phase, 22 operational-increment, and Waves 01–03 gates remain unchanged.
- All 15 projects build with zero warnings and zero errors.

## Verification result

`scripts/verify-project.ps1` verifies the historical acceptance chain, CR-005 approval, prerequisite proof validation, action-specific OPA scope, policy-before-source ordering, fixed scope-first parameterized Cypher, per-result RBAC/ABAC, PostgreSQL evidence atomicity, safe conditional composition, build, repository-default runtime posture, readiness, and OpenAPI contract.
