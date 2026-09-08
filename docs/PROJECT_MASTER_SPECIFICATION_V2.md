# PROJECT MASTER SPECIFICATION v2 — APPROVED

This is the only implementation reference. The approved 30-phase order is fixed.

## Core model

`BUILD <-> UNDERSTAND <-> OPERATE <-> ACT`

- Enterprise Model connects systems and institutional context.
- Governance connects intelligence to permitted action.
- Evidence connects action to trust.
- Software Factory connects AI to the software-delivery lifecycle.

## Approved technology baseline

| Area | Decision |
|---|---|
| Backend | .NET 10 / ASP.NET Core |
| Architecture | Modular Monolith |
| Frontend | Blazor WebAssembly |
| API | REST + OpenAPI 3.1 |
| Primary DB | PostgreSQL |
| Enterprise Graph | Neo4j |
| Retrieval | Abstraction; Neo4j baseline |
| pgvector / Qdrant | Conditional only |
| Knowledge | Controlled GraphRAG |
| Policy | OPA |
| Agent Runtime | Abstraction; Agent Framework candidate |
| Durable Execution | Required |
| Sandbox | Firecracker-class |
| Telemetry | OpenTelemetry |
| Logs / Traces | OpenSearch |
| Metrics | Prometheus |
| Identity | OIDC/OAuth2 + RBAC/ABAC |
| Source | Git |
| CI/CD | DevSecOps |
| Evidence | Cryptographic / tamper-evident |
| Deployment | Air-gapped / on-premises ready |

## Enterprise Model

An Enterprise Object contains Identity, Type, State, Owner, Classification, Relationships, Policies, Actions, Source, Confidence, Evidence, Lifecycle, and Timestamps. Relationships are `CONFIRMED`, `DISCOVERED`, `INFERRED`, or `UNKNOWN`.

## Retrieval authorization

`User/Agent -> Identity -> Purpose -> Policy -> Authorized Scope -> Graph/Vector/Lexical -> Fusion/Reranking -> Authorization Re-check -> AI Context`

Retrieval is never “search everything, filter later.”

## Agentic work and governance

`Enterprise Workflow -> Deterministic Step/Approval/AI Step -> Agent Runtime -> Tool Gateway -> Identity -> OPA -> Action -> Evidence`

The LLM has no workflow authority. AI Runtime is not the Policy Authority. OPA policies are versioned, signed, verified, auditable, and environment-aware. The domain core must not bind directly to a vendor runtime.

Durable execution requires persistent state, checkpoints, resume, retry, timeout, human approval, failure recovery, and idempotency. Agent state is enterprise data governed by classification, retention, encryption, authorization, tenant isolation, evidence, and deletion policy.

## Software Factory

`Intent -> Enterprise Context -> Existing Architecture -> Approved Packages -> AI Planning -> Code Generation -> Static Validation -> Security Validation -> Sandbox -> Tests -> Human Review -> Git -> CI/CD -> Artifact -> Deployment -> Registration -> Observability -> Evidence`

There is no direct `AI -> Production` path. The sandbox is ephemeral Firecracker-class isolation with no production credentials, restricted networking, isolated filesystems, and CPU, memory, and time limits. WASM is specialized, not the general .NET sandbox.

## Observability and evidence

`Applications -> OpenTelemetry SDK -> Collector Agent -> Trace-aware Routing -> Collector Gateway -> Processing -> Logs/Metrics/Traces -> Storage -> Enterprise Model`

No numerical SLO is approved before workload benchmarking.

Evidence is append-only, tamper-evident, cryptographically verifiable, signed, ordered, traceable, and access-controlled:

`Request -> Context -> Knowledge -> Decision -> Policy -> Approval -> Action -> Result -> Telemetry -> Evidence`

## Security, supply chain, and sovereignty

Cross-cutting foundations are Identity, RBAC, ABAC, Zero Trust, PKI, HSM/key management, secrets management, workload identity, encryption, separation of duties, audit, and evidence.

`Source -> Provenance -> SBOM -> Dependency Validation -> Build -> Build Attestation -> Artifact Signing -> Registry -> Verification -> Deployment`

This applies to containers, NuGet, AI models, policies, frontend dependencies, and sandbox images.

