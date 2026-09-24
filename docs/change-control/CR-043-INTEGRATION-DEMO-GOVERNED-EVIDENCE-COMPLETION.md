# CR-043 — Integration Demo Stage 21: Governed Evidence Completion

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 22, `docs/change-control/CR-022-OPERATIONALIZATION-WAVE-21.md`, `docs/change-control/CR-001-AMENDMENT-19-EVIDENCE-COMPLETION.md`, and the Integration Demo roadmap.

Preceding accepted gate: `docs/demo/STAGE_20_GOVERNED_ENTERPRISE_MODEL_ACCEPTANCE.md`.

## Decision

Connect only the terminal Stage 21 localhost boundary for Governed Evidence Completion. Exact accepted Enterprise Model contextualization and the `EnterpriseModel`-stopped run may reach the existing Phase 30 engine only after signed exact-action OPA authorization. Independent append/read/classification authorization, atomic append-only persistence, sovereign signing, all ten ordered stages, hash links, signatures, scope, completeness, and independent result authorization are mandatory.

## Approved scope

Add signed permit/deny scope for `internal-service.evidence.complete`; bind exact contextualization, run, chain, correlation, payload, traces and evidence; reuse Phase 30 access authorization, store, signer, verifier and proof engine; mount migrations 035 and 036; expose exact inputs without fabricated success.

No update/deletion, repair, different stage, workflow advancement, model mutation, production action, post-Evidence station, architectural deviation, or Phase 31 is introduced. Defaults contain no signing keys and fail closed. Data is synthetic only.

## Acceptance authority

Stage 21 requires independent acceptance, full verification, immutable commit, PR, and green CI. Integration Demo closure remains a separate final acceptance gate.
