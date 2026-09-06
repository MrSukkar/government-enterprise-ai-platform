# CR-001 Amendment 15 — Governed Sovereign Deployment

Status: **Approved for Operational Increment 18**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-14-ARTIFACT.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only `Artifact -> Deployment`.

Increment name: **Operational Increment 18 — Governed Sovereign Deployment**. This amendment does not create Phase 31.

### Product outcome

An authorized operator can deploy the exact immutable, supply-chain-verified Artifact from Increment 17 to one exact policy-approved environment. Verified OPA authorizes the Artifact receipt and digest, immutable registry reference, deployment profile identity/version/digest, topology, target environment, human approval, production intent, tenant, purpose, classification, and evidence scope before Artifact or profile read. A deterministic delivery run must be stopped exactly at `Artifact`. The authoritative deployment artifact and sovereign profile are revalidated, institutional preflight must pass, and a vendor-neutral gateway may perform exactly one idempotent deployment with runtime identity, activation, rollback, and evidence proof. Result authorization and cryptographic evidence precede receipt release. No OpenTelemetry, automatic registration, Enterprise Model mutation, or workflow advancement is available.

## 2. Impact Analysis

### In scope

- Protected REST deployment after an accepted Artifact receipt.
- Governed identity, explicit deploy permission, tenant, purpose, classification, exact target, production intent, human approval, profile, artifact, and evidence.
- Signed OPA authorization before prerequisite or deployment-profile reads and before preflight or runtime invocation.
- Authoritative Artifact receipt, verified deployment-artifact, delivery-run, and sovereign-profile readers.
- Exact Artifact digest, immutable registry, SBOM, build attestation, signature, and supply-chain evidence revalidation.
- Cloud, private-cloud, hybrid, on-premises, and air-gapped profiles with exactly one trusted dependency binding for every required sovereign service.
- Air-gapped default-deny networking, local dependencies, and no external API, AI, SaaS, or control plane.
- Institutional preflight for profile trust, target isolation, secrets references, workload identity, rollback, capacity policy, and Artifact availability.
- Vendor-neutral, idempotent deployment gateway returning exact runtime identity, artifact, profile, target, activation, rollback, external-effect, production-effect, and evidence proof.
- Result authorization and cryptographic evidence.
- Explicit deployment and external-effect recording; production effect must exactly equal the OPA-authorized request.
- No workflow completion or advancement; OpenTelemetry is separately governed.
- OpenAPI 3.1, Blazor, readiness, acceptance, and non-regression verification.
- Missing dependencies fail closed before reads, preflight, secrets access, or runtime invocation.

### Out of scope

- OpenTelemetry, Automatic Registration, Enterprise Model, Evidence completion, or later stations.
- Deployment of any unverified, mutable, substituted, overwritten, unsigned, or non-institutional Artifact.
- Deployment before OPA permit, valid human approval, exact profile, and preflight.
- AI deployment authority, policy authority, target choice, credential access, rollback choice, or workflow authority.
- Caller-supplied commands, credentials, secrets, keys, arbitrary endpoints, manifests, infrastructure definitions, or runtime parameters.
- Profile substitution, target widening, multi-environment deployment, silent retry to another target, rollback suppression, or bypass of sovereign controls.
- A concrete cloud/orchestrator vendor, mandatory external SaaS/control plane, conditional technology approval, new service/project/database/queue/package, or architecture deviation.
- Numerical SLO, capacity, timeout, retry, or rollout percentage before institutional policy and benchmarking.
- Changes to the fixed roadmap or constitutional invariants.

### Affected boundaries

- `Platform.SoftwareFactory/InternalService` binds deployment to the Artifact and delivery evidence chain.
- `Platform.Infrastructure/Sovereignty` remains the foundation for sovereign deployment semantics and vendor-neutral runtime integration.
- `Platform.SoftwareFactory/Delivery` remains the only workflow authority; deployment cannot advance it.
- Identity and Governance retain identity and signed OPA authority; Evidence retains cryptographic evidence authority.
- API composes only; Web communicates readiness without claiming connectivity.

