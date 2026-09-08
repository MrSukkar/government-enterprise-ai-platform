# CR-008 — Operationalization Wave 07: Governed AI Planning Runtime

Status: **Approved for Operationalization Wave 07**

Preceding completed authority: `docs/change-control/CR-007-OPERATIONALIZATION-WAVE-06.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Operationalize only the AI Planning station of the approved Create Internal Service path by connecting it to the exact evidence-bearing Approved Packages snapshot produced by Wave 06, a deterministic delivery-run snapshot stopped at `ApprovedPackages`, the sovereign signed-bundle/OPA boundary, a signed governed prompt registry, exact authorized context material, a provider-neutral sovereign planning protocol, an independently operated evaluator, per-result RBAC/ABAC re-authorization, and immutable PostgreSQL planning evidence.

Wave name: **Operationalization Wave 07 — Governed AI Planning Runtime**.

### Outcome

When—and only when—deployment-approved identity, policy, PostgreSQL, prompt trust, AI-generation, independent-evaluation, authorization, and evidence profiles are complete, an authorized caller can request a non-executable planning candidate. The platform revalidates every authoritative prerequisite, obtains an exact action-specific OPA scope, loads and verifies the exact signed prompt, loads only exact policy-authorized context material, re-authorizes every context item immediately before disclosure, invokes a fixed sovereign planning-only protocol with tools disabled, independently evaluates the candidate through a separately configured and trusted evaluator, re-authorizes the result, and atomically records deterministic evidence.

Any missing configuration, prerequisite mismatch, policy denial, stale or substituted prompt/context, unsigned response, generation/evaluation identity collision, incomplete evaluation, failed criterion, unexpected generated path or tool request, authorization rejection, malformed data, or persistence ambiguity fails closed without releasing a candidate.

## 2. Current-state evidence

- All 30 phases, CR-001 Operational Increments 01–22, and Operationalization Waves 01–06 are complete and verified.
- Wave 06 persists the authoritative Approved Packages selection but deliberately leaves `IAuthorizedApprovedPackagesSnapshotReader` disconnected.
- `IAiPlanningDeliveryRunReader`, `IAiPlanningPolicyGate`, `IGovernedPlanningPromptTemplateReader`, `IAiPlanningContextAuthorizer`, `IAiDevelopmentRuntime`, `IAiOutputEvaluator`, `IAiPlanningResultAuthorizer`, and `IAiPlanningEvidenceRecorder` remain disconnected.
- `GovernedAiPlanningEngine` already enforces the approved station order, independent evaluation, result authorization, deterministic evidence, `IsExecutable: false`, and `CanAdvance: false`.
- The current runtime request carries prompt and context references but not their verified content. Operational use therefore requires an explicit typed, authorized content-release boundary before AI invocation.
- No durable delivery-run persistence adapter is currently connected. Operational use requires an exact deployment-controlled run snapshot stopped at `ApprovedPackages`; this wave does not create or advance a workflow.
- No AI provider, model, SDK, endpoint, credential, protocol adapter, prompt registry, or independent evaluator is selected or configured in repository defaults.

## 3. Impact Analysis

### In scope

- Implement `IAuthorizedApprovedPackagesSnapshotReader` over the Wave 06 PostgreSQL evidence table with exact tenant/selection binding, stored JSON SHA-256 verification, evidence-reference verification, and selection-digest recomputation.
- Add a read-only PostgreSQL delivery-run snapshot schema and implement `IAiPlanningDeliveryRunReader` with exact tenant/run binding, fixed parameterized SQL, stored JSON digest verification, and structural proof that history stops exactly at `ApprovedPackages`. Run creation, append, or advancement remains unavailable.
- Extend the sovereign typed OPA response envelope with an action-specific AI Planning scope for the exact prompt identity/digest, runtime profile, context references, package coordinates, constraints, classification, required roles, output kind, and configured deployment safety limits. Every other action adapter rejects this scope.
- Implement `IAiPlanningPolicyGate` for exact action `internal-service.ai-planning.create`, with signed-bundle verification before prompt/context read or AI invocation and exact request/decision/scope/evidence/time validation.
- Add a deployment-controlled read-only PostgreSQL governed prompt-template schema and exact reader. Prompt content must be active, planning-approved, tenant/purpose/environment/classification scoped, digest verified, and cryptographically signed under deployment-pinned public-key trust.
- Add a typed `AuthorizedAiPlanningContextItem` boundary carrying exact reference, source station, classification, SHA-256 digest, content, and evidence. Context material is loaded only from the immutable authorized evidence chain and every item is independently re-authorized through RBAC/ABAC immediately before runtime disclosure.
- Extend the existing vendor-neutral `AiDevelopmentRequest` only as required to carry the verified prompt content and authorized typed context items. It remains bound to `AiDevelopmentTaskKind.Planning` and exposes no tool, command, filesystem, package, Git, workflow, credential, or production capability.
- Implement a platform-owned provider-neutral sovereign HTTPS JSON protocol using built-in `HttpClient` and `System.Text.Json`; add no AI SDK or vendor package. The configured endpoint and runtime/model deployment reference are deployment data, never repository defaults or policy authority.
- Require fixed deployment-configured HTTPS endpoint allowlists, exact runtime profile mapping, positive deployment-supplied request/response/time safety bounds, no redirects, no caller-controlled URL, and no ambient credential or public-service fallback. No numerical value is selected in source before institutional benchmarking.
- Require planning requests to declare tools disabled and generated files forbidden. Responses must bind the invocation, runtime profile, exact input digest, context digests, package coordinates, output digest, creation time, and evidence to a deployment-pinned signature.
- Implement `IAiOutputEvaluator` through a separately configured sovereign endpoint and trust identity. The evaluator endpoint, runtime profile, signing key, and operator identity must be distinct from generation. Its signed response must contain exactly one finding for each required criterion: grounding, correctness, security, policy compliance, package compliance, and traceability.
- Implement deterministic AI-context and planning-result authorizers through the existing RBAC/ABAC evaluator using authenticated identity, exact OPA roles/scope, classification, prompt, context, packages, runtime profile, and candidate digest.
- Add an explicit PostgreSQL migration and `IAiPlanningEvidenceRecorder` for immutable, idempotent, tenant-scoped planning candidates and SHA-256-qualified evidence references. Writes use explicit transactions and never retry an ambiguous mutation.
- Register the planning runtime contracts only under complete valid policy, PostgreSQL, prompt-trust, generation, evaluator, and response-trust configuration. `IAuthorizedAiPlanningCandidateReader` and Code Generation remain disconnected.
- Add non-sensitive readiness, OpenAPI/UI wording, operational guidance, acceptance evidence, deterministic verifier checks, and atomic project-state updates after successful verification.

### Out of scope

- Selection of OpenAI, Azure OpenAI, Anthropic, Gemini, Bedrock, Ollama, vLLM, or any other provider, product, model, SDK, API key, credential format, SaaS control plane, or public endpoint.
- Agent Framework approval, agent execution, MCP, tools, function calling, shell, filesystem, browser, network destinations selected by the model, secret retrieval, live sessions, or autonomous action.
- Prompt creation, editing, approval, signing, revocation, institutional import, or prompt-authoring UI.
- Context retrieval outside the exact previously authorized evidence chain; Graph search, vector search, lexical search, unrestricted retrieval, or caller-supplied context becoming authoritative.
- Delivery-run creation, history append, retry, checkpoint, resume, timeout policy, stage completion, or workflow advancement.
- Code Generation, generated files, patches, commands, package transfer/installation/execution, validation, sandbox, Git, CI/CD, deployment, registration, mutation, external effect, or production action.
- A new project, service boundary, database, queue, cache, package dependency, numerical SLO, benchmark result, model-quality threshold, token value, timeout value, or retry value.
- External provisioning, live schema application, institutional data import, endpoint selection, certificate issuance, trust-key selection, credentials, public deployment, or production traffic.

### Affected boundaries

- `Platform.SoftwareFactory/InternalService` owns orchestration, exact prerequisite verification, action-specific policy, context release, evaluation, result authorization, and evidence semantics.
- `Platform.SoftwareFactory/AiDevelopment` remains the provider-neutral planning runtime and independent-evaluation boundary; it gains typed verified input, not policy or workflow authority.
- `Platform.SoftwareFactory/Delivery` remains deterministic workflow authority; this wave reads an authoritative stopped snapshot and cannot mutate it.
- `Platform.Governance` owns only bounded policy transport; OPA remains policy authority.
- `Platform.Identity` remains RBAC/ABAC authority.
- `Platform.Api` conditionally composes valid adapters and exposes non-sensitive readiness.
- PostgreSQL stores governed prompt metadata, delivery-run snapshots, and immutable planning evidence; it is not a model runtime.

## 4. Data and security review

1. Approved Packages, delivery run, prompt, context, policy, runtime, evaluation, authorization, and evidence identities/digests are exact and tenant scoped.
2. Signed-bundle verification and exact OPA scope precede prompt/context reads and every AI call; denial carries no operational scope.
3. Prompt and context SQL is fixed, parameterized, bounded, tenant/purpose/environment/classification scoped, and read only.
4. Raw caller prompt or context content cannot become authoritative or reach the runtime.
5. The generation adapter accepts no caller-controlled endpoint and sends only policy-authorized planning data to a configured sovereign allowlist target.
6. Tools, generated files, commands, credentials, workflow authority, and external actions are structurally absent and explicitly forbidden by the protocol.
7. Generation and evaluation use distinct configured endpoints, profiles, operator identities, and signing keys; one runtime cannot self-attest independence.
8. Signed responses bind canonical request and result digests, identities, times, and evidence to deployment-pinned trust.
9. Every context item and final candidate is independently RBAC/ABAC-authorized before disclosure or release.
10. Planning evidence is deterministic, append-only, tenant scoped, idempotent, transactionally committed, and cryptographically addressable.
11. Endpoint details, credentials, trust material, prompt content, context content, raw policy inputs, and model responses are not exposed through readiness or logs.

## 5. Proposed implementation sequence

1. Add exact PostgreSQL Approved Packages and delivery-run snapshot readers.
2. Add the distinct typed AI Planning OPA scope and action-specific gate.
3. Add signed prompt-template persistence, trust configuration, verification, and exact reader.
4. Add typed authorized context material and per-item RBAC/ABAC authorization.
5. Extend the vendor-neutral planning request with verified prompt/context content.
6. Implement the signed sovereign generation protocol and separately trusted independent evaluator.
7. Implement result authorization and immutable PostgreSQL planning evidence.
8. Compose only under complete valid configuration; keep Code Generation disconnected.
9. Update readiness, OpenAPI, UI, operational documentation, acceptance verification, and project state.
10. Run the complete verifier, then commit and synchronize only verified source.

## 6. Architectural Review

Finding: **Conforms without architectural deviation if explicitly approved and verified**.

- .NET `HttpClient`, `System.Text.Json`, PostgreSQL, OPA, RBAC/ABAC, cryptographic trust, and the existing modular-monolith boundaries are reused.
- The platform-owned bounded protocol avoids a vendor SDK or mandatory external control plane and can terminate at a sovereign or air-gapped runtime.
- The Enterprise Model and authorized evidence chain remain contextual authority; AI receives only re-authorized copies and cannot mutate them.
- OPA remains policy authority, the delivery engine remains workflow authority, and the independent evaluator cannot grant execution authority.
- No direct `AI -> Production` path, tool capability, source mutation, stage advancement, or production action is introduced.

## 7. Package decision

No new package is requested. Wave 07 uses locked `Npgsql` `10.0.3`, ASP.NET Core `HttpClient`, `System.Text.Json`, and built-in .NET cryptography. No AI-provider SDK, agent SDK, resilience package, ORM, cloud SDK, or external evaluator package is proposed.

## 8. Decision requested

Approve **CR-008 — Operationalization Wave 07: Governed AI Planning Runtime** for bounded implementation, local verification, source control, and GitHub synchronization only.

Decision: **Approved by the repository owner on 2026-09-08 for bounded Wave 07 implementation, local verification, source control, and GitHub synchronization.**

Approval would not select a provider, model, endpoint, credential, trust key, institutional prompt/context data, benchmark value, or public deployment. Those remain deployment-controlled inputs requiring separate authority.

## Acceptance gate

1. Repository-default and incomplete profiles remain unconfigured and fail closed; the disconnected dependency count remains exactly 142.
2. Exact Approved Packages and delivery-run snapshots are cryptographically revalidated before OPA; the run is stopped exactly at `ApprovedPackages`.
3. Signed-bundle verification and exact action-specific OPA scope precede prompt/context access or AI invocation; denial carries no scope.
4. The exact prompt is active, planning-approved, scoped, digest verified, and signature verified under deployment-pinned trust.
5. Every exact context item comes only from the immutable authorized evidence chain and is digest verified and re-authorized immediately before disclosure.
6. Generation uses only the configured provider-neutral sovereign planning protocol, exact runtime profile, verified prompt/context, approved package coordinates, constraints, and deployment safety bounds.
7. No tool, command, credential, generated file, workflow authority, mutation, external action, or production capability is present in the planning request or result.
8. The generation response is signed and canonically bound to its exact request, runtime identity/profile, input/output digests, context, packages, time, and evidence.
9. Independent evaluation is separately configured and signed; generation and evaluation endpoints, profiles, operator identities, and keys are distinct.
10. Exactly all required evaluation criteria pass with non-placeholder rationale and cryptographic evidence.
11. Every candidate is result-authorized using authenticated identity and exact policy scope before release.
12. Missing, duplicate, substituted, unexpected, stale, unsigned, untrusted, self-evaluated, over-classified, failed, or malformed data releases no partial candidate.
13. Planning evidence is deterministic, immutable, tenant scoped, idempotent, atomic, and SHA-256 qualified.
14. `IsExecutable: false`, `CanAdvance: false`, `IAuthorizedAiPlanningCandidateReader` disconnected, and Code Generation unavailable are structurally verified.
15. No secret, endpoint detail, trust material, prompt/context content, raw policy input, unauthorized result, command, session, mutation, or external effect is disclosed or committed.
16. All preceding gates remain satisfied; all 15 projects build with zero warnings and zero errors; runtime verification proves the safe default posture.
