# CR-001 Amendment 06 — Code Generation Boundary

Status: **Approved for Operational Increment 09**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-05-AI-PLANNING.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next station in the approved vertical slice after Governed AI Planning Candidate:

`AI Planning -> Code Generation`

Approved increment name: **Operational Increment 09 — Governed Code Generation Candidate**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request an independently evaluated, evidence-bearing code-generation candidate for a previously released Governed AI Planning candidate. A deployment-controlled deterministic delivery run must already be stopped exactly at `AiPlanning`. Verified OPA policy authorizes the exact planning-candidate digest, code-generation prompt template, runtime profile, context references, approved package coordinates, constraints, tenant, purpose, environment, classification, and output-file scope before any AI invocation. Authorized context is re-checked immediately before release to the runtime. Returned file paths and candidate content are data only: they cannot be written, executed, compiled, restored, validated, committed, or otherwise applied by this increment. Independent evaluation and result re-authorization are mandatory before deterministic evidence-bearing release. The result cannot advance to Static Validation or any later station.

## 2. Impact Analysis

### In scope

- A protected REST contract for Code Generation from a previously released, evidence-bearing AI Planning candidate.
- Revalidation of authenticated subject, tenant, purpose, classification clearance, explicit code-generation permission, authorization evidence, governed-intent identity/version, all prerequisite snapshot identities/digests/evidence, AI Planning identity/digest/evidence, and delivery-run identity.
- Deployment-controlled readers for the authoritative AI Planning receipt and deterministic `SoftwareDeliveryRun`; caller-supplied plan content, evaluation, evidence, or run history is never authoritative.
- Structural proof that the planning receipt was released under a permit, is non-executable and non-advancing, contains a non-empty candidate and digest, and is bound to the same Approved Packages snapshot and delivery run.
- Structural proof that the delivery run is tenant-matching, evidence-bearing, ordered without gaps, and stopped exactly at `DeliveryStage.AiPlanning` before prompt/context release or AI invocation.
- An exact governed code-generation prompt-template identity and version loaded from a deployment-controlled registry and verified for digest, signature, integrity, lifecycle, tenant, purpose, environment, classification, and code-generation eligibility.
- A signed, environment-aware OPA decision defining the exact planning digest, prompt digest, runtime profile, authorized context, approved packages, constraints, permitted relative output paths, file-count-independent output scope, tenant, purpose, environment, and classification before AI invocation.
- Per-context-item authorization re-check immediately before AI context release; inaccessible, stale, substituted, mismatched, or over-classified context causes a hard failure.
- Use of the existing vendor-neutral `IAiDevelopmentRuntime` through `GovernedAiDevelopmentService` with `AiDevelopmentTaskKind.CodeGeneration`; no provider, SDK, model, endpoint, or external control plane is selected.
- Structural validation that generated paths are unique, normalized, repository-relative, inside the policy-authorized scope, and contain no absolute path, drive, URI, traversal, empty segment, control character, or prohibited secret/binary location.
- Candidate content and generated paths remain an inert proposal held behind existing abstractions; this increment exposes no filesystem writer, patch applier, command runner, compiler, package restore, Git client, CI/CD client, sandbox runtime, credential, or tool gateway.
- Independent evaluation for grounding, correctness, security, policy compliance, package compliance, and traceability using the existing `IAiOutputEvaluator` boundary.
- Acceptance only when generation and evaluation are independent, every required criterion exists and passes, every finding has non-placeholder evidence, context references are exact, runtime profile is authorized, and generated paths are policy-conformant.
- Per-result authorization after evaluation and before release, including candidate digest, planning digest, tenant, purpose, classification, policy, prompt, context, package, runtime-profile, and output-path scope.
- A SHA-256 code-candidate digest and cryptographic evidence receipt bound to the complete prerequisite chain, delivery run, OPA decision, prompt, invocation, candidate content, generated paths, evaluation, and result authorization.
- Explicit `IsExecutable: false`, `IsApplied: false`, `CanAdvance: false`, and no stage-completion receipt; Static Validation remains a separately governed future station.
- OpenAPI 3.1, Blazor boundary communication, runtime-readiness visibility, acceptance verification, and non-regression checks.
- Fail-closed behavior when any planning read, delivery-run read, package/context read, signed-policy, prompt, context-authorization, runtime, evaluator, result-authorization, or evidence dependency is unavailable.

### Out of scope

