# Integration Demo Stage 18 — Governed OpenTelemetry Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-040-INTEGRATION-DEMO-GOVERNED-OPENTELEMETRY.md`

Preceding gate: `docs/demo/STAGE_17_GOVERNED_DEPLOYMENT_ACCEPTANCE.md`

## Verification record

All checks passed: exact accepted Deployment evidence and `Deployment`-stopped run; signed permit/deny OPA scope; immutable signed profile; exact workload/resource binding; traces, metrics, and logs; strict redaction; trusted local collector routing; provider-neutral signed gateway; result authorization; fail-closed defaults; append-only PostgreSQL evidence; Compose; OPA bundle validation; and 15-project 0/0 build.

No fake profile, collector, redaction decision, signal, gateway response, private key, credential, secret, trust material, endpoint, institutional data, telemetry, production effect, or fabricated success is included. Missing or mismatched evidence, profile, resource, signal, collector, redaction, signature, trust, or gateway fails closed.

The receipt records `TelemetryConfigured: true`, `ExternalEffectOccurred: true`, `ProductionEffectOccurred: false`, `AutomaticRegistrationOccurred: false`, `EnterpriseModelMutated: false`, and `CanAdvance: false`. Automatic Registration and all later stages remain outside this gate.
