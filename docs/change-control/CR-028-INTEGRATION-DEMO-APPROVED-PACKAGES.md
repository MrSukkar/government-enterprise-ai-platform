# CR-028 — Integration Demo Stage 06: Approved Packages Selection

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`

Preceding gate: `docs/demo/STAGE_05_EXISTING_ARCHITECTURE_ACCEPTANCE.md`

## Outcome

Demonstrate only governed Approved Packages selection from the exact evidence-bearing Existing Architecture snapshot.

## Invariants

1. The signed localhost-only policy permits exactly two immutable synthetic package coordinates and requires the Stage 05 architecture prerequisite.
2. PostgreSQL is read-only for the institutional catalog; package records must be approved, current, tenant/environment scoped, sovereign, provenance/SBOM/signature attested, and re-authorized per result.
3. Selection evidence is append-only and `CanAdvance` remains `false`; AI Planning, transfer, installation, execution, workflow advancement, production action, and Phase 31 remain disconnected.
4. Repository defaults stay unconfigured and fail closed. Runtime credentials, keys, certificates, bundles, logs, and volumes remain outside Git.

Approved by the repository owner on 2026-09-19 for bounded implementation, local verification, source control, GitHub synchronization, and CI only.
