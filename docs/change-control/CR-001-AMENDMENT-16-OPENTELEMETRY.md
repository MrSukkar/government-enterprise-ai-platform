# CR-001 Amendment 16 — Governed OpenTelemetry Activation

Status: **Approved for Operational Increment 19**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-15-DEPLOYMENT.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded increment covering only `Deployment -> OpenTelemetry`.

Increment name: **Operational Increment 19 — Governed OpenTelemetry Activation**. This amendment does not create Phase 31.

### Product outcome

An authorized operator can activate the exact approved OpenTelemetry profile for the exact governed Deployment from Increment 18. Verified OPA authorizes the Deployment receipt, runtime, Artifact and evidence, target and production-effect posture, telemetry profile identity/version/digest, service resource identity, required traces/metrics/logs signals, collector identity, redaction policy, tenant, purpose, classification, environment, and evidence before Deployment or profile read. A deterministic delivery run must be stopped exactly at `Deployment`. The profile and strict fail-closed redaction policy are independently verified before a vendor-neutral institutional gateway configures the deployed workload and proves exact resource binding, baggage clearing, sensitive-field dropping, unknown-field redaction, bounded strings, trusted collector routing, signal acceptance, and evidence. Result authorization and cryptographic evidence precede receipt release. No Automatic Registration, Enterprise Model mutation, or workflow advancement is available.

## 2. Impact Analysis

### In scope

- Protected REST activation after an accepted Deployment receipt.
- Governed identity, permission, exact Deployment/runtime/Artifact/effect posture, telemetry profile, resource identity, signals, collector, redaction, purpose, classification, environment, and evidence.
- Signed OPA authorization before prerequisite or telemetry-profile reads and before gateway invocation.
- Authoritative Deployment receipt and delivery-run readers; signed telemetry-profile reader.
- Exact traces, metrics, and logs coverage with unique signal results and evidence.
- Strict redaction verification: sensitive names dropped, unknown attributes redacted, retained strings bounded, baggage cleared at start/end, and only approved low-cardinality attributes retained.
- Trusted HTTPS collector agent and gateway references, trace-aware routing, tenant/classification isolation, sovereign or air-gapped local operation, and no mandatory external control plane.
- Vendor-neutral telemetry activation gateway returning exact deployment/runtime/resource/profile/collector binding and configuration proof.
- Result authorization and cryptographic evidence.
- Explicit telemetry configuration without registration, Enterprise Model mutation, or workflow advancement.
- OpenAPI 3.1, Blazor, readiness, acceptance, and non-regression verification.
- Missing dependencies fail closed before reads or telemetry configuration.

### Out of scope

- Automatic Registration, Enterprise Model mutation, Evidence completion, or any later station.
- Arbitrary caller attributes, baggage, exporter endpoints, credentials, secrets, tokens, queries, exception details, or storage selection.
- Weakening redaction, bypassing tenant/classification isolation, omitting required signals, accepting untrusted collectors, or silently dropping failed signals.
- AI telemetry policy, configuration, routing, registration, or workflow authority.
- Concrete observability vendor, mandatory SaaS/public control plane, new package/project/service/database/queue, conditional technology, or architecture deviation.
- Numerical SLO, sampling rate, retention, cardinality, throughput, or latency before policy and benchmarking.
- Real external activation without configured institutional adapters and authority.

### Affected boundaries

- `Platform.SoftwareFactory/InternalService` binds activation to the Deployment evidence chain.
- `Platform.Observability` remains the OpenTelemetry SDK, strict redaction, collector, signal, and central-pipeline foundation.
- `Platform.SoftwareFactory/Delivery` remains workflow authority and cannot be advanced here.
- Identity/Governance retain identity and OPA authority; Evidence retains cryptographic evidence authority.
- API composes only; Web communicates readiness.

### Deployment prerequisites not supplied

- OpenTelemetry OPA adapter; Deployment receipt/run and signed profile readers.
- Institutional redaction-policy verifier and sovereign-compatible telemetry activation gateway.
- Trusted collector agent/gateway, workload identity, certificates/trust anchors, result authorizer, and evidence recorder.

Until configured, OpenTelemetry returns unavailable or denied without reads, exporter configuration, signal transmission, registration, Enterprise Model mutation, workflow advancement, or external action.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to implementation verification**.

- OpenTelemetry and strict redaction remain the approved telemetry baseline.
- OPA and deterministic workflow remain policy/workflow authorities; AI cannot configure telemetry.
- Exact Deployment/runtime/resource/profile binding and mandatory signals preserve traceability.
- Sovereign and air-gapped operation has no mandatory external dependency.
- Later registration/model/evidence stations remain unavailable.
- No numerical SLO is invented.

## 4. Decision

Approve **Operational Increment 19 — Governed OpenTelemetry Activation** only, including implementation, verification, source control, and synchronization. No real external activation or later station is authorized.

Decision: **Approved by the repository owner for Operational Increment 19**.

## 5. Master Specification Update

Append upon implementation verification:

> Operational Increment 19 is **Governed OpenTelemetry Activation**. It defines protected activation of the exact approved telemetry profile for an accepted governed Deployment and a deterministic delivery run stopped at `Deployment`. Verified OPA authorizes the exact Deployment, runtime, Artifact, effect posture, telemetry profile, resource identity, required signals, collectors, redaction, tenant, purpose, classification, environment, and evidence before reads or configuration. Strict redaction and trusted sovereign collector routing are independently verified before a vendor-neutral gateway may configure traces, metrics, and logs and prove resource binding, baggage clearing, redaction, signal acceptance, and evidence. Result authorization and cryptographic evidence are mandatory. No Automatic Registration, Enterprise Model mutation, or workflow advancement is available.

The constitutional architecture and fixed roadmap remain unchanged.

## 6. Approval

- Product: Create Internal Service Workspace.
- Increment: 19 — Governed OpenTelemetry Activation.
- Authorization: bounded contract implementation, verification, source control, and synchronization only.

Approval state: **Approved**.

Repository-owner decision: **Approved on 2026-09-06**.

## Acceptance criteria

1. Exact identity, permission, Deployment/runtime/Artifact/effect posture, profile, resource, signals, collectors, redaction, purpose, classification, environment, and evidence are required.
2. Signed OPA authorization precedes all prerequisite reads and activation.
3. Deployment receipt is accepted, exact, evidence-bearing, and non-advancing; the run is stopped at `DeliveryStage.Deployment`.
4. Signed telemetry profile exactly binds resource, signals, collectors, redaction, tenant, environment, and deployment.
5. Redaction verification enforces sensitive-field drop, unknown-field redaction, bounded strings, baggage clearing, and controlled low-cardinality attributes.
6. Gateway proves exact runtime/resource/profile/collector binding and unique accepted evidence-bearing traces, metrics, and logs.
7. Result authorization and cryptographic evidence precede disclosure.
8. No Automatic Registration, Enterprise Model mutation, or workflow advancement occurs.
9. Missing dependencies fail closed before reads or external action.
10. No concrete vendor, mandatory external control plane, SLO, conditional technology, or architecture deviation is introduced.
11. OpenAPI and Blazor expose the boundary without claiming configured readiness.
12. All previous gates remain satisfied; all 15 projects build without warnings/errors; runtime remains fail closed.