- Approval or implementation of Static Validation, Security Validation, Sandbox, Tests, Human Review, Git, CI/CD, Artifact, Deployment, or any later station.
- Writing, creating, replacing, deleting, renaming, moving, patching, formatting, compiling, restoring, resolving, validating, scanning, testing, executing, or publishing any file or package.
- Repository, branch, commit, pull-request, pipeline, artifact, registry, deployment, infrastructure, environment, Enterprise Model, or institutional-state mutation.
- Automatic creation, persistence, advancement, completion, retry, resume, or mutation of a Software Delivery Run or durable workflow.
- AI selection of its own purpose, prompt, model, runtime profile, context, packages, constraints, output scope, tools, next step, approval, or workflow transition.
- Tool use, MCP invocation, shell access, filesystem access, network access, source-control access, CI/CD access, sandbox execution, secret retrieval, credentials, live sessions, or external effects.
- Treating AI output as architecture approval, package approval, policy decision, human approval, executable code, validation result, workflow receipt, merge instruction, or production instruction.
- New package selection, package registry access, download, restore, installation, execution, substitution, or mutation.
- Unrestricted retrieval, new GraphRAG retrieval, caller-supplied authoritative context, or release of context not re-authorized for this invocation.
- A concrete AI provider, model, SDK, API, endpoint, credential, agent-framework binding, external evaluator, external prompt service, or mandatory non-sovereign dependency.
- Fake production adapters, synthetic permits, fabricated evaluations, placeholder evidence, sample institutional data, or hard-coded successful AI output accepted as authoritative.
- A new database, queue, package, .NET project, service boundary, microservice, conditional technology approval, or architectural deviation.
- Numerical quality scores, token limits, temperatures, timeouts, retry counts, file-count limits, size limits, acceptance thresholds, SLOs, or performance targets before benchmarking and institutional policy configuration.
- Changes to the fixed 30-phase roadmap, Master Specification, or constitutional invariants before this amendment is approved through Change Control.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: owns the Create Internal Service composition and binds generation to the complete prerequisite evidence chain.
- `Platform.SoftwareFactory/Delivery`: remains workflow authority; generation requires a deployment-controlled run stopped at `AiPlanning`, while this increment cannot advance it.
- `Platform.SoftwareFactory/AiDevelopment`: remains the sole vendor-neutral generation and independent-evaluation boundary through `GovernedAiDevelopmentService`, `IAiDevelopmentRuntime`, and `IAiOutputEvaluator`.
- `Platform.SoftwareFactory/Packages`: supplies exact previously approved immutable package coordinates without package transfer or mutation.
- `Platform.SoftwareFactory/Validation`: remains unchanged and unavailable to this increment; it becomes relevant only under separately approved Static Validation change control.
- `Platform.Identity`: supplies authenticated governed identity and access context; it does not parse credentials here.
- `Platform.Governance`: remains signed-policy verification and OPA authority. AI, API, UI, prompt, evaluator, and runtime cannot grant generation or workflow authority.
- `Platform.Knowledge` and `Platform.EnterpriseModel`: retain contextual source-of-truth responsibilities; context is re-authorized before AI release and never mutated here.
- `Platform.Api`: composes the protected REST boundary without acquiring policy, workflow, prompt, runtime, evaluation, path-authorization, or evidence authority.
- `Platform.Web`: communicates candidate-only and fail-closed state without claiming source writes, model connectivity, validation, or readiness.
- `Platform.Evidence`: remains cryptographic evidence authority; no fabricated evidence implementation is introduced.
- OpenAPI and verification scripts: record and prove authentication, ordering, path safety, candidate-only output, independent evaluation, non-execution, and fail-closed behavior.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity-provider configuration.
- A sovereign signed-policy verifier and OPA evaluation adapter.
- Deployment-controlled authoritative readers for the AI Planning receipt, prerequisite snapshots, and deterministic Software Delivery Run.
- A governed code-generation prompt registry with signature and integrity verification.
- A policy-authorized context assembler and per-item context authorizer.
- A sovereign or air-gapped-compatible `IAiDevelopmentRuntime` implementation for code generation.
- An independent `IAiOutputEvaluator` implementation.
- A policy-authorized generated-path and code-result authorizer.
- A sovereign cryptographic evidence implementation when authoritative evidence persistence is required.

Until these dependencies are configured, Code Generation must return unavailable or denied without AI invocation, context disclosure, filesystem access, generated-file application, workflow advancement, institutional mutation, or external effect.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to approval and implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- The Enterprise Model remains contextual source of truth; AI receives only policy-authorized, re-authorized evidence-chain context and cannot mutate it.
- The existing AI development abstractions already distinguish `Planning` from `CodeGeneration` and enforce `DeliveryStage.AiPlanning` as the generation prerequisite.
- The deterministic delivery engine remains workflow authority. No LLM, runtime, evaluator, API, or UI can advance the run.
- OPA remains Policy Authority and is evaluated before prompt/context release or AI invocation.
- Generated code remains a non-executable candidate. It is not written, applied, compiled, restored, scanned, tested, committed, or deployed.
- Static Validation remains a distinct later gate; generation cannot self-validate or create its completion receipt.
- Independent evaluation covers every Phase 11 criterion and cannot be self-attested by the generation runtime.
- PostgreSQL, Neo4j, conditional vector stores, Agent Framework candidacy, and all other technology decisions remain unchanged.
- Evidence remains tenant-scoped, classified, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable through existing abstractions.
- Sovereign and air-gapped operation gains no mandatory external model, API, SaaS, telemetry, licensing, or control-plane dependency.
- No direct `AI -> Production` path, material action, workflow advancement, or external effect is introduced.

## 4. Decision

Approve one additional CR-001 product increment limited to **Governed Code Generation Candidate**.

