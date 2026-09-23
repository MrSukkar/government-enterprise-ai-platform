# Integration Demo Stage 17 — Governed Sovereign Deployment Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-039-INTEGRATION-DEMO-GOVERNED-DEPLOYMENT.md`

Preceding gate: `docs/demo/STAGE_16_GOVERNED_ARTIFACT_ACCEPTANCE.md`

## Verification record

All checks passed: exact accepted Artifact evidence and `Artifact`-stopped run; signed permit/deny OPA scope; explicit human approval; exact signed air-gapped profile with all local dependencies; institutional preflight; provider-neutral signed non-production gateway; idempotent runtime, activation, rollback, and effect proof; result authorization; fail-closed defaults; append-only PostgreSQL evidence; Compose; OPA bundle validation; and 15-project 0/0 build.

No fake approval, profile, preflight, runtime, activation, rollback, gateway response, private key, credential, secret, trust material, endpoint, institutional data, deployment, production effect, or fabricated success is included. Missing or mismatched evidence, approval, profile, dependency, preflight, signature, trust, or gateway fails closed.

The receipt records `DeploymentOccurred: true`, `ExternalEffectOccurred: true`, `ProductionEffectOccurred: false`, `TelemetryConfigured: false`, `AutomaticRegistrationOccurred: false`, `EnterpriseModelMutated: false`, and `CanAdvance: false`. OpenTelemetry and all later stages remain outside this gate.
