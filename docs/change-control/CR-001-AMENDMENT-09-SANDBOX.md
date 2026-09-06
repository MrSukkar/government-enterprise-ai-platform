# CR-001 Amendment 09 — Governed Sandbox Boundary

Status: **Approved for Operational Increment 12**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-08-SECURITY-VALIDATION.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next approved vertical-slice station:

`Security Validation -> Sandbox`

Proposed increment name: **Operational Increment 12 — Governed Security Sandbox Execution**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request isolated execution of an authoritative code candidate only after an accepted, evidence-bearing Security Validation receipt. Verified OPA policy authorizes the exact candidate digest, Security report digest, tenant, purpose, environment, classification, delivery-run identity, institutional sandbox-image coordinate, isolation policy, non-secret environment references, and network destinations before prerequisite read, candidate disclosure, image access, or runtime invocation. A deployment-controlled delivery run must be stopped exactly at `SecurityValidation`. Execution uses only the existing vendor-neutral `GovernedSandboxService` and `ISecuritySandboxRuntime` under a validated Firecracker-class ephemeral microVM policy with no production credentials, no host-filesystem access, network default deny, configured resource/time limits, and an exact approved image. Results are accepted only for zero exit, no timeout, no isolation violation, and non-placeholder evidence. Result authorization and cryptographic evidence precede release. The receipt cannot advance to Tests or any later station.

## 2. Impact Analysis

### In scope

- A protected REST contract for governed Sandbox execution after accepted Security Validation.
- Governed identity, tenant, purpose, classification clearance, explicit sandbox-execution permission, authorization evidence, candidate and Security-report identities/digests/evidence, delivery-run identity, requested sandbox image, isolation policy, environment references, and network scope.
- A signed, verified, environment-aware OPA decision over every exact execution input before prerequisite read, candidate disclosure, image access, or runtime invocation.
- Deployment-controlled readers for the authoritative Security Validation receipt, immutable Code Generation candidate, and deterministic delivery run.
- Structural proof that Security Validation is permit-backed, accepted, `Gate: Security`, non-executable, non-advancing, digest-matching, and evidence-bearing.
- Structural proof that the code candidate is permit-backed, released, unapplied, non-advancing, evidence-bearing, and exactly matches its authoritative artifact.
- A tenant-matching, complete, ordered, evidence-bearing delivery run stopped exactly at `DeliveryStage.SecurityValidation`.
- Exact immutable `PackageCoordinate` selection for a `PackageKind.SandboxImage`, loaded from the institutional registry only after permit.
- Existing institutional package eligibility and supply-chain assurance checks for current approval, expiry, tenant/environment scope, sovereign copy, provenance, SBOM, signature, and immutable digest before runtime use.
- Exact `SandboxIsolationPolicy` validation requiring `Firecracker-class`, ephemeral microVM isolation, production credentials prohibited, host filesystem prohibited, network default deny, policy-authorized destinations only, and configured positive CPU, memory, and time limits.
- Non-secret environment references only; secret-like names, inline secrets, raw credentials, production connection material, and unapproved references are rejected.
- Use only of the existing `GovernedSandboxService` and vendor-neutral `ISecuritySandboxRuntime`; no provider or external control plane is selected.
- Runtime result validation requiring zero exit, no timeout, no isolation violation, unique non-placeholder produced-artifact references, and non-placeholder execution evidence.
- Per-result authorization before disclosure, covering candidate/security digests, image, isolation policy, network/environment scope, exit state, produced references, evidence, tenant, purpose, environment, and classification.
- A deterministic SHA-256 Sandbox-result digest and cryptographic evidence receipt bound to the complete prerequisite, policy, package assurance, isolation, invocation, result, authorization, and time chain.
- Explicit `IsAccepted`, `ProductionEffectOccurred: false`, `CanAdvance: false`, and no `StageCompletion`; Tests remains separately governed.
- OpenAPI 3.1, Blazor boundary communication, runtime readiness, acceptance verification, and non-regression checks.
- Fail-closed behavior when any policy, prerequisite reader, package registry/eligibility/assurance, sandbox runtime, result authorization, or evidence dependency is unavailable.

### Out of scope