The platform supports cloud, private cloud, hybrid, on-premises, air-gapped, and sovereign operation. Air-gapped environments use local model runtime, registry, policy, identity, evidence, and observability, with no external API, AI, SaaS, or control-plane dependency.

## Phase capability mapping

| Phase | Final role |
|---|---|
| 01 | Product Constitution |
| 02 | UX & Roles |
| 03 | Backend Foundation |
| 04 | Frontend Foundation |
| 05 | Identity & Access + PKI + separation of duties |
| 06 | OpenAPI contract + platform boundaries |
| 07 | Enterprise Model base |
| 08 | Knowledge + GraphRAG + retrieval authorization |
| 09 | Institutional package registry |
| 10 | Software Factory governance + supply chain |
| 11 | AI development + agent runtime + AI evaluation |
| 12 | Validation + security sandbox |
| 13 | Git + CI + provenance + SBOM + signing |
| 14 | Sovereign deployment |
| 15 | OpenTelemetry + redaction |
| 16 | Central observability |
| 17 | Automatic registration |
| 18 | Understanding engine |
| 19 | Agentic work + durable execution + agent state |
| 20 | Governance + OPA + actions + MCP |
| 21 | Enterprise modeling + impact analysis |
| 22 | Simulation + resilience + disaster recovery |
| 23 | Proactive intelligence |
| 24 | Government productization + compliance |
| 25 | Front Door |
| 26 | Developer Experience |
| 27 | Closed Loop |
| 28 | Final System Architecture |
| 29 | Vertical Slice |
| 30 | Evidence Engine + cryptographic proof |

## Approved vertical slice

`Developer -> Create Internal Service -> Enterprise Context -> Existing Systems -> Existing Architecture -> Approved Packages -> AI Planning -> Code Generation -> Validation -> Security -> Sandbox -> Human Review -> Git -> CI/CD -> Deployment -> OpenTelemetry -> Automatic Registration -> Enterprise Model -> Evidence`

This proves `BUILD -> REGISTER -> OPERATE -> PROVE`, then expands to `UNDERSTAND -> ANALYZE -> DECIDE -> ACT`.

## Change control

`Change Request -> Impact Analysis -> Architectural Review -> Decision -> Master Specification Update -> Approval`

No architectural deviation is permitted outside this process.

## Approved business implementation addendum — CR-001

The first business/domain implementation is **Create Internal Service Workspace**. It is delivered as product increments and does not create Phase 31.

Operational Increment 01 is the **Governed Intent Workspace**. It exposes a non-authoritative product preview, validates service intent, and publishes the approved delivery sequence. Material execution remains unavailable until governed identity, tenant, purpose, classification, OPA policy, required human approval, and runtime readiness are established.

Operational Increment 02 is **Governed Intent Submission**. It adds a protected REST contract and server-side validation of authenticated subject, tenant, purpose, classification clearance, explicit permission, authorization evidence, and intent evidence. The result is a deterministic validation receipt only: it is not persisted, is not an OPA decision, grants no workflow authority, and cannot execute material work. Until a governed OIDC/OAuth2 adapter is configured, authentication fails closed.

Operational Increment 03 is **Governed Intent Registration**. It defines an OPA-gated registration command, deterministic idempotency, optimistic version checks, and an atomic repository boundary that must commit the governed intent with cryptographic registration evidence. OPA denial cannot reach persistence. Missing sovereign policy or repository adapters return service unavailable without mutation. No fake persistence, external credential, AI step, workflow advancement, or material execution is introduced.

Operational Increment 04 is **Authorized Enterprise Context Discovery**. It defines a protected, OPA-scoped discovery request for a previously registered governed intent. Authorization is required before any Enterprise Model or knowledge source access and is re-checked for every candidate before release into a deterministic, evidence-bearing context snapshot. Missing registered-intent, sovereign policy, retrieval, or evidence adapters fail closed without source access or institutional mutation. The result cannot advance to Existing Systems, AI planning, code generation, workflow execution, or material action.

Operational Increment 05 is **Authorized Existing Systems Discovery**. It defines a protected, OPA-scoped inventory request bound to a previously released, evidence-bearing Enterprise Context snapshot. Verified policy establishes explicit system-object, relationship, source, tenant, purpose, and classification scope before any inventory source access. Every returned system and relationship is structurally validated and re-authorized before release into a deterministic, evidence-bearing snapshot. Missing context-read, sovereign policy, inventory-source, authorization, or evidence adapters fail closed without source access or institutional mutation. The result cannot advance to Existing Architecture, AI planning, code generation, workflow execution, or material action.

