# CR-001 Amendment 07 — Static Validation Boundary

Status: **Approved for Operational Increment 10**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-06-CODE-GENERATION.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next station in the approved vertical slice after Governed Code Generation Candidate:

`Code Generation -> Static Validation`

Approved increment name: **Operational Increment 10 — Governed Static Validation**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request deterministic static validation of a previously released, non-executable and unapplied code-generation candidate. Verified OPA policy must authorize the exact code-candidate digest, tenant, purpose, environment, classification, delivery-run identity, and required static-control identities before the candidate is read or any control runs. A deployment-controlled delivery run must be stopped exactly at `CodeGeneration`. The authoritative candidate is loaded through a controlled reader, revalidated against its cryptographic receipt, and passed only to the existing vendor-neutral `CodeValidationPipeline` with `ValidationGate.Static`. Every policy-required control must be present, complete, evidence-bearing, and free of Error or Critical findings. The resulting report is re-authorized and cryptographically evidenced, remains non-executable and non-advancing, and cannot start Security Validation or any later station.

## 2. Impact Analysis

### In scope

- A protected REST contract for Static Validation of a previously released Governed Code Generation candidate.
- Revalidation of authenticated subject, tenant, purpose, classification clearance, explicit static-validation permission, authorization evidence, generation identity/digest/evidence, and delivery-run identity.
- A signed, verified, environment-aware OPA decision over the exact generation identity, candidate digest, run identity, tenant, subject, purpose, environment, classification, requested static-control identities, and prerequisite evidence before candidate read or control execution.
- A deployment-controlled read boundary returning the authoritative Code Generation receipt and immutable `AiCandidateArtifact`; caller-supplied code content, paths, runtime metadata, evaluation, authorization, or evidence is never authoritative.
- Structural verification that the receipt is permit-backed, released, non-executable, unapplied, non-advancing, evidence-bearing, and matches the exact authoritative artifact content and generated paths.
- A deployment-controlled reader for a tenant-matching, evidence-bearing, deterministic `SoftwareDeliveryRun` stopped exactly at `DeliveryStage.CodeGeneration` with every preceding stage present, passed, ordered, and evidenced.
- Use of the existing `CodeValidationPipeline`, `ICodeValidationControl`, `CodeValidationRequest`, and `ValidationGate.Static` boundaries; no parallel validation framework is introduced.
- Exact matching between OPA-authorized control identities, requested control identities, and registered Static controls. Missing, duplicate, substituted, unexpected, or wrong-gate controls cause a hard failure.
- Execution of all and only the exact required Static controls against the inert authoritative candidate through the existing asynchronous vendor-neutral interface.
- Structural verification that every control report belongs to `ValidationGate.Static`, has the exact control identity, is complete, carries non-placeholder evidence, and contains structurally valid findings.
- Fail-closed rejection when any report is incomplete, evidence is absent, a finding is malformed, or an Error/Critical finding exists. Informational and Warning findings remain recorded and evidenced but grant no workflow authority.
- Per-result authorization after validation and before report release, bound to candidate digest, exact control/report identities, findings, evidence, tenant, purpose, environment, and classification.
- A deterministic SHA-256 validation-report digest and cryptographic evidence receipt bound to request, code-generation evidence, delivery run, OPA decision, candidate digest, controls, reports, findings, result authorization, and time.
- Explicit `Gate: Static`, `IsAccepted`, `IsExecutable: false`, `CanAdvance: false`, and no `StageCompletion`; Security Validation remains a separately governed future station even when Static Validation is accepted.
- OpenAPI 3.1, Blazor boundary communication, runtime-readiness visibility, acceptance verification, and non-regression checks.
- Fail-closed behavior when policy, candidate-read, run-read, required-control, result-authorization, or evidence dependencies are unavailable.

### Out of scope

