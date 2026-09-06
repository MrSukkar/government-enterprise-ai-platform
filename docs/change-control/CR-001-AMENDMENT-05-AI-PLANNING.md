# CR-001 Amendment 05 — AI Planning Boundary

Status: **Approved for Operational Increment 08**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-04-APPROVED-PACKAGES.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next station in the approved vertical slice after Governed Approved Packages Selection:

`Approved Packages -> AI Planning`

Approved increment name: **Operational Increment 08 — Governed AI Planning Candidate**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request a non-executable, independently evaluated, evidence-bearing AI planning candidate for a previously released Approved Packages snapshot. A deployment-controlled deterministic delivery run must already be stopped at `ApprovedPackages`. Verified OPA policy authorizes the exact planning purpose, prompt template, runtime profile, context references, approved package coordinates, constraints, tenant, environment, and classification before any AI runtime invocation. Authorized context is re-checked before release to the runtime; the candidate is structurally constrained to planning content with no generated files, tool call, command, workflow authority, or external effect. Independent evaluation and result re-authorization are required before release. The result cannot advance to Code Generation, execute code, mutate institutional state, or reach production.

## 2. Impact Analysis

### In scope

- A protected REST contract for AI Planning from a previously released, evidence-bearing Approved Packages selection snapshot.
- Revalidation of authenticated subject, tenant, purpose, classification clearance, explicit planning permission, authorization evidence, governed-intent registration identity and version, Enterprise Context, Existing Systems, Existing Architecture, Approved Packages identities and digests, and their evidence references.
- A deployment-controlled read boundary for the authoritative Approved Packages snapshot; caller-supplied package records, approval state, assurance, or evidence are not authoritative.
- A deployment-controlled read boundary for the deterministic `SoftwareDeliveryRun`; it must be tenant-matching, evidence-bearing, structurally valid, and stopped exactly at `DeliveryStage.ApprovedPackages` before planning can be considered.
- A caller-selected approved prompt-template identity and version, treated only as a request until loaded from a deployment-controlled template registry and verified for signature, integrity, tenant, purpose, environment, classification, lifecycle, and planning-task eligibility.
- A signed, environment-aware OPA policy reference and evidence-bearing decision defining explicit planning purpose, prompt-template identity and digest, allowed AI runtime profile, authorized context references, exact approved package coordinates, constraints, tenant, environment, classification, and output kind before any AI runtime invocation.
- Assembly of AI planning context only from the previously authorized Enterprise Context, Existing Systems, Existing Architecture, and Approved Packages evidence chain. Caller-supplied context content cannot become authoritative.
- Per-context-item authorization re-check immediately before release into the AI runtime request. Any inaccessible, stale, substituted, over-classified, or mismatched context item causes a hard failure.
- Use of the existing vendor-neutral `IAiDevelopmentRuntime` through `GovernedAiDevelopmentService` with `AiDevelopmentTaskKind.Planning`; no provider, SDK, model, endpoint, or external control plane is selected.
- Structural verification that the runtime profile matches policy; invocation identity and time are valid; returned context references are exactly authorized; planning content is non-empty; and `GeneratedFilePaths` is empty.
- Independent evaluation through the existing `IAiOutputEvaluator` for grounding, correctness, security, policy compliance, package compliance, and traceability.
- Acceptance only when the evaluator is independent from the generation runtime, every required criterion is present and passes, and every finding carries non-placeholder evidence.
- Per-result authorization after evaluation and before release, including tenant, purpose, classification, policy, prompt, context, package, runtime-profile, and disclosure scope.
- A SHA-256 planning-candidate digest and cryptographic evidence receipt bound to every prerequisite snapshot, delivery run, OPA decision, prompt template, runtime invocation, candidate content, evaluation findings, and result authorization.
- Explicit `IsExecutable: false`, `CanAdvance: false`, and no stage-completion receipt; Code Generation remains a separately governed future station.
- OpenAPI 3.1, Blazor boundary communication, readiness visibility, acceptance verification, and runtime non-regression checks.
- Fail-closed behavior when Approved Packages snapshot read, delivery-run read, signed OPA policy, prompt-template registry or verifier, context re-authorization, AI runtime, independent evaluator, result authorization, or evidence dependencies are unavailable.

### Out of scope

