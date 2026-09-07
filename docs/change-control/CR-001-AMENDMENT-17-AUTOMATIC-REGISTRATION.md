# CR-001 Amendment 17 — Governed Automatic Registration

Status: **Approved for Operational Increment 20**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-16-OPENTELEMETRY.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded increment covering only `OpenTelemetry -> Automatic Registration`.

Increment name: **Operational Increment 20 — Governed Automatic Registration**. This amendment does not create Phase 31.

### Product outcome

An authorized operator can register the exact deployed and observed service from an accepted OpenTelemetry receipt. Signed OPA authorization precedes every receipt, delivery-run, or manifest read. A deployment-controlled, signed registration manifest binds the stable service key, runtime, Artifact, owner, classification, policies, permitted actions, relationships, and complete evidence chain. The existing Phase 17 `AutomaticRegistrationEngine` performs the sole atomic, deterministic persistence operation and validates the returned Enterprise Object. Result authorization and cryptographic evidence precede disclosure. The delivery run is not advanced and Enterprise Model contextualization or Evidence completion remains separately governed.

## 2. Impact Analysis

### In scope

- Protected REST registration after accepted OpenTelemetry activation.
- Exact tenant, environment, service identity/version, runtime, Artifact, activation, manifest, owner, classification, policies, permitted actions, relationships, and evidence.
- Signed OPA authorization before prerequisite reads or persistence.
- Delivery run stopped exactly at `OpenTelemetry`.
- Signed deployment-controlled registration manifest and accepted traces, metrics, and logs.
- Existing deterministic fingerprint and atomic Phase 17 registration repository.
- Result authorization, cryptographic evidence, OpenAPI 3.1, Blazor, readiness, acceptance, and verification.
- Missing adapters fail closed before reads or mutation.

### Out of scope

- Delivery-run advancement, Enterprise Model contextualization, Evidence completion, understanding, inference, impact analysis, or autonomous action.
- Caller-supplied arbitrary relationships, policies, actions, endpoints, secrets, or discovered facts outside the signed manifest.
- AI policy, persistence, registration, workflow, or production authority.
- New database, service, package, queue, external control plane, conditional technology, or numerical SLO.
- Real institutional mutation without configured adapters and authority.

### Affected boundaries

- `Platform.SoftwareFactory/InternalService` binds the vertical slice to the Phase 17 registration foundation.
- `Platform.EnterpriseModel/Registration` remains the sole registration and atomic repository boundary.
- Identity and OPA retain authorization; Evidence retains cryptographic evidence authority; Delivery retains workflow authority.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to implementation verification**.

- It reuses the approved Phase 17 engine and repository rather than creating another registry.
- OPA authorizes before access; AI has no authority.
- Registration remains deterministic, atomic, evidence-backed, tenant-scoped, and sovereign-compatible.
- Later stations remain unavailable and no numerical SLO is invented.

## 4. Decision

Approve **Operational Increment 20 — Governed Automatic Registration** only, including implementation, verification, source control, and synchronization. No real external registration or later station is authorized.

Decision: **Approved by the repository owner for Operational Increment 20**.

## 5. Master Specification Update

Append upon implementation verification:

> Operational Increment 20 is **Governed Automatic Registration**. It defines protected deterministic registration of the exact deployed and observed service from an accepted OpenTelemetry receipt and a delivery run stopped at `OpenTelemetry`. Verified OPA authorizes the exact activation, runtime, Artifact, service key, signed registration manifest, owner, classification, policies, permitted actions, relationships, tenant, purpose, environment, and evidence before reads or mutation. The existing Phase 17 engine performs one atomic evidence-backed registration and validates the returned Enterprise Object. Result authorization and cryptographic evidence are mandatory. No workflow advancement, Enterprise Model contextualization, Evidence completion, understanding, inference, or autonomous action is available.

The constitutional architecture and fixed roadmap remain unchanged.

## 6. Approval

- Product: Create Internal Service Workspace.
- Increment: 20 — Governed Automatic Registration.
- Authorization: bounded contract implementation, verification, source control, and synchronization only.

Approval state: **Approved**.

Repository-owner decision: **Approved on 2026-09-07**.

## Acceptance criteria

1. Exact governed identity, permission, activation, run, manifest, service key, runtime, Artifact, owner, classification, policies, actions, relationships, purpose, environment, and evidence are required.
2. Signed OPA authorization precedes all reads and mutation.
3. OpenTelemetry receipt is accepted, exact, evidence-bearing, and complete for traces, metrics, and logs; the run is stopped at `DeliveryStage.OpenTelemetry`.
4. The signed deployment-controlled manifest exactly binds the registration and evidence chain.
5. The existing Phase 17 engine and atomic repository are the sole mutation boundary.
6. Repository output, result authorization, and cryptographic evidence are validated before disclosure.
7. No workflow advancement or later station occurs.
8. Missing dependencies fail closed before reads or mutation.
9. OpenAPI and Blazor expose the boundary without claiming operational readiness.
10. All previous gates remain satisfied; all 15 projects build without warnings/errors; runtime remains fail closed.
