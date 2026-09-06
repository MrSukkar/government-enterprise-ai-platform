# CR-001 Amendment 08 — Security Validation Boundary

Status: **Approved for Operational Increment 11**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-07-STATIC-VALIDATION.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next approved vertical-slice station:

`Static Validation -> Security Validation`

Approved increment name: **Operational Increment 11 — Governed Security Validation**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request deterministic Security Validation of a previously released Code Generation candidate only after an authoritative, accepted Static Validation report. Verified OPA policy authorizes the exact code-candidate digest, Static report digest, tenant, purpose, environment, classification, delivery-run identity, and required Security-control identities before any prerequisite or candidate read and before any control runs. A deployment-controlled delivery run must be stopped exactly at `StaticValidation`. The existing vendor-neutral `CodeValidationPipeline` is invoked only with `ValidationGate.Security`. Every exact required Security control must be present, complete, evidence-bearing, and free of Error or Critical findings. The result is re-authorized and cryptographically evidenced, remains non-executable and non-advancing, and cannot start Sandbox or any later station.

## 2. Impact Analysis

### In scope

- A protected REST contract for Security Validation of a previously released inert code candidate with a previously accepted Static Validation receipt.
- Governed identity, tenant, purpose, classification clearance, explicit security-validation permission, authorization evidence, exact generation candidate digest/evidence, Static report identity/digest/evidence, and delivery-run identity.
- A signed, verified, environment-aware OPA decision over the exact candidate, accepted Static report, run, tenant, subject, purpose, environment, classification, required Security controls, and evidence scope before prerequisite read or control execution.
- Deployment-controlled readers for the authoritative Static Validation receipt, Code Generation candidate, and deterministic delivery run; caller-supplied code, reports, findings, controls, evidence, or run history is never authoritative.
- Structural proof that the Static receipt is permit-backed, accepted, `Gate: Static`, non-executable, non-advancing, digest-matching, and evidence-bearing.
- Structural proof that the Code Generation receipt is permit-backed, released, non-executable, unapplied, non-advancing, evidence-bearing, and exactly matches the immutable candidate artifact.
- A tenant-matching delivery run stopped exactly at `DeliveryStage.StaticValidation`, with every preceding stage present, passed, ordered, and evidenced.
- Use only of the existing `CodeValidationPipeline`, `ICodeValidationControl`, `CodeValidationRequest`, and `ValidationGate.Security` boundaries.
- Exact equality between requested, OPA-authorized, and registered Security-control identities; missing, duplicate, substituted, unexpected, or wrong-gate controls fail closed before validation.
- Institutionally approved Security controls such as dependency validation, secret scanning, SAST, license checks, and other deployment-selected controls, without selecting a vendor or dynamically acquiring tools.
- Structural verification of every Security control report, finding, severity, location, and evidence reference.
- Fail-closed rejection of missing controls, incomplete or unevidenced reports, malformed findings, or Error/Critical findings; warnings remain visible and evidenced without waiver authority.
- Per-result authorization of the complete Security report before release.
- A deterministic SHA-256 report digest and cryptographic evidence receipt bound to the complete prerequisite, policy, control, finding, authorization, and time chain.
- Explicit `Gate: Security`, `IsAccepted`, `IsExecutable: false`, `CanAdvance: false`, and no `StageCompletion`; Sandbox remains separately governed.
- OpenAPI 3.1, Blazor boundary communication, readiness visibility, acceptance verification, and runtime non-regression checks.
- Fail-closed behavior when any policy, Static-receipt reader, code-candidate reader, run reader, required Security control, result-authorizer, or evidence dependency is unavailable.

### Out of scope

- Approval or implementation of Sandbox, Tests, Human Review, Git, CI/CD, Artifact, Deployment, or any later station.
- Executing candidate code, scripts, commands, installers, analyzers obtained dynamically, package managers, tests, containers, virtual machines, or sandbox workloads.
- Source mutation, patch application, formatting mutation, package download/restore, durable compilation artifact, repository access, branch/commit creation, pipeline execution, credential or secret retrieval, network access, or external effect.
- Automatic creation, persistence, advancement, completion, retry, resume, or mutation of a Software Delivery Run or durable workflow.
- Allowing callers, AI, API, UI, or controls to waive findings, alter required controls, choose workflow transitions, self-approve, suppress evidence, or grant execution authority.
- Treating Security Validation acceptance as Sandbox approval, human approval, Git authority, deployment authority, or production readiness.
- Returning candidate content or validation results before verified authorization.
- A concrete SAST product, secret scanner, dependency scanner, compiler, vendor, package, SDK, SaaS service, external API, or mandatory external control plane.
- Fake readers, synthetic permits, fabricated reports, placeholder evidence, or hard-coded passing controls registered as operational dependencies.
- A new database, queue, package, .NET project, service boundary, microservice, conditional technology approval, or architectural deviation.
- Numerical risk scores, thresholds, severity budgets, timeouts, retries, performance targets, SLOs, or universal acceptance scores before institutional configuration and workload benchmarking.
- Changes to the fixed roadmap, Master Specification, or constitutional invariants before approval through Change Control.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: binds Security Validation to the accepted Static and Code Generation evidence chain.
- `Platform.SoftwareFactory/Validation`: remains the sole validation boundary; only `ValidationGate.Security` is authorized.
- `Platform.SoftwareFactory/Delivery`: remains workflow authority; the run must already be stopped at `StaticValidation`, and this increment cannot advance it.
- `Platform.SoftwareFactory/Sandbox`: remains unchanged and unreachable; a future separately approved increment is required.
- `Platform.Identity` and `Platform.Governance`: retain governed identity and signed OPA authority.
- `Platform.Api`: composes the protected boundary without acquiring candidate, validation, workflow, or evidence authority.
- `Platform.Web`: communicates fail-closed Security Validation without claiming configured scanners, Sandbox approval, or readiness.
- `Platform.Evidence`: remains cryptographic evidence authority.
- OpenAPI and verification scripts prove ordering, exact control scope, non-execution, and fail-closed behavior.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity-provider configuration.
- A sovereign signed-policy verifier and OPA adapter for Security Validation.
- Deployment-controlled authoritative Static Validation receipt, Code Generation candidate, and delivery-run readers.
- Institutionally approved implementations of every policy-required `ICodeValidationControl` for `ValidationGate.Security`.
- A policy-authorized Security Validation result authorizer.
- A sovereign cryptographic evidence implementation when authoritative persistence is required.

