# CR-009 — Operationalization Wave 08: Governed Code Generation Runtime

Status: **Approved for Operationalization Wave 08**

Preceding completed authority: `docs/change-control/CR-008-OPERATIONALIZATION-WAVE-07.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Operationalize only Governed Code Generation by connecting the existing Increment 09 contract to the immutable Wave 07 AI Planning evidence, exact Approved Packages evidence, a deterministic delivery-run snapshot stopped at `AiPlanning`, action-specific signed-bundle/OPA authorization, a signed Code Generation prompt registry, exact evidence-chain context, a provider-neutral sovereign HTTPS generation protocol, an independently operated evaluator, per-result RBAC/ABAC authorization, and immutable PostgreSQL evidence.

Wave name: **Operationalization Wave 08 — Governed Code Generation Runtime**.

## 2. Approved implementation boundary

- Read the exact tenant/purpose-bound AI Planning evidence and Approved Packages snapshot and cryptographically revalidate their identities, digests, evidence, and relationship.
- Read a deployment-controlled delivery-run snapshot bound to the exact planning candidate, package selection, purpose, and run and stopped at `AiPlanning`; expose no run writer or advancement.
- Add a distinct Code Generation OPA scope for exact prompt identity/digest, runtime profile, context, packages, constraints, normalized repository-relative output paths, required roles, inert-code output kind, classification, and deployment safety bounds. Every other action rejects the scope.
- Read only an active, Code-Generation-approved, tenant/purpose/environment-scoped prompt whose content digest and signature verify under deployment-pinned trust.
- Load context only by exact immutable evidence reference and re-authorize each item immediately before runtime disclosure.
- Use built-in .NET `HttpClient`, `System.Text.Json`, and cryptography for a provider-neutral HTTPS JSON protocol. Generation and evaluation have distinct configured endpoints, profiles, operators, and signing key sets; redirects, cookies, tools, commands, and runtime-selected destinations are forbidden.
- Return only signed inert content and the exact policy-approved normalized paths as data. No filesystem writer, patch applier, command runner, compiler, package operation, Git, CI/CD, sandbox, deployment, credential, or tool gateway is connected.
- Require an independent signed evaluation containing exactly grounding, correctness, security, policy compliance, package compliance, and traceability, all passing with evidence.
- Re-authorize the result and exact path set, then append immutable, idempotent, tenant-scoped PostgreSQL evidence.
- Compose adapters only under complete valid PostgreSQL, policy, package-trust, AI Planning, Code Generation prompt/runtime/evaluator trust configuration. Static Validation and `IAuthorizedCodeGenerationCandidateReader` remain disconnected.

## 3. Security and architecture decision

This change conforms without architectural deviation. OPA remains policy authority, the delivery engine remains workflow authority, the Enterprise Model/evidence chain remains contextual authority, and AI has neither workflow nor production authority. The code candidate is never applied or executed. Repository defaults contain no provider, model, endpoint, credential, trust key, prompt, context, or institutional data and remain fail closed.

No new package, project, service boundary, database, queue, conditional technology, numerical SLO, benchmark value, retry value, or provider selection is approved. Existing locked `Npgsql` `10.0.3`, ASP.NET Core, and built-in .NET facilities are reused.

## 4. Out of scope

- Static or Security Validation, sandbox, tests, human review, Git, CI/CD, artifact, deployment, registration, workflow advancement, institutional mutation, external effect, or production action.
- Any source write, patch, create, replace, delete, rename, move, formatting, compile, restore, package transfer, execution, or publication initiated by the generated candidate.
- Prompt authoring/approval, live schema application, infrastructure provisioning, credential/trust issuance, model/provider selection, public endpoint, public deployment, or institutional data import.
- Unrestricted retrieval, caller-supplied authoritative context, tool/MCP/function calling, shell, filesystem, browser, secret access, or autonomous action.

## 5. Decision

Decision: **Approved by the repository owner on 2026-09-08 for bounded Wave 08 implementation, local verification, source control, and GitHub synchronization.**

The active delivery instruction “اعتمد وكمل” supplies the repository-owner approval. Deployment configuration and live external effects remain separately controlled.

## Acceptance gate

1. All preceding gates remain satisfied and repository defaults keep exactly 142 institutional dependencies fail closed.
2. AI Planning, Approved Packages, and delivery-run snapshots are exact, tenant/purpose/digest/evidence bound before OPA; the run stops exactly at `AiPlanning`.
3. Signed-bundle verification and exact `internal-service.code-generation.create` OPA scope precede prompt/context access and AI invocation; denial carries no scope.
4. The exact Code Generation prompt is scoped, active, approved, digest verified, and signature verified.
5. Every context item is loaded only from immutable prerequisite evidence, digest verified, and independently RBAC/ABAC re-authorized before disclosure.
6. Generation uses a fixed configured sovereign HTTPS endpoint, exact profile, verified prompt/context, exact packages/constraints/paths, and policy-bound positive safety limits.
7. The signed response binds invocation, input/output, context, packages, paths, operator, profile, evidence, and time; tools are forbidden and returned paths exactly equal policy scope.
8. Generated paths are unique, normalized, repository-relative, and outside prohibited repository, secret, binary, and generated-output locations.
9. Generation/evaluation endpoints, profiles, operators, and signing keys are distinct; exactly all six independent criteria pass with rationale and evidence.
10. Every candidate and path set is result-authorized before release and atomically evidenced with deterministic SHA-256 identity.
11. The receipt remains `IsExecutable: false`, `IsApplied: false`, and `CanAdvance: false`; no filesystem or later-station capability is connected.
12. Missing, stale, substituted, unsigned, mismatched, malformed, denied, or unavailable data releases no partial candidate and causes no mutation or external effect.
13. No secret, endpoint detail, trust material, prompt/context content, raw policy input, or unauthorized result is disclosed or committed.
14. All 15 projects build with zero warnings and zero errors and the complete runtime/acceptance verifier succeeds.