- Approval or implementation of Tests, Human Review, Git, CI/CD, Artifact, Deployment, or any later station.
- Any production execution, production credential, production endpoint, production data mutation, production network destination, host-filesystem access, host process, privileged container, or direct AI-to-production path.
- Non-ephemeral execution, shared mutable workspace, persistent sandbox state, unrestricted egress, inbound network exposure, or destination not explicitly permitted by verified policy.
- Caller-selected unverified images, floating tags, mutable coordinates, image download from an unapproved source, package substitution, or registry mutation.
- Secrets, tokens, passwords, private keys, raw credentials, production connection strings, or secret material in the request or environment map.
- Writing candidate output to the source repository, applying patches, creating commits/branches/PRs, invoking CI/CD, publishing artifacts, or deployment.
- Automatic creation, persistence, advancement, completion, retry, resume, or mutation of the Software Delivery Run or durable workflow.
- Allowing AI, the runtime, API, UI, or caller to choose policy, image approval, network scope, credentials, next stage, or workflow authority.
- Treating Sandbox success as test approval, human approval, source approval, artifact approval, deployment approval, or production readiness.
- A concrete Firecracker provider, VM manager, container engine, cloud service, runtime SDK, image registry product, external API, or mandatory non-sovereign control plane.
- Fake runtime execution, synthetic isolation, fabricated package approval, placeholder evidence, or hard-coded successful results.
- A new database, queue, package, .NET project, service boundary, microservice, conditional technology approval, or architectural deviation.
- Universal CPU, memory, duration, network, artifact-count, performance, or SLO values before workload benchmarking and institutional risk configuration.
- Changes to the fixed roadmap, Master Specification, or constitutional invariants before Change Control approval.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: binds Sandbox execution to the complete validated evidence chain.
- `Platform.SoftwareFactory/Sandbox`: remains the sole execution boundary through `GovernedSandboxService`, `SandboxIsolationPolicy`, and `ISecuritySandboxRuntime`.
- `Platform.SoftwareFactory/Delivery`: remains workflow authority; the run must already be stopped at `SecurityValidation`, and Sandbox cannot advance it.
- `Platform.SoftwareFactory/Packages` and `SupplyChain`: retain institutional image approval and assurance authority.
- `Platform.Identity` and `Platform.Governance`: retain governed identity and signed OPA authority.
- `Platform.Api`: composes the protected boundary without acquiring policy, registry, isolation, runtime, result-authorization, workflow, or evidence authority.
- `Platform.Web`: communicates Sandbox readiness and isolation without claiming runtime/image connectivity, Tests approval, or production effects.
- `Platform.Evidence`: remains cryptographic evidence authority.
- OpenAPI and verification scripts prove authorization ordering, isolation invariants, fail-closed dependencies, and non-advancement.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity-provider configuration.
- A sovereign signed-policy verifier and OPA adapter for Sandbox execution.
- Deployment-controlled Security Validation receipt, code-candidate, and delivery-run readers.
- An institutional exact package-registry reader, sandbox-image eligibility evaluator, and supply-chain assurance verifier.
- A sovereign or air-gapped `ISecuritySandboxRuntime` implementing Firecracker-class ephemeral microVM isolation.
- A policy-authorized Sandbox-result authorizer.
- A sovereign cryptographic evidence implementation when authoritative persistence is required.

Until all dependencies are configured, Sandbox must return unavailable or denied without prerequisite/candidate disclosure, image transfer, runtime invocation, code execution, workflow advancement, institutional mutation, or external effect.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to approval and implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- Phase 12 already provides the vendor-neutral runtime, isolation policy, governed service, and exact `SecurityValidation` prerequisite.
- OPA authorizes every execution input before reads, image access, or invocation; the runtime is not policy or workflow authority.
- Firecracker-class ephemeral microVM isolation, no production credentials, no host filesystem, network default deny, and configured resource limits are mandatory.
- The exact sandbox image remains governed by the institutional package and supply-chain boundaries.
- The deterministic delivery engine remains the only workflow authority; Sandbox cannot create a completion or advance to Tests.
- Execution is isolated and produces no production effect; there is no direct `AI -> Production` path.
- Evidence remains classified, tenant-scoped, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable.
- Sovereign and air-gapped operation gains no mandatory external runtime, image registry, API, SaaS, telemetry, licensing, or control plane.
- No numerical SLO or unbenchmarked resource value is invented.