### Deployment prerequisites not supplied

- Deployment OPA adapter; authoritative Artifact receipt/artifact, run, and profile readers.
- Institutional preflight validator and vendor-neutral deployment gateway backed by an approved sovereign runtime.
- Workload identity, secrets/key management, trusted dependency bindings, rollback implementation, result authorizer, and evidence recorder.

Until configured, Deployment returns unavailable or denied without prerequisite disclosure, secrets access, runtime invocation, infrastructure mutation, external effect, production effect, registration, telemetry, or advancement.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to implementation verification**.

- The approved .NET 10 Modular Monolith, Blazor WebAssembly, REST/OpenAPI 3.1, and sovereign topology baseline remains unchanged.
- OPA and deterministic workflow remain the only policy/workflow authorities; AI cannot deploy.
- Deployment consumes only the exact immutable verified Artifact and exact approved sovereign profile with human approval.
- Air-gapped and sovereign operation gains no mandatory external dependency.
- Deployment effects are explicit and bounded; later stations remain unavailable.
- Evidence remains cryptographic, traceable, access-controlled, and tamper-evident.
- No numerical SLO is invented.

## 4. Decision

Approve **Operational Increment 18 — Governed Sovereign Deployment** only. This authorizes implementation, verification, source control, and governed GitHub synchronization, but not later stations or any real public deployment.

Decision: **Approved by the repository owner for Operational Increment 18**.

## 5. Master Specification Update

Append upon implementation verification:

> Operational Increment 18 is **Governed Sovereign Deployment**. It defines protected deployment of the exact immutable, supply-chain-verified Artifact to one exact policy-approved environment from a deterministic delivery run stopped at `Artifact`. Verified OPA authorizes the Artifact, registry reference, sovereign profile, topology, target, human approval, production intent, tenant, purpose, classification, and evidence before reads or invocation. The authoritative Artifact and profile are revalidated, institutional preflight is mandatory, and a vendor-neutral gateway returns idempotent runtime, activation, rollback, effect, and evidence proof. Result authorization and cryptographic evidence are mandatory. AI has no deployment or workflow authority. No OpenTelemetry, registration, Enterprise Model mutation, or workflow advancement is available.

The constitutional architecture and fixed roadmap remain unchanged.

## 6. Approval

- Product: Create Internal Service Workspace.
- Increment: 18 — Governed Sovereign Deployment.
- Authorization: bounded contract implementation, verification, source control, and synchronization only; no real public deployment.

Approval state: **Approved**.

Repository-owner decision: **Approved on 2026-09-06**.

## Acceptance criteria

1. Exact identity, permission, Artifact, profile, target, production intent, human approval, purpose, classification, environment, and evidence are required.
2. Signed OPA authorization precedes all prerequisite reads and runtime actions.
3. Artifact receipt and deployment artifact are accepted, immutable, supply-chain verified, digest-matching, signed, evidence-bearing, and non-advancing.
4. The delivery run is stopped exactly at `DeliveryStage.Artifact`.
5. The exact signed sovereign profile has every trusted dependency binding and enforces air-gap constraints when applicable.
6. Institutional preflight passes exact Artifact, profile, target, workload identity, secrets-reference, rollback, isolation, and capacity-policy controls.
7. The gateway performs one idempotent deployment and returns matching runtime identity, Artifact, profile, target, activation, rollback, and evidence.
8. Production effect exactly matches the OPA-authorized request; AI cannot authorize or invoke deployment.
9. Result authorization and cryptographic evidence precede receipt disclosure.
10. No telemetry, registration, Enterprise Model mutation, or workflow advancement occurs.
11. Missing or mismatched dependencies fail closed before disclosure or external effect.
12. No concrete vendor, mandatory external control plane, conditional technology, or architecture deviation is introduced.
13. OpenAPI and Blazor expose the boundary without claiming configured runtime readiness.
14. All previous gates remain satisfied; all 15 projects build without warnings/errors; runtime remains fail closed.