- Approval or implementation of Security Validation, secret scanning as a Security gate, SAST as a Security gate, Sandbox, Tests, Human Review, Git, CI/CD, Artifact, Deployment, or any later station.
- Source-file writes, patch application, formatting mutations, package restore, dependency download, compilation that creates durable artifacts, command or script execution, test execution, sandbox execution, Git access, network access, credentials, secrets, tools, or external effects.
- Automatic creation, persistence, advancement, completion, retry, resume, or mutation of a Software Delivery Run or durable workflow.
- Allowing the caller, AI, API, UI, validator, or validation control to choose workflow order, approve itself, alter policy scope, waive a required control, suppress a finding, or manufacture evidence.
- Treating Static Validation acceptance as security approval, sandbox approval, human approval, executable authority, merge authority, deployment authority, or production readiness.
- Returning or disclosing code before verified OPA authorization, or reading any candidate other than the exact tenant-scoped authoritative candidate.
- Validation-control discovery from an external marketplace, dynamic package acquisition, plugin installation, package registry mutation, or execution of unapproved binaries.
- A concrete compiler, analyzer, SAST vendor, secret scanner, package, SDK, external service, SaaS dependency, or mandatory external control plane.
- Fake production readers, synthetic permits, fabricated control reports, placeholder evidence, hard-coded passing results, or in-memory success adapters registered as operational dependencies.
- A new database, queue, package, .NET project, service boundary, microservice, conditional technology approval, or architectural deviation.
- Numerical thresholds, warning budgets, file limits, timeouts, retries, performance targets, SLOs, or universal acceptance scores before workload benchmarking and institutional policy configuration.
- Changes to the fixed 30-phase roadmap, Master Specification, or constitutional invariants before this amendment is approved through Change Control.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: owns Create Internal Service composition and binds Static Validation to the governed Code Generation evidence chain.
- `Platform.SoftwareFactory/Validation`: remains the sole validation boundary through `CodeValidationPipeline`; only `ValidationGate.Static` is authorized by this amendment.
- `Platform.SoftwareFactory/Delivery`: remains workflow authority; the run must already be stopped at `CodeGeneration`, and this increment cannot advance it.
- `Platform.SoftwareFactory/AiDevelopment`: supplies the immutable candidate-artifact shape only; the AI runtime is not invoked during validation and has no validation authority.
- `Platform.Identity`: supplies authenticated governed identity and access checks; it does not parse credentials here.
- `Platform.Governance`: remains signed-policy verification and OPA authority. Validators, controls, API, and UI cannot widen policy or grant progression.
- `Platform.Api`: composes the protected REST boundary without acquiring candidate-store, policy, validation-control, result-authorization, workflow, or evidence authority.
- `Platform.Web`: communicates validation availability and fail-closed state without claiming configured controls, source mutation, security approval, or workflow advancement.
- `Platform.Evidence`: remains cryptographic evidence authority; no fabricated evidence implementation is introduced.
- OpenAPI and verification scripts: record and prove authentication, OPA-before-read ordering, exact control scope, fail-closed reports, non-execution, and non-advancement.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity-provider configuration.
- A sovereign signed-policy verifier and OPA evaluation adapter for Static Validation.
- A deployment-controlled authoritative Code Generation candidate reader.
- A deployment-controlled deterministic Software Delivery Run reader.
- Institutionally approved implementations of every policy-required `ICodeValidationControl` for `ValidationGate.Static`.
- A policy-authorized Static Validation result authorizer.
- A sovereign cryptographic evidence implementation when authoritative evidence persistence is required.

Until these dependencies are configured, Static Validation must return unavailable or denied without candidate disclosure, control execution, source mutation, workflow advancement, institutional mutation, or external effect.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to approval and implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- The existing Phase 12 pipeline already separates Static and Security gates and requires `CodeGeneration` as the exact prior stage for Static Validation.
- No competing validation pipeline, policy authority, workflow engine, or execution runtime is introduced.
- OPA authorizes the exact candidate and control set before candidate read or control execution.
- The deterministic delivery engine remains the only workflow authority; validation reports cannot create a `StageCompletion` or advance the run.
- The AI runtime is not invoked and has no authority to validate, suppress findings, select controls, or approve output.
- Missing controls, incomplete reports, missing evidence, and Error/Critical findings fail closed as required by Phase 12.
- Static Validation and Security Validation remain separate stations with separate future authorization.
- No source write, durable build artifact, package acquisition, code execution, sandbox execution, Git action, or external effect is introduced.
- Evidence remains tenant-scoped, classified, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable through existing abstractions.
- Sovereign and air-gapped operation gains no mandatory external analyzer, service, telemetry, SaaS, licensing, or control-plane dependency.
- No numerical SLO, threshold, warning budget, or performance claim is invented.

## 4. Decision

Approve one additional CR-001 product increment limited to **Governed Static Validation**.