- Approval or implementation of Code Generation, Static Validation, Security Validation, Sandbox, or any later vertical-slice station.
- Automatic creation, advancement, completion, persistence, retry, resume, or mutation of a Software Delivery Run or durable workflow.
- AI selection of its own purpose, prompt, model, runtime profile, context, packages, constraints, tools, next step, approval, or workflow transition.
- Code generation, generated file paths, patches, repository edits, executable commands, scripts, binaries, package manifests, lock files, infrastructure manifests, deployment plans with execution authority, or machine-actionable tool requests.
- Tool use, MCP invocation, agent runtime execution, shell access, filesystem writes, network access, source-control access, CI/CD access, sandbox execution, credential use, secret retrieval, live sessions, or external effects.
- Treating AI output as architecture approval, package approval, policy decision, human approval, executable command, code, workflow receipt, or production instruction.
- Prompt-template creation, editing, approval, signing, publication, revocation, or deletion.
- Context retrieval outside the previously authorized snapshot chain, unrestricted search, new GraphRAG retrieval, or release of data not re-authorized for AI context.
- Package registry access, package download, restore, installation, resolution, execution, substitution, or mutation.
- A concrete AI provider, model, SDK, API, endpoint, credential, Agent Framework binding, external evaluator, external prompt service, or non-sovereign control-plane dependency.
- Fake or in-memory production adapters, synthetic permits, fabricated evaluations, placeholder evidence, sample institutional context, or hard-coded successful AI output accepted as authoritative.
- A new database, queue, package, .NET project, service boundary, microservice, model decision, agent framework decision, vector-store decision, or conditional technology approval.
- A numerical quality score, token limit, temperature, timeout, retry count, context-size limit, acceptance threshold, SLO, or performance target before workload benchmarking, runtime validation, and institutional policy configuration.
- Enterprise Model, package registry, architecture, policy, evidence, Git, workflow, deployment, or other institutional-state mutation.
- Changes to the fixed 30-phase roadmap, the Master Specification, or any constitutional invariant before this amendment is approved through Change Control.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: owns Create Internal Service orchestration and binds planning to the governed intent and all preceding evidence-bearing snapshots.
- `Platform.SoftwareFactory/Delivery`: remains the deterministic workflow and stage-order authority; planning requires a deployment-controlled run stopped at `ApprovedPackages`, but this increment cannot advance it.
- `Platform.SoftwareFactory/AiDevelopment`: remains the vendor-neutral planning runtime, independent evaluation, and non-executable candidate boundary.
- `Platform.SoftwareFactory/Packages`: supplies the exact approved package coordinates from the authoritative selection snapshot without registry mutation or package transfer.
- `Platform.Identity`: supplies the already-authenticated governed request context and access checks; it does not parse credentials in this increment.
- `Platform.Governance`: remains the signed-policy verification and OPA authority. AI runtime, API, prompt template, and evaluator cannot grant planning or workflow authority.
- `Platform.Knowledge` and `Platform.EnterpriseModel`: retain authorized context and contextual source-of-truth responsibilities; context is re-authorized before AI release and is not mutated.
- `Platform.Api`: composes the protected REST boundary without moving policy, workflow, prompt, runtime, evaluation, authorization, or evidence authority into the API layer.
- `Platform.Web`: communicates planning availability and fail-closed state without claiming a model, prompt registry, or runtime is connected.
- `Platform.Evidence`: remains the cryptographic evidence authority; no fabricated evidence implementation is introduced.
- OpenAPI and verification scripts: record and prove the protected contract, ordering, non-executable output, evaluation, and non-regression boundaries.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity provider configuration.
- A sovereign signed-policy verifier and OPA evaluation adapter.
- A deployment-approved Authorized Approved Packages snapshot reader.
- A deployment-controlled deterministic Software Delivery Run reader with an evidence-bearing run stopped at `ApprovedPackages`.
- A governed prompt-template registry and signature/integrity verifier.
- A policy-authorized AI-context assembler and per-item context authorizer.
- A sovereign or air-gapped-compatible implementation of `IAiDevelopmentRuntime` for planning.
- An independent `IAiOutputEvaluator` implementation.
- A policy-authorized planning-result evaluator.
- A sovereign cryptographic evidence implementation when authoritative evidence persistence is required.

Until these dependencies are configured, AI Planning must return unavailable or denied without AI invocation, context disclosure, workflow advancement, institutional mutation, or external effect.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- The Enterprise Model remains the contextual source of truth; AI receives only policy-authorized, re-authorized snapshot context and cannot mutate it.
- The existing `GovernedAiDevelopmentService`, `IAiDevelopmentRuntime`, and `IAiOutputEvaluator` boundaries remain authoritative; no competing AI runtime abstraction is introduced.
- The deterministic delivery engine remains workflow authority. The LLM, AI runtime, evaluator, API, and UI cannot select or advance the next stage.
- OPA remains Policy Authority and is evaluated before prompt/context release or runtime invocation.
- Planning and code generation remain separate task kinds and gates; this amendment authorizes planning only.
- The planning candidate is non-executable, produces no generated files, has no tools or credentials, and cannot create a workflow completion receipt.
- Independent evaluation covers every Phase 11 criterion and cannot be self-attested by the generation runtime.
- PostgreSQL, Neo4j, conditional vector stores, Agent Framework candidacy, and all other technology decisions remain unchanged.
- Evidence remains tenant-scoped, classified, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable through existing abstractions.
- Sovereign and air-gapped operation gains no mandatory external model, API, SaaS, licensing, telemetry, or control-plane dependency.
- No direct `AI -> Production` path, material action, external effect, or advancement beyond AI Planning is introduced.