Operational Increment 06 is **Authorized Existing Architecture Discovery**. It defines a protected, OPA-scoped architecture request bound to a previously released, evidence-bearing Existing Systems snapshot. Verified policy establishes explicit system, architecture-source, component, dependency, interface, constraint, decision-reference, tenant, purpose, and classification scope before any architecture source access. Every released architecture item is bound to an authorized existing system, structurally validated, checked for constitutional conformance, and re-authorized before release into a deterministic, evidence-bearing snapshot. Missing Existing Systems read, sovereign policy, approved architecture source, result-authorization, conformance, or evidence adapters fail closed without source access or institutional mutation. The result cannot advance to Approved Packages, AI planning, code generation, workflow execution, or material action.

Operational Increment 07 is **Governed Approved Packages Selection**. It defines a protected, OPA-scoped exact-package request bound to a previously released, evidence-bearing Existing Architecture snapshot. Verified policy establishes an explicit set of immutable package coordinates, kinds, tenant, purpose, environment, classification, and result scope before any institutional registry read. Every registry record must exactly match the authorized coordinate and pass institutional approval, expiry, tenant, environment, sovereign-copy, provenance, SBOM, signature, supply-chain, and per-result authorization checks before release into a deterministic, evidence-bearing snapshot. Missing Existing Architecture read, sovereign policy, institutional registry, eligibility, supply-chain assurance, result-authorization, or evidence adapters fail closed without registry access, package transfer, or institutional mutation. The result cannot advance to AI Planning, code generation, workflow execution, or material action.

Operational Increment 08 is **Governed AI Planning Candidate**. It defines a protected planning request bound to a previously released Approved Packages snapshot and a deterministic delivery run stopped at `ApprovedPackages`. Verified OPA policy authorizes the exact prompt template, runtime profile, context references, approved package coordinates, constraints, tenant, purpose, environment, and classification before context release or AI invocation. The vendor-neutral AI runtime may produce only a non-executable planning candidate with no generated files, tools, workflow authority, or external effect. Independent evaluation across all required criteria and per-result authorization are mandatory before deterministic, evidence-bearing release. Missing Approved Packages read, delivery-run read, sovereign policy, prompt-template, context-authorization, AI runtime, independent-evaluation, result-authorization, or evidence adapters fail closed without AI invocation, context disclosure, workflow advancement, or institutional mutation. The result cannot advance to Code Generation, execute code, or perform material action.

Operational Increment 09 is **Governed Code Generation Candidate**. It defines a protected code-generation request bound to a previously released AI Planning candidate and a deterministic delivery run stopped exactly at `AiPlanning`. Verified OPA policy authorizes the exact planning digest, code-generation prompt, runtime profile, context references, approved packages, constraints, permitted relative output paths, tenant, purpose, environment, and classification before context release or AI invocation. The vendor-neutral runtime may return only a non-executable, unapplied code candidate; generated paths are normalized, repository-relative, policy-scoped data and no filesystem, tool, command, package, Git, CI/CD, sandbox, or deployment access is exposed. Independent evaluation and per-result authorization are mandatory before deterministic evidence-bearing release. Missing prerequisite-read, policy, prompt, context-authorization, runtime, evaluator, result-authorization, or evidence adapters fail closed without AI invocation, context disclosure, source mutation, workflow advancement, or external effect. The result cannot advance to Static Validation or perform material action.

Operational Increment 10 is **Governed Static Validation**. It defines a protected Static Validation request bound to a previously released Code Generation candidate and a deterministic delivery run stopped exactly at `CodeGeneration`. Verified OPA policy authorizes the exact candidate digest, required Static control identities, tenant, purpose, environment, classification, and evidence scope before candidate read or control execution. The authoritative inert candidate is loaded through a deployment-controlled reader and processed only by the existing vendor-neutral `CodeValidationPipeline` with `ValidationGate.Static`. Every exact required control must be present, complete, evidence-bearing, and free of Error or Critical findings; missing, substituted, incomplete, unevidenced, or blocking results fail closed. Result authorization and cryptographic evidence are mandatory before report release. No source mutation, command, durable artifact, package acquisition, code execution, workflow advancement, or external effect is available. The result cannot advance to Security Validation or perform material action.