Approval would authorize implementation, verification, source control, and governed GitHub synchronization for this increment only. It would not authorize a concrete model/provider, credentials, source-file writes, code execution, package operations, validation, sandboxing, workflow advancement, Git operations performed by generated output, CI/CD, deployment, institutional mutation, public deployment, or any later station.

Decision: **Approved by the repository owner in the active delivery record**.

## 5. Master Specification Update

Append the following paragraph to the CR-001 business implementation addendum in `docs/PROJECT_MASTER_SPECIFICATION_V2.md`:

> Operational Increment 09 is **Governed Code Generation Candidate**. It defines a protected code-generation request bound to a previously released AI Planning candidate and a deterministic delivery run stopped exactly at `AiPlanning`. Verified OPA policy authorizes the exact planning digest, code-generation prompt, runtime profile, context references, approved packages, constraints, permitted relative output paths, tenant, purpose, environment, and classification before context release or AI invocation. The vendor-neutral runtime may return only a non-executable, unapplied code candidate; generated paths are normalized, repository-relative, policy-scoped data and no filesystem, tool, command, package, Git, CI/CD, sandbox, or deployment access is exposed. Independent evaluation and per-result authorization are mandatory before deterministic evidence-bearing release. Missing prerequisite-read, policy, prompt, context-authorization, runtime, evaluator, result-authorization, or evidence adapters fail closed without AI invocation, context disclosure, source mutation, workflow advancement, or external effect. The result cannot advance to Static Validation or perform material action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 09 — Governed Code Generation Candidate.
- Authorization requested: implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved in the active Codex delivery record on 2026-09-06.**

## Acceptance criteria

1. Generation requires an authenticated subject, matching tenant, sufficient classification clearance, explicit code-generation permission, purpose, authorization evidence, and matching identities, versions, digests, and evidence across every prerequisite.
2. The authoritative AI Planning receipt and deterministic delivery run are loaded through deployment-controlled readers and structurally revalidated; caller-supplied plan, candidate, evaluation, evidence, or run history cannot become authoritative.
3. The planning receipt is permit-backed, released, non-executable, non-advancing, evidence-bearing, and bound to the exact Approved Packages snapshot and delivery run.
4. The delivery run is valid, tenant-matching, evidence-bearing, deterministically ordered without missing stages, and stopped exactly at `DeliveryStage.AiPlanning`; any other state fails before prompt/context release or AI invocation.
5. An exact governed prompt template passes digest, signature, integrity, tenant, purpose, environment, classification, lifecycle, and code-generation eligibility checks.
6. A verified OPA permit defines the exact planning digest, prompt digest, runtime profile, context, packages, constraints, permitted relative paths, tenant, purpose, environment, classification, and output scope before AI invocation.
7. OPA denial, signature failure, mismatch, invalid prerequisite, unavailable dependency, stale prompt, or missing scope cannot invoke AI.
8. Every AI-context item is derived from the authorized prerequisite chain and re-authorized immediately before runtime release; one denied or mismatched item causes a hard failure.
9. The existing AI development boundary is invoked only with `AiDevelopmentTaskKind.CodeGeneration`, the verified prompt, exact planning-derived context, exact approved packages, policy-authorized constraints, and a run stopped at `AiPlanning`.
10. The runtime candidate has the authorized runtime profile, non-empty invocation identity/content, exact context references, and one or more unique normalized repository-relative generated paths wholly inside policy scope.
11. Absolute paths, drive-qualified paths, URIs, traversal, empty segments, control characters, duplicate paths, and prohibited secret, binary, generated-output, or repository-control locations are rejected.
12. Independent evaluation is required for grounding, correctness, security, policy compliance, package compliance, and traceability; all criteria pass with non-placeholder evidence and an evaluator independent from generation.
13. Any incomplete, failing, self-evaluated, mismatched, over-classified, package-noncompliant, unsafe-path, ungrounded, untraceable, or unauthorized candidate fails closed; every accepted candidate is re-authorized before release.
14. The receipt binds every prerequisite, run, policy, prompt, invocation, candidate content/path, evaluation, and authorization; carries a SHA-256 digest and non-placeholder cryptographic evidence; and records `IsExecutable: false`, `IsApplied: false`, and `CanAdvance: false`.
15. Missing planning reader, run reader, policy, prompt, context authorizer, runtime, evaluator, path/result authorizer, or evidence recorder remains visible and fails closed without AI invocation, context disclosure, filesystem access, workflow advancement, or mutation.
16. No source write, patch application, command, restore, compilation, validation, security scan, sandbox, test, workflow completion/advancement, Git, CI/CD, artifact, deployment, institutional mutation, external effect, new package, new project, or architectural deviation is introduced.
17. OpenAPI 3.1 and Blazor expose the candidate-only boundary without claiming model, source-write, validation, or operational readiness.
18. All prior phase and Increment 01–08 acceptance gates remain satisfied.
19. `scripts/verify-project.ps1` succeeds with all 15 projects at zero warnings and zero errors, and runtime verification proves the protected endpoint and missing dependencies remain fail closed.