## 4. Decision

Approve one additional CR-001 product increment limited to **Governed AI Planning Candidate**.

Approval would authorize implementation, verification, source control, and governed GitHub synchronization for this increment only. It would not authorize a concrete model or provider, credentials, public deployment, context outside authorized scope, tools, workflow advancement, Code Generation, code execution, institutional mutation, or any later station.

Decision: **Approved by the repository owner in the active delivery record**.

## 5. Master Specification Update

Append the following paragraph to the CR-001 business implementation addendum in `docs/PROJECT_MASTER_SPECIFICATION_V2.md`:

> Operational Increment 08 is **Governed AI Planning Candidate**. It defines a protected planning request bound to a previously released Approved Packages snapshot and a deterministic delivery run stopped at `ApprovedPackages`. Verified OPA policy authorizes the exact prompt template, runtime profile, context references, approved package coordinates, constraints, tenant, purpose, environment, and classification before context release or AI invocation. The vendor-neutral AI runtime may produce only a non-executable planning candidate with no generated files, tools, workflow authority, or external effect. Independent evaluation across all required criteria and per-result authorization are mandatory before deterministic, evidence-bearing release. Missing Approved Packages read, delivery-run read, sovereign policy, prompt-template, context-authorization, AI runtime, independent-evaluation, result-authorization, or evidence adapters fail closed without AI invocation, context disclosure, workflow advancement, or institutional mutation. The result cannot advance to Code Generation, execute code, or perform material action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 08 — Governed AI Planning Candidate.
- Authorization: implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved in the active Codex delivery record on 2026-09-01.**

## Acceptance criteria

1. Planning requires an authenticated subject, matching tenant, sufficient classification clearance, explicit AI Planning permission, purpose, authorization evidence, and matching governed-intent and all preceding snapshot identities, versions, digests, and evidence.
2. The authoritative Approved Packages snapshot and deterministic delivery run are loaded through deployment-controlled read boundaries and structurally revalidated; caller-supplied snapshot or run content cannot become authoritative.
3. The delivery run is valid, tenant-matching, evidence-bearing, and stopped exactly at `DeliveryStage.ApprovedPackages`; any other stage fails before prompt/context release or AI invocation.
4. A governed prompt template is loaded by exact identity and version and must pass signature, digest, tenant, purpose, environment, classification, lifecycle, and planning-task verification.
5. A verified signed-policy reference and evidence-bearing OPA permit define exact prompt-template digest, runtime profile, context references, approved package coordinates, constraints, tenant, purpose, environment, classification, and planning-output scope before context release or AI invocation.
6. OPA denial, signature failure, mismatched decision, invalid prerequisite, missing scope, stale prompt, unavailable context authorization, or unavailable runtime/evaluator dependency cannot invoke AI.
7. Every AI-context item is derived from the authorized prerequisite chain, matches policy scope, and is re-authorized immediately before runtime release; one denied or mismatched item causes a hard failure.
8. The existing vendor-neutral AI development boundary is invoked only with `AiDevelopmentTaskKind.Planning`, the verified prompt, re-authorized context references, exact approved package coordinates, and policy-authorized constraints.
9. The runtime candidate matches the authorized runtime profile, has a non-empty invocation identity and content, returns only authorized context references, contains no generated file paths, and has no tool call, command, credential, workflow authority, or external effect.
10. Independent evaluation is required for grounding, correctness, security, policy compliance, package compliance, and traceability; every criterion passes with non-placeholder evidence and the evaluator is independent from the runtime.
11. Any incomplete, failing, self-evaluated, mismatched, over-classified, package-noncompliant, ungrounded, untraceable, or unauthorized candidate causes a hard failure; every accepted candidate is re-authorized before release.
12. The returned receipt is bound to all prerequisite snapshots, delivery run, policy, prompt, runtime invocation, candidate, evaluation, and authorization; carries a SHA-256 digest and non-placeholder cryptographic evidence; and records `IsExecutable: false` and `CanAdvance: false`.
13. Missing package-snapshot reader, delivery-run reader, OPA, prompt registry/verifier, context assembler/authorizer, AI runtime, independent evaluator, result authorizer, or evidence recorder remains visible and fails closed without AI invocation, context disclosure, workflow advancement, or institutional mutation.
14. No Code Generation, generated file, tool use, command execution, package operation, sandbox, workflow completion or advancement, Git, CI/CD, deployment, institutional mutation, material action, new package, new project, or architectural deviation is introduced.
15. OpenAPI 3.1 and the Blazor experience expose the boundary without claiming model, prompt registry, runtime, or operational readiness.
16. All prior phase and Increment 01–07 acceptance gates remain satisfied.
17. `scripts/verify-project.ps1` succeeds with all 15 projects at zero warnings and zero errors, and runtime verification proves protected and fail-closed behavior.