Until these dependencies are configured, Security Validation must return unavailable or denied without prerequisite or candidate disclosure, control execution, source mutation, workflow advancement, institutional mutation, or external effect.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to approval and implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- Phase 12 already separates Static and Security gates and requires `StaticValidation` immediately before Security Validation.
- The existing validation pipeline and control abstraction remain authoritative; no competing framework is added.
- OPA authorizes exact prerequisites and controls before reads or execution.
- The deterministic delivery engine alone owns workflow progression; reports cannot create stage completion.
- The AI runtime is not invoked and has no validation, control-selection, or waiver authority.
- Missing, incomplete, unevidenced, Error, or Critical control results fail closed.
- Security Validation and Sandbox remain separate gates.
- No candidate execution, sandbox execution, source mutation, Git action, deployment, or external effect is introduced.
- Evidence and sovereign/air-gapped invariants remain unchanged, with no mandatory external scanner or control plane.
- No numerical SLO or unbenchmarked threshold is invented.

## 4. Decision

Approve one additional CR-001 product increment limited to **Governed Security Validation**.

Approval would authorize implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only. It would not authorize concrete scanner products, dynamic tool acquisition, code execution, Sandbox, workflow advancement, Git, CI/CD, deployment, institutional mutation, public deployment, or any later station.

Decision: **Approved by the repository owner in the active delivery record**.

## 5. Master Specification Update

Append the following paragraph to the CR-001 business implementation addendum:

> Operational Increment 11 is **Governed Security Validation**. It defines a protected Security Validation request bound to an accepted Static Validation receipt, the authoritative inert Code Generation candidate, and a deterministic delivery run stopped exactly at `StaticValidation`. Verified OPA policy authorizes the exact candidate and Static-report digests, required Security-control identities, tenant, purpose, environment, classification, and evidence scope before prerequisite read or control execution. The existing vendor-neutral `CodeValidationPipeline` runs only with `ValidationGate.Security`; every exact required control must be present, complete, evidence-bearing, and free of Error or Critical findings. Result authorization and cryptographic evidence are mandatory before report release. No source mutation, dynamic tool acquisition, command, durable artifact, candidate execution, sandbox execution, workflow advancement, or external effect is available. The result cannot advance to Sandbox or perform material action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 11 — Governed Security Validation.
- Authorization requested: implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved in the active Codex delivery record on 2026-09-06.**

## Acceptance criteria

1. Security Validation requires governed identity, exact generation and Static Validation identities/digests/evidence, exact run identity, required controls, purpose, environment, classification, and authorization evidence.
2. Verified signed OPA policy authorizes the exact request and Security-control set before any prerequisite/candidate read or control execution.
3. Denial, invalid signature, mismatch, missing scope, or unavailable policy cannot disclose prerequisites or run controls.
4. Authoritative Static receipt, code candidate, and run are loaded only through deployment-controlled readers after permit.
5. The Static receipt is accepted, `Gate: Static`, digest-matching, non-executable, non-advancing, and evidence-bearing.
6. The Code Generation receipt and artifact are exact, permit-backed, released, inert, unapplied, non-advancing, and evidence-bearing.
7. The run is tenant-matching, complete, ordered, evidence-bearing, and stopped exactly at `DeliveryStage.StaticValidation`.
8. Only the existing pipeline with `ValidationGate.Security` runs; no Static control or alternate pipeline executes.
9. Requested, authorized, and registered Security-control sets match exactly.
10. Every control report is exact, complete, structurally valid, evidence-bearing, and free of Error/Critical findings.
11. Missing, incomplete, unevidenced, malformed, or blocking results fail closed; warnings carry no waiver or workflow authority.
12. The full report is re-authorized before release.
13. A deterministic report digest and cryptographic evidence bind every prerequisite, policy, control, finding, authorization, and time reference.
14. The receipt records `Gate: Security`, `IsExecutable: false`, and `CanAdvance: false`, with no stage completion.
15. Missing required dependencies remain visible and fail closed without disclosure, control execution, mutation, or external effect.
16. No source write, dynamic acquisition, command, code or sandbox execution, test, Git, CI/CD, deployment, workflow advancement, institutional mutation, new package/project, or deviation is introduced.
17. OpenAPI 3.1 and Blazor expose the boundary without claiming configured scanners, Sandbox approval, or readiness.
18. All prior phase and Increment 01–10 gates remain satisfied.
19. Project and runtime verification succeed with all 15 projects at zero warnings and zero errors.
