# CR-025 — Integration Demo Stage 03: Authorized Enterprise Context

Status: **Approved for bounded implementation**

Authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

Preceding gate: `docs/demo/STAGE_02_GOVERNED_INTENT_REGISTRATION_ACCEPTANCE.md`

## Outcome

Demonstrate the existing Authorized Enterprise Context station after an exact persisted Governed Intent using a signed OPA bundle, synthetic read-only Neo4j graph, and immutable PostgreSQL evidence.

## Approved scope

- Localhost-only OPA `1.20.2-static`, PostgreSQL `18.6-bookworm`, and Neo4j Community `2025.10`, all pinned by official image digest.
- Signed policy verification before graph access; only the exact synthetic tenant, purpose, action, registration version, Intent digest, bundle identity, and `Internal` classification may permit.
- Neo4j is read-only from the application, receives fixed parameterized tenant/resource/classification/result bounds, and contains synthetic data only.
- Every released result is re-authorized by the existing retrieval boundary; the PostgreSQL evidence record is immutable.
- The existing UI may request and display an authorized-context receipt only after a persisted registration.

## Invariants

1. A valid persisted registration is a prerequisite; OPA permit and scope occur before graph access.
2. The permitted scope contains Graph only, two fixed resource identifiers, `Internal` maximum classification, and a result bound of two.
3. A seeded out-of-scope graph object must never be released.
4. Denial returns no scope and creates no Enterprise Context evidence.
5. `CanAdvance` remains `false`; Existing Systems, AI, workflow progression, production action, and Phase 31 remain disconnected.
6. Secrets, tokens, private keys, bundles, TLS private keys, passwords, logs, and volumes remain outside Git.

## Image decision

- `openpolicyagent/opa:1.20.2-static@sha256:bb245e9e36be0d0ed486c240b606c56be7aba96014a4a87895fed4ba7a6dfa8d`
- `postgres:18.6-bookworm@sha256:1c59e2c3c818eaa0f0628f695b36e7c9e362d6b219b36a54a32df645cbd7e1af`
- `neo4j:2025.10-community@sha256:155c8aad10d5c838bc3bbc476c0418779086547822acb214ec5e3d49ba336907`

## Decision

Approved by the repository owner on 2026-09-18 for implementation, local verification, source control, GitHub synchronization, and CI only.
