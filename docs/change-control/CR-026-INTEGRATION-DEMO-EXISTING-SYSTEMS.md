# CR-026 — Integration Demo Stage 04: Existing Systems Discovery

Status: **Approved for bounded implementation**

Authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

Preceding gate: `docs/demo/STAGE_03_AUTHORIZED_ENTERPRISE_CONTEXT_ACCEPTANCE.md`

## Outcome

Demonstrate the existing Authorized Existing Systems station using only the exact evidence-bearing Enterprise Context snapshot, a signed OPA scope, synthetic read-only Neo4j inventory, and immutable PostgreSQL evidence.

## Invariants

1. The persisted Enterprise Context snapshot is validated before OPA and graph access.
2. OPA permits exactly two UUID-identified synthetic systems, `DependsOn`, `enterprise-graph`, `Developer`, `Internal`, and at most two results.
3. Every system and relationship is re-authorized; the third synthetic system remains out of scope.
4. `CanAdvance` remains false; no live connector, graph mutation, network probe, Existing Architecture, AI, workflow advancement, production action, architectural deviation, or Phase 31 is introduced.
5. Secrets, keys, tokens, bundles, certificates, passwords, logs, and volumes remain outside Git.

Approved by the repository owner on 2026-09-18 for implementation, local verification, source control, GitHub synchronization, and CI only.
