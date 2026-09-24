# CR-042 — Integration Demo Stage 20: Governed Enterprise Model Contextualization

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 21, `docs/change-control/CR-021-OPERATIONALIZATION-WAVE-20.md`, `docs/change-control/CR-001-AMENDMENT-18-ENTERPRISE-MODEL.md`, and the Integration Demo roadmap.

Preceding accepted gate: `docs/demo/STAGE_19_GOVERNED_AUTOMATIC_REGISTRATION_ACCEPTANCE.md`.

## Decision

Connect only the Stage 20 localhost boundary for Governed Enterprise Model Contextualization. Exact accepted Automatic Registration evidence and the `AutomaticRegistration`-stopped run may reach the existing authorized exact-object reader only after signed exact-action OPA authorization. Returned object structure, relationships, policy, actions, lifecycle, tenant, classification, time, and evidence are revalidated and result-authorized.

## Approved scope

Add signed permit/deny scope for `internal-service.enterprise-model.contextualize`; bind exact registration, run, object, fingerprint and evidence; reuse the authorized reader, result authorization, and append-only evidence; mount migrations 033 and 034; expose exact inputs without fabricated success.

No new model mutation, graph traversal, impact analysis, simulation, inference, action, workflow advancement, Evidence completion, production effect, architectural deviation, or Phase 31 is introduced. Defaults remain unconfigured and fail closed. Data is synthetic only.

## Acceptance authority

Stage 20 requires independent acceptance, full verification, immutable commit, PR, and green CI. Stage 21 remains separately governed and unauthorized.