Operational Increment 11 is **Governed Security Validation**. It defines a protected Security Validation request bound to an accepted Static Validation receipt, the authoritative inert Code Generation candidate, and a deterministic delivery run stopped exactly at `StaticValidation`. Verified OPA policy authorizes the exact candidate and Static-report digests, required Security-control identities, tenant, purpose, environment, classification, and evidence scope before prerequisite read or control execution. The existing vendor-neutral `CodeValidationPipeline` runs only with `ValidationGate.Security`; every exact required control must be present, complete, evidence-bearing, and free of Error or Critical findings. Result authorization and cryptographic evidence are mandatory before report release. No source mutation, dynamic tool acquisition, command, durable artifact, candidate execution, sandbox execution, workflow advancement, or external effect is available. The result cannot advance to Sandbox or perform material action.

Operational Increment 12 is **Governed Security Sandbox Execution**. It defines protected isolated execution bound to an accepted Security Validation receipt, an authoritative inert code candidate, and a deterministic delivery run stopped exactly at `SecurityValidation`. Verified OPA policy authorizes the exact candidate/security digests, institutional sandbox-image coordinate, isolation policy, non-secret environment references, network destinations, tenant, purpose, environment, classification, and evidence scope before prerequisite read, image access, or runtime invocation. The existing vendor-neutral `GovernedSandboxService` runs only through a sovereign-compatible `ISecuritySandboxRuntime` under Firecracker-class ephemeral microVM isolation with no production credentials, no host filesystem, network default deny, configured positive resource/time limits, and an exact approved supply-chain-assured image. Acceptance requires zero exit, no timeout, no isolation violation, result authorization, and cryptographic evidence. No production effect or workflow advancement is available. The result cannot advance to Tests or perform production action.

Operational Increment 13 is **Governed Tests Execution**. It defines protected deterministic tests bound to an accepted Sandbox receipt, the complete Security Validation evidence chain, an authoritative inert code candidate, and a deterministic delivery run stopped exactly at `Sandbox`. Verified OPA policy authorizes the exact prerequisite digests, governed test manifest, institutional image, isolation policy, non-secret environment references, network destinations, tenant, purpose, environment, classification, and evidence scope before reads, image access, or invocation. A vendor-neutral governed test runtime executes only the exact authorized manifest inside Firecracker-class ephemeral microVM isolation. Every required test must be uniquely discovered, completed, passed, and evidence-bearing; missing, duplicate, skipped, failed, timed-out, or isolation-violating results fail closed. Result authorization and cryptographic evidence are mandatory. No production effect or workflow advancement is available. The result cannot advance to Human Review or perform production action.

Operational Increment 14 is **Governed Human Review**. It defines a protected human approve/reject decision bound to accepted governed Tests, the complete Sandbox and validation evidence chain, an authoritative inert code candidate, and a deterministic delivery run stopped exactly at `Tests`. Verified OPA policy authorizes the exact human reviewer, review action, prerequisite digests, tenant, purpose, environment, classification, evidence scope, and separation-of-duties constraints before prerequisite read or candidate disclosure. The reviewer must differ from conflicting actors and provide explicit rationale plus a deployment-verifiable non-repudiable attestation bound to the complete review package. AI, policy, runtime, and API cannot supply or alter the human decision. The decision and cryptographic evidence are recorded atomically with deterministic idempotency and optimistic concurrency. No production effect or workflow advancement is available. The result cannot advance to Git or perform production action.

Operational Increment 15 is **Governed Git Source Commit**. It defines a protected source mutation bound to an approving Human Review receipt, accepted Tests, the authoritative code candidate, and a deterministic delivery run stopped exactly at `HumanReview`. Verified OPA policy authorizes the exact candidate/review digests, immutable change set, repository identity, base commit, non-protected change branch, commit metadata, tenant, purpose, environment, classification, and evidence scope before repository read or mutation. A vendor-neutral institutional Git gateway verifies the exact clean base, applies only policy-approved normalized paths and digests, rejects secrets and unrelated content, and creates one signed immutable commit without force update, protected-branch mutation, CI/CD invocation, or production effect. Result authorization and cryptographic evidence are mandatory. No workflow advancement is available. The result cannot advance to CI/CD or perform production action.