Approval would authorize implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only. It would not authorize concrete validation products, package acquisition, candidate mutation, compilation artifacts, code execution, Security Validation, sandboxing, workflow advancement, Git, CI/CD, deployment, institutional mutation, public deployment, or any later station.

Decision: **Approved by the repository owner in the active delivery record**.

## 5. Master Specification Update

Append the following paragraph to the CR-001 business implementation addendum in `docs/PROJECT_MASTER_SPECIFICATION_V2.md`:

> Operational Increment 10 is **Governed Static Validation**. It defines a protected Static Validation request bound to a previously released Code Generation candidate and a deterministic delivery run stopped exactly at `CodeGeneration`. Verified OPA policy authorizes the exact candidate digest, required Static control identities, tenant, purpose, environment, classification, and evidence scope before candidate read or control execution. The authoritative inert candidate is loaded through a deployment-controlled reader and processed only by the existing vendor-neutral `CodeValidationPipeline` with `ValidationGate.Static`. Every exact required control must be present, complete, evidence-bearing, and free of Error or Critical findings; missing, substituted, incomplete, unevidenced, or blocking results fail closed. Result authorization and cryptographic evidence are mandatory before report release. No source mutation, command, durable artifact, package acquisition, code execution, workflow advancement, or external effect is available. The result cannot advance to Security Validation or perform material action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 10 — Governed Static Validation.
- Authorization requested: implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved in the active Codex delivery record on 2026-09-06.**

## Acceptance criteria

1. Static Validation requires authenticated identity, matching tenant, sufficient classification clearance, explicit permission, purpose, authorization evidence, exact generation identity/digest/evidence, and matching delivery-run identity.
2. A verified signed-policy reference and evidence-bearing OPA decision authorize the exact request, candidate digest, control identities, tenant, subject, purpose, environment, classification, and evidence scope before candidate read or control execution.
3. OPA denial, invalid signature, stale or mismatched decision, missing control scope, or unavailable policy dependency cannot read or disclose the candidate and cannot invoke a control.
4. The authoritative Code Generation receipt and immutable artifact are loaded only through a deployment-controlled reader after policy permit; caller-supplied code, paths, runtime metadata, evaluation, or evidence is not authoritative.
5. The receipt is permit-backed, released, non-executable, unapplied, non-advancing, digest-matching, evidence-bearing, and structurally identical to the authoritative artifact content and paths.
6. The delivery run is loaded through a deployment-controlled reader and must be tenant-matching, evidence-bearing, deterministically ordered, and stopped exactly at `DeliveryStage.CodeGeneration`.
7. Static Validation uses only the existing `CodeValidationPipeline` with `ValidationGate.Static`; no alternate pipeline or Security control is invoked.
8. Requested, policy-authorized, and registered Static control identity sets match exactly; missing, duplicate, substituted, unexpected, or wrong-gate controls fail before validation.
9. Every exact required control returns a matching Static report that is complete and carries non-placeholder evidence; every finding has a rule, valid severity, message, location, and evidence reference.
10. Missing controls, incomplete reports, absent evidence, malformed findings, or any Error/Critical finding cause a fail-closed non-accepted result.
11. Informational and Warning findings are retained with evidence but do not create execution, waiver, approval, or workflow authority.
12. The complete validation report is re-authorized before release against candidate digest, controls, findings, evidence, tenant, purpose, environment, and classification.
13. The receipt carries a deterministic SHA-256 report digest and non-placeholder cryptographic evidence bound to the request, candidate, run, policy, controls, reports, findings, authorization, and time.
14. The receipt records `Gate: Static`, `IsExecutable: false`, and `CanAdvance: false`; no `StageCompletion` or delivery-run mutation is created.
15. Missing policy, candidate reader, run reader, exact Static controls, result authorizer, or evidence recorder remains visible and fails closed without candidate disclosure, control execution, mutation, or external effect.
16. No source write, patch, formatter mutation, package restore/download, command, durable build artifact, code execution, Security Validation, sandbox, test, Git, CI/CD, deployment, workflow advancement, institutional mutation, external effect, new package, new project, or architectural deviation is introduced.
17. OpenAPI 3.1 and Blazor expose the fail-closed validation boundary without claiming configured controls, Security approval, source mutation, or operational readiness.
18. All prior phase and Increment 01–09 acceptance gates remain satisfied.
19. `scripts/verify-project.ps1` succeeds with all 15 projects at zero warnings and zero errors, and runtime verification proves protected and fail-closed behavior.
