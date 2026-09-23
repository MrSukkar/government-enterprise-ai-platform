# CR-039 — Integration Demo Stage 17: Governed Sovereign Deployment

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 18, `docs/change-control/CR-018-OPERATIONALIZATION-WAVE-17.md`, `docs/change-control/CR-001-AMENDMENT-15-DEPLOYMENT.md`, and `docs/demo/INTEGRATION_DEMO_STAGE_ROADMAP.md`.

Preceding accepted gate: `docs/demo/STAGE_16_GOVERNED_ARTIFACT_ACCEPTANCE.md`.

## Decision

Connect only the Stage 17 localhost Integration Demo boundary for Governed Sovereign Deployment. Exact accepted Artifact evidence and the `Artifact`-stopped run may reach the existing Deployment boundary only after signed exact-action OPA authorization, explicit human approval, exact signed sovereign profile, and successful institutional preflight. A real signed provider-neutral gateway may perform one idempotent synthetic non-production deployment only.

## Approved scope

1. Add signed OPA permit/deny scope for `internal-service.deployment.execute` and `deployment-result` only.
2. Bind exact Artifact evidence, profile, air-gapped topology, target, human approval, workload identity, secrets and rollback policies, tenant, purpose, environment, classification, and evidence.
3. Reuse existing readers, profile validation, preflight, `IInstitutionalSovereignDeploymentGateway`, result authorization, and append-only PostgreSQL evidence.
4. Add migrations 027 and 028 while keeping runtime inputs, gateway, approval, credentials, secrets, trust, private material, deployment, and effects outside Git.
5. Expose exact deployment inputs without defaulting human approval or fabricating success.

No fake approval, profile, preflight, runtime, activation, rollback, gateway response, or success; no public or production deployment, telemetry, registration, Enterprise Model mutation, workflow advancement, architectural deviation, or Phase 31 is introduced. Defaults remain unconfigured and fail closed. Data is synthetic only.

## Acceptance authority

Stage 17 requires independent acceptance, full verification, immutable commit, PR, and green CI. Stage 18 remains separately governed and unauthorized.