Operational Increment 16 is **Governed CI/CD Execution**. It defines a protected deterministic pipeline execution bound to the exact signed Git receipt and a delivery run stopped exactly at `Git`. Verified OPA policy authorizes the exact repository, commit, tree and change-set digests, immutable workflow definition and digest, pipeline profile, isolated runner pool, required stages and controls, tenant, purpose, environment, classification, and evidence scope before checkout or invocation. A vendor-neutral institutional CI/CD gateway executes only the approved workflow using locked dependencies and approved institutional sources, producing an exact evidence-bearing pipeline-output manifest with SBOM, checksums, provenance, attestations, and signatures. Result authorization and cryptographic evidence are mandatory. No source mutation, Artifact-stage publication, workflow advancement, deployment, or production effect is available. The result cannot advance to Artifact or perform production action.

Operational Increment 17 is **Governed Artifact Publication**. It defines protected publication of one exact immutable artifact from an accepted, non-released CI/CD pipeline-output manifest and a deterministic delivery run stopped exactly at `CiCd`. Verified OPA policy authorizes the exact prerequisite, source, workflow, manifest, artifact coordinate and digest, institutional registry, signing policy, supply-chain controls, tenant, purpose, environment, classification, and evidence scope before reads or registry access. The exact package is validated, published without overwrite through a vendor-neutral institutional gateway, and must pass source provenance, SBOM, dependency validation, build attestation, artifact signature, and registry verification. Result authorization and cryptographic evidence are mandatory. No deployment, production effect, or workflow advancement is available. The result cannot advance to Deployment or perform production action.

Operational Increment 18 is **Governed Sovereign Deployment**. It defines protected deployment of the exact immutable, supply-chain-verified Artifact to one exact policy-approved environment from a deterministic delivery run stopped at `Artifact`. Verified OPA policy authorizes the Artifact, registry reference, sovereign profile, topology, target, human approval, production intent, tenant, purpose, classification, and evidence before reads or invocation. The authoritative Artifact and profile are revalidated, institutional preflight is mandatory, and a vendor-neutral gateway returns idempotent runtime, activation, rollback, effect, and evidence proof. Result authorization and cryptographic evidence are mandatory. AI has no deployment or workflow authority. No OpenTelemetry, registration, Enterprise Model mutation, or workflow advancement is available.

Operational Increment 19 is **Governed OpenTelemetry Activation**. It defines protected activation of the exact approved telemetry profile for an accepted governed Deployment and a deterministic delivery run stopped at `Deployment`. Verified OPA policy authorizes the exact Deployment, runtime, Artifact, effect posture, telemetry profile, resource identity, required signals, collectors, redaction, tenant, purpose, classification, environment, and evidence before reads or configuration. Strict redaction and trusted sovereign collector routing are independently verified before a vendor-neutral gateway may configure traces, metrics, and logs and prove resource binding, baggage clearing, redaction, signal acceptance, and evidence. Result authorization and cryptographic evidence are mandatory. No Automatic Registration, Enterprise Model mutation, or workflow advancement is available.

Operational Increment 20 is **Governed Automatic Registration**. It defines protected deterministic registration of the exact deployed and observed service from an accepted OpenTelemetry receipt and a delivery run stopped at `OpenTelemetry`. Verified OPA authorizes the exact activation, runtime, Artifact, service key, signed registration manifest, owner, classification, policies, permitted actions, relationships, tenant, purpose, environment, and evidence before reads or mutation. The existing Phase 17 engine performs one atomic evidence-backed registration and validates the returned Enterprise Object. Result authorization and cryptographic evidence are mandatory. No workflow advancement, Enterprise Model contextualization, Evidence completion, understanding, inference, or autonomous action is available.

Operational Increment 21 is **Governed Enterprise Model Contextualization**. It defines protected confirmation of the exact atomically registered service as an authorized Enterprise Object from a delivery run stopped at `AutomaticRegistration`. Verified OPA authorizes the exact registration receipt, object identity, fingerprint, tenant, purpose, environment, classification, and evidence before reads. The object and its lifecycle, source, policies, actions, relationships, timestamps, and evidence are revalidated and result-authorized before an evidence-bearing contextualization receipt is released. No additional model mutation, workflow advancement, impact analysis, simulation, inference, action, or Evidence completion is available.