## 4. Decision

Propose one additional CR-001 product increment limited to **Governed Security Sandbox Execution**.

Approval would authorize implementation, verification, source control, and governed GitHub synchronization for this increment only. It would not authorize production credentials/effects, unrestricted networking, host access, Tests, workflow advancement, Git, CI/CD, deployment, institutional mutation, public deployment, or any later station.

Decision: **Approved by the repository owner for Operational Increment 12**.

## 5. Master Specification Update

Upon approval, append the following paragraph to the CR-001 business implementation addendum:

> Operational Increment 12 is **Governed Security Sandbox Execution**. It defines protected isolated execution bound to an accepted Security Validation receipt, an authoritative inert code candidate, and a deterministic delivery run stopped exactly at `SecurityValidation`. Verified OPA policy authorizes the exact candidate/security digests, institutional sandbox-image coordinate, isolation policy, non-secret environment references, network destinations, tenant, purpose, environment, classification, and evidence scope before prerequisite read, image access, or runtime invocation. The existing vendor-neutral `GovernedSandboxService` runs only through a sovereign-compatible `ISecuritySandboxRuntime` under Firecracker-class ephemeral microVM isolation with no production credentials, no host filesystem, network default deny, configured positive resource/time limits, and an exact approved supply-chain-assured image. Acceptance requires zero exit, no timeout, no isolation violation, result authorization, and cryptographic evidence. No production effect or workflow advancement is available. The result cannot advance to Tests or perform production action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Proposed scope:

- Product: Create Internal Service Workspace.
- Increment: 12 — Governed Security Sandbox Execution.
- Authorization requested: implementation, isolated Sandbox runtime invocation subject to all configured controls, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved on 2026-09-06**.

## Acceptance criteria

1. Sandbox requires governed identity, explicit permission, exact candidate/Security identities, digests and evidence, run, image, isolation, environment/network scope, purpose, classification, and authorization evidence.
2. Verified signed OPA policy authorizes every exact input before prerequisite read, candidate disclosure, image access, or runtime invocation.
3. Denial, invalid signature, mismatch, missing scope, or unavailable policy cannot read prerequisites, access an image, or invoke the runtime.
4. Authoritative Security receipt, candidate, and run are loaded only through deployment-controlled readers after permit and structurally revalidated.
5. Security is accepted, `Gate: Security`, digest-matching, evidence-bearing, non-executable, and non-advancing.
6. The code receipt and artifact are exact, permit-backed, released, inert, unapplied, non-advancing, and evidence-bearing.
7. The run is tenant-matching, complete, ordered, evidence-bearing, and stopped exactly at `DeliveryStage.SecurityValidation`.
8. The image is an exact immutable institutional `SandboxImage` coordinate with current approval and complete sovereign supply-chain assurance.
9. Isolation is exactly Firecracker-class, ephemeral microVM, no production credentials, no host filesystem, network default deny, authorized destinations only, and configured positive CPU/memory/time limits.
10. Environment values are non-secret references; secret-like keys, inline secrets, credentials, and unapproved destinations are rejected.
11. Only `GovernedSandboxService` and `ISecuritySandboxRuntime` invoke execution; no competing or external mandatory runtime is introduced.
12. Acceptance requires zero exit, no timeout, no isolation violation, unique artifact references, and non-placeholder evidence.
13. The complete result is re-authorized before disclosure.
14. A deterministic digest and cryptographic evidence bind prerequisites, policy, image assurance, isolation, invocation, result, authorization, and time.
15. The receipt records no production effect and `CanAdvance: false`; no stage completion or workflow mutation is created.
16. Missing dependencies fail closed without disclosure, transfer, execution, mutation, or external effect.
17. No production access/effect, source write, Tests, Git, CI/CD, deployment, new package/project, or deviation is introduced.
18. OpenAPI 3.1 and Blazor expose the boundary without claiming runtime/image readiness, Tests approval, or production effects.
19. All prior phase and Increment 01–11 gates remain satisfied.
20. Project and runtime verification succeed with all 15 projects at zero warnings and zero errors.
