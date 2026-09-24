# CR-041 — Integration Demo Stage 19: Governed Automatic Registration

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 20, `docs/change-control/CR-020-OPERATIONALIZATION-WAVE-19.md`, `docs/change-control/CR-001-AMENDMENT-17-AUTOMATIC-REGISTRATION.md`, and the Integration Demo roadmap.

Preceding accepted gate: `docs/demo/STAGE_18_GOVERNED_OPENTELEMETRY_ACCEPTANCE.md`.

## Decision

Connect only the Stage 19 localhost boundary for Governed Automatic Registration. Exact accepted OpenTelemetry evidence and the `OpenTelemetry`-stopped run may reach the existing Phase 17 engine only after signed exact-action OPA authorization and verification of the exact signed deployment-controlled manifest.

## Approved scope

Add signed permit/deny scope for `internal-service.automatic-registration.execute`; bind the exact activation, run, manifest, service, runtime, Artifact and evidence; reuse the deterministic atomic registration repository, result authorization, and append-only evidence; mount migrations 031 and 032; expose exact inputs without fabricated success.

No workflow advancement, Enterprise Model contextualization, Evidence completion, understanding, inference, autonomous action, production effect, architectural deviation, or Phase 31 is introduced. Defaults remain unconfigured and fail closed. Data is synthetic only.

## Acceptance authority

Stage 19 requires independent acceptance, full verification, immutable commit, PR, and green CI. Stage 20 remains separately governed and unauthorized.