Operational Increment 22 is **Governed Evidence Completion**. It defines protected completion and cryptographic verification of the exact evidence chain for an accepted Enterprise Model contextualization and a delivery run stopped at `EnterpriseModel`. Verified OPA authorizes the exact chain, correlation, contextualization, payload digest, trace references, tenant, purpose, environment, classification, and evidence before reads or append. The existing Phase 30 engine atomically appends only the final `Evidence` entry and then verifies all ten ordered entries, hashes, links, signatures, authorization, scope, and completeness. Independent result authorization is mandatory before release. No workflow advancement, evidence update/deletion, model mutation, production action, or later station is available.

The governing path remains:

`Intent -> Enterprise Context -> Existing Systems -> Existing Architecture -> Approved Packages -> AI Planning -> Code Generation -> Validation -> Security -> Sandbox -> Tests -> Human Review -> Git -> CI/CD -> Artifact -> Deployment -> OpenTelemetry -> Automatic Registration -> Enterprise Model -> Evidence`

This addendum introduces no new project, service boundary, database, policy authority, workflow authority, AI-to-production path, or conditional technology decision. The Modular Monolith and all v2 constitutional controls remain unchanged.

Change-control record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`.
Increment 04 amendment: `docs/change-control/CR-001-AMENDMENT-01-ENTERPRISE-CONTEXT.md`.
Increment 05 amendment: `docs/change-control/CR-001-AMENDMENT-02-EXISTING-SYSTEMS.md`.
Increment 06 amendment: `docs/change-control/CR-001-AMENDMENT-03-EXISTING-ARCHITECTURE.md`.
Increment 07 amendment: `docs/change-control/CR-001-AMENDMENT-04-APPROVED-PACKAGES.md`.
Increment 08 amendment: `docs/change-control/CR-001-AMENDMENT-05-AI-PLANNING.md`.
Increment 09 amendment: `docs/change-control/CR-001-AMENDMENT-06-CODE-GENERATION.md`.
Increment 10 amendment: `docs/change-control/CR-001-AMENDMENT-07-STATIC-VALIDATION.md`.
Increment 11 amendment: `docs/change-control/CR-001-AMENDMENT-08-SECURITY-VALIDATION.md`.

Increment 12 amendment: `docs/change-control/CR-001-AMENDMENT-09-SANDBOX.md`.

Increment 13 amendment: `docs/change-control/CR-001-AMENDMENT-10-TESTS.md`.

Increment 14 amendment: `docs/change-control/CR-001-AMENDMENT-11-HUMAN-REVIEW.md`.

Increment 15 amendment: `docs/change-control/CR-001-AMENDMENT-12-GIT.md`.

Increment 16 amendment: `docs/change-control/CR-001-AMENDMENT-13-CICD.md`.

Increment 17 amendment: `docs/change-control/CR-001-AMENDMENT-14-ARTIFACT.md`.

Increment 18 amendment: `docs/change-control/CR-001-AMENDMENT-15-DEPLOYMENT.md`.

Increment 19 amendment: `docs/change-control/CR-001-AMENDMENT-16-OPENTELEMETRY.md`.

Increment 20 amendment: `docs/change-control/CR-001-AMENDMENT-17-AUTOMATIC-REGISTRATION.md`.

Increment 21 amendment: `docs/change-control/CR-001-AMENDMENT-18-ENTERPRISE-MODEL.md`.

Increment 22 amendment: `docs/change-control/CR-001-AMENDMENT-19-EVIDENCE-COMPLETION.md`.

## Approved operationalization addendum — CR-002

Operationalization Wave 01 introduces configuration-gated, standards-based bearer authentication and sovereign, vendor-neutral signed-policy-bundle and OPA adapters inside the existing Identity, Governance, and API module boundaries. Missing, incomplete, or unsafe deployment configuration preserves the fail-closed authentication and runtime posture. This addendum selects no provider, credential, trust anchor, deployment, organization-specific claim mapping, persistence technology, or new workflow authority.

Change-control record: `docs/change-control/CR-002-OPERATIONALIZATION-WAVE-01.md`.

## Approved operationalization addendum — CR-003

Operationalization Wave 02 connects the existing Governed Intent Registration contract to signed-bundle-bound OPA evaluation and an atomic tenant-scoped PostgreSQL repository. The adapters are composed only under complete, safe deployment configuration; repository defaults contain no connection or secret and remain fail closed. Schema application, external infrastructure, credentials, Enterprise Context reads, later stations, and production action remain outside this wave.

Change-control record: `docs/change-control/CR-003-OPERATIONALIZATION-WAVE-02.md`.

## Approved operationalization addendum — CR-004

Operationalization Wave 03 connects Authorized Enterprise Context Discovery to the exact tenant-scoped governed-intent record, signed-bundle-bound OPA scope, the Neo4j Enterprise Graph baseline, existing pre-access and per-result authorization, and immutable PostgreSQL context evidence. Only fixed, parameterized, scope-first Graph reads are enabled under complete deployment configuration. Graph mutation, unrestricted retrieval, vector/lexical technology, later stations, external provisioning, and production action remain outside this wave.

Change-control record: `docs/change-control/CR-004-OPERATIONALIZATION-WAVE-03.md`.

## Approved operationalization addendum — CR-005

Operationalization Wave 04 connects Authorized Existing Systems Discovery to the exact evidence-bearing Enterprise Context snapshot, signed-bundle-bound OPA scope, the Neo4j Enterprise Graph baseline, deterministic RBAC/ABAC re-authorization for every system and relationship, and immutable PostgreSQL inventory evidence. Graph access is fixed, parameterized, read-only, tenant scoped, exact-system scoped, relationship scoped, classification bounded, source scoped, and result bounded before retrieval. Repository defaults remain fail closed. Live connectors, Graph mutation, network probing, Existing Architecture and later stations, external provisioning, and production action remain outside this wave.

Change-control record: `docs/change-control/CR-005-OPERATIONALIZATION-WAVE-04.md`.

## Approved operationalization addendum — CR-006

Operationalization Wave 05 connects Authorized Existing Architecture Discovery to the exact evidence-bearing Existing Systems snapshot, signed-bundle-bound OPA scope, the Neo4j Enterprise Graph baseline, deterministic conformance to the Master Specification, per-item RBAC/ABAC re-authorization, and immutable PostgreSQL architecture evidence. Only fixed, parameterized, scope-first reads of already approved and versioned architecture facts are enabled under complete deployment configuration. Architecture creation, approval, redesign, source crawling, Graph mutation, Approved Packages and later stations, external provisioning, and production action remain outside this wave.

Change-control record: `docs/change-control/CR-006-OPERATIONALIZATION-WAVE-05.md`.

## Approved operationalization addendum — CR-007

Operationalization Wave 06 connects Governed Approved Packages Selection to the exact evidence-bearing Existing Architecture snapshot, signed-bundle-bound exact-coordinate OPA scope, a read-only PostgreSQL institutional package catalog, deterministic institutional eligibility, cryptographically verified digest-bound supply-chain attestations under deployment-pinned trust, per-package RBAC/ABAC re-authorization, and immutable PostgreSQL selection evidence. Only exact, approved, current, sovereign, attested package metadata is released under complete deployment configuration. Package discovery, recommendation, transfer, installation, execution, publication, catalog or approval mutation, AI Planning and later stations, external provisioning, and production action remain outside this wave.

Change-control record: `docs/change-control/CR-007-OPERATIONALIZATION-WAVE-06.md`.

## Approved operationalization addendum — CR-008

Operationalization Wave 07 connects Governed AI Planning to the exact evidence-bearing Approved Packages snapshot and deterministic delivery-run snapshot stopped at `ApprovedPackages`, signed-bundle-bound exact AI Planning OPA scope, signed governed prompt and authorized context material, a provider-neutral sovereign HTTPS planning protocol, a separately configured and trusted independent evaluator, per-context and per-result RBAC/ABAC re-authorization, and immutable PostgreSQL planning evidence. Only non-executable, independently evaluated planning content can be released under complete deployment configuration. Provider or model selection, tools, generated files, workflow advancement, Code Generation and later stations, external provisioning, and production action remain outside this wave.

Change-control record: `docs/change-control/CR-008-OPERATIONALIZATION-WAVE-07.md`.
