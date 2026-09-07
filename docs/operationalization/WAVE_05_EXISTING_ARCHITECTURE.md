# Operationalization Wave 05 — Authorized Existing Architecture Runtime

Authority: approved `CR-006`; source of truth: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`.

This wave connects only Authorized Existing Architecture Discovery. It revalidates the exact tenant-scoped Existing Systems evidence, verifies the signed policy bundle, obtains the action-specific OPA architecture scope, validates that scope against the Master Specification, performs one fixed read-only scope-first Neo4j query, checks every approved active architecture item, re-authorizes every item through RBAC/ABAC, and atomically records immutable PostgreSQL evidence.

`Authenticated request -> exact PostgreSQL Existing Systems proof -> signed bundle -> architecture OPA scope -> constitutional scope validation -> fixed Neo4j read -> per-item conformance -> per-item RBAC/ABAC -> deterministic digest -> atomic PostgreSQL evidence`

The runtime adapters are registered only when policy, PostgreSQL, and Neo4j profiles are complete and valid. Repository defaults contain no endpoints, credentials, institutional data, trust material, or organization policy, so the readiness state is `unconfigured` and fail closed.

The Graph adapter accepts only source kind `enterprise-graph`, approved state, active lifecycle, exact tenant/environment/system/item/relationship scope, classification ceiling, and bounded results. It exposes no mutation API and performs no crawling, scanning, command, session, inference, redesign, approval, or external effect.

Migration `004_existing_architecture_evidence.sql` is deployment-controlled and is never applied automatically. The runtime does not retry ambiguous evidence mutations.

`IAuthorizedExistingArchitectureSnapshotReader`, Approved Packages, AI Planning, later stations, workflow advancement, Graph mutation, live infrastructure, and production action remain disconnected.
