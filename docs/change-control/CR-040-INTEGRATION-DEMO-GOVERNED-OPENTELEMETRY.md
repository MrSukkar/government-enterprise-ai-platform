# CR-040 — Integration Demo Stage 18: Governed OpenTelemetry

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 19, `docs/change-control/CR-019-OPERATIONALIZATION-WAVE-18.md`, `docs/change-control/CR-001-AMENDMENT-16-OPENTELEMETRY.md`, and `docs/demo/INTEGRATION_DEMO_STAGE_ROADMAP.md`.

Preceding accepted gate: `docs/demo/STAGE_17_GOVERNED_DEPLOYMENT_ACCEPTANCE.md`.

## Decision

Connect only the Stage 18 localhost Integration Demo boundary for Governed OpenTelemetry. Exact accepted Deployment evidence and the `Deployment`-stopped run may reach the existing telemetry boundary only after signed exact-action OPA authorization, exact signed profile verification, strict redaction verification, and trusted local collector routing. A real signed provider-neutral gateway may configure traces, metrics, and logs for the synthetic non-production workload only.

## Approved scope

1. Add signed OPA permit/deny scope for `internal-service.opentelemetry.activate` and `opentelemetry-result` only.
2. Bind exact Deployment evidence, runtime, Artifact digest, profile, service resource, all three signals, redaction policy, tenant, purpose, environment, classification, and evidence.
3. Reuse existing readers, profile and redaction validation, `IInstitutionalOpenTelemetryGateway`, result authorization, and append-only PostgreSQL evidence.
4. Add migrations 029 and 030 while keeping runtime inputs, collectors, gateway, credentials, secrets, trust, private material, telemetry, and effects outside Git.
5. Expose exact activation inputs without fabricating profile, collector, redaction, signal, or activation success.

No public exporter, mandatory external control plane, Automatic Registration, Enterprise Model mutation, workflow advancement, production effect, architectural deviation, or Phase 31 is introduced. Defaults remain unconfigured and fail closed. Data is synthetic only.

## Acceptance authority

Stage 18 requires independent acceptance, full verification, immutable commit, PR, and green CI. Stage 19 remains separately governed and unauthorized.
