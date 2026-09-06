# CR-001 Amendment 13 — Governed CI/CD Execution

Status: **Approved for Operational Increment 16**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-12-GIT.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next approved vertical-slice station:

`Git -> CI/CD`

Proposed increment name: **Operational Increment 16 — Governed CI/CD Execution**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request one deterministic CI/CD execution for the exact signed Git commit produced by Operational Increment 15. Verified OPA policy authorizes the exact Git receipt, repository, commit, tree and change-set digests, immutable workflow definition and digest, pipeline profile, runner pool, required stages and controls, tenant, purpose, environment, classification, and evidence scope before source checkout or pipeline invocation. A deployment-controlled run must be stopped exactly at `Git`. A vendor-neutral institutional CI/CD gateway executes only the approved immutable workflow on an approved isolated runner, using locked dependencies and approved institutional registries, and returns verifiable stage, SBOM, provenance, attestation, signature, and evidence references. The execution may produce only a non-released pipeline-output candidate; it cannot publish or register an artifact, mutate source, merge, deploy, cause a production effect, or advance the workflow. Result authorization and cryptographic evidence precede receipt release.

## 2. Impact Analysis

### In scope

- A protected REST contract for governed CI/CD execution after a verified signed Git receipt.
- Governed identity, tenant, purpose, classification clearance, explicit CI/CD-execute permission, authorization evidence, and exact prerequisite identities, digests, and evidence.
- A signed, verified, environment-aware OPA decision authorizing every exact input before repository checkout, source disclosure to a runner, or pipeline invocation.
- Deployment-controlled readers for the authoritative Git receipt, approving Human Review chain, source commit proof, deterministic delivery run, immutable workflow definition, pipeline profile, and runner-pool policy.
- Structural proof that the Git receipt is signed, evidence-bearing, exact-base and exact-change-set bound, production-effect-free, CI/CD-not-yet-triggered, and non-advancing.
- A tenant-matching, complete, ordered, evidence-bearing delivery run stopped exactly at `DeliveryStage.Git`.
- An immutable workflow definition with deployment-verifiable identity, version, digest, signature, exact ordered stages, exact required controls, immutable task/action references, declared inputs and outputs, and least-privilege permissions.
- A deployment-controlled workflow validator rejecting mutable task references, dynamic workflow substitution, unapproved scripts or commands, undeclared downloads, secret material in inputs, unauthorized registries, production credentials, privileged runners, and policy/configuration tampering.
- A vendor-neutral institutional CI/CD gateway restricted to the exact authorized repository, signed commit, workflow digest, pipeline profile, isolated runner pool, and non-production environment.
- Approved sovereign or air-gapped runner operation with ephemeral isolation, no production credentials, isolated filesystem, network default deny except exact authorized institutional endpoints, and positive deployment-defined resource and time limits.
- Locked dependency restoration from approved institutional sources, Project OS verification, compilation, governed tests, publication into an ephemeral output boundary, CycloneDX SBOM generation, SHA-256 checksums, provenance, build attestation, output signing, and evidence capture as exact policy-required stages.
- Exact stage/control cardinality, ordering, completion, status, evidence, and output-digest validation; missing, duplicate, substituted, skipped, failed, timed-out, or unevidenced results fail closed.
- A deterministic pipeline-output manifest binding the source commit and tree, workflow definition, runner identity, dependency lock state, build outputs, SBOM, checksums, provenance, attestations, signatures, and evidence without publishing an Artifact-stage object.
- Result authorization after execution and before receipt disclosure, covering all prerequisites, policy, runner, stages, controls, output manifest, tenant, purpose, environment, classification, and evidence.
- Cryptographic evidence binding request, Git proof, OPA, workflow and runner validation, execution, results, result authorization, and time.
- Explicit `CiCdTriggered: true`, `SourceMutationOccurred: false`, `ArtifactPublished: false`, `DeploymentOccurred: false`, `ProductionEffectOccurred: false`, `CanAdvance: false`, and no workflow stage completion.
- OpenAPI 3.1, Blazor boundary communication, runtime readiness, acceptance verification, and non-regression checks.
- Fail-closed behavior when policy, any prerequisite reader, workflow reader/validator, runner policy, CI/CD gateway, result authorization, or evidence dependency is unavailable.

### Out of scope

- Approval or implementation of Artifact, Deployment, or any later station.
- Workflow advancement from Git to CI/CD or from CI/CD to Artifact.
- Pull-request creation, merge, protected-branch update, tag creation, release creation, source rewrite, force update, or any other Git mutation.
- Artifact publication, registry mutation, package publication, container push, release promotion, deployment, infrastructure mutation, registration, or production effect.
- Treating an ephemeral pipeline output, checksum, SBOM, provenance statement, attestation, or signature as a released Artifact-stage record.
- Checkout, source disclosure, task execution, dependency access, or runner allocation before exact OPA permit and all deployment dependencies are available.
- Caller-supplied arbitrary workflow, task/action reference, runner, filesystem path, shell command, environment variable, registry, network destination, credential, secret, token, signing key, or output location.
- Dynamic dependency resolution, unlocked restoration, mutable third-party action tags, runtime task acquisition, or access to an unapproved public registry.
- AI-generated pipeline authority, workflow choice, runner choice, release decision, policy decision, or workflow advancement.
- A concrete CI/CD provider, hosted runner, GitHub-specific application dependency, external SaaS, mandatory public network, or mandatory non-sovereign control plane.
- Fake execution identifiers, synthetic attestations, placeholder signatures, fabricated SBOM/provenance, or hard-coded success.
- A new database, queue, package, .NET project, service boundary, microservice, conditional technology approval, or architectural deviation.
- Universal stage duration, resource size, concurrency, retry, retention, or SLO before institutional policy and workload benchmarking.
- Changes to the fixed roadmap, Master Specification, or constitutional invariants before Change Control approval.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: binds CI/CD to the complete signed Git and Human Review evidence chain.
- `Platform.SoftwareFactory/Delivery`: remains the sole workflow authority; the run must already be stopped at `Git`, and CI/CD cannot advance it.
- `Platform.SoftwareFactory/SupplyChain`: supplies the existing vendor-neutral provenance, SBOM, dependency-validation, build-attestation, signature, and verification vocabulary without granting artifact release authority.
- `Platform.Identity` and `Platform.Governance`: retain governed identity and signed OPA authority over the exact material execution.
- `Platform.Api`: composes the protected boundary without acquiring policy, workflow, runner, CI/CD, signing, artifact, or evidence authority.
- `Platform.Web`: communicates CI/CD readiness without claiming runner connectivity, artifact publication, deployment, or production effects.
- `Platform.Evidence`: remains cryptographic evidence authority.
- OpenAPI and verification scripts prove authorization ordering, exact commit/workflow/runner binding, locked supply-chain execution, fail-closed dependencies, and non-advancement.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity-provider configuration and CI/CD workload identity.
- A sovereign signed-policy verifier and OPA adapter for CI/CD execution.
- Deployment-controlled Git-receipt, Human Review chain, delivery-run, workflow-definition, pipeline-profile, and runner-policy readers.
- An institutional immutable workflow validator and approved task/action catalog.
- An isolated sovereign-compatible runner pool and vendor-neutral institutional CI/CD gateway.
- Approved institutional dependency sources, signing service or HSM adapter, provenance/attestation service, and temporary output store.
- A policy-authorized CI/CD-result authorizer.
- A sovereign cryptographic evidence implementation when authoritative persistence is required.

Until all dependencies are configured, CI/CD must return unavailable or denied without checkout, source disclosure, runner allocation, task execution, dependency access, output creation, source mutation, artifact publication, workflow advancement, deployment, or external effect.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to approval and implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- The fixed Software Factory sequence places CI/CD after Git and before Artifact.
- OPA authorizes the exact material execution; AI, API, workflow definition, runner, and CI/CD gateway are not policy or workflow authority.
- The deterministic delivery engine remains the only workflow authority; CI/CD cannot create a completion or advance to Artifact.
- Immutable source, workflow, task references, locked dependencies, isolated runners, exact stage/control validation, and cryptographic outputs preserve supply-chain integrity.
- Pipeline output remains explicitly distinct from a released Artifact-stage record.
- No source mutation, artifact publication, deployment, production effect, or direct AI-to-production path is introduced.
- Evidence remains append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable.
- Sovereign and air-gapped operation gains no mandatory external CI provider, hosted runner, public registry, SaaS, signing, licensing, or control plane.
- No numerical SLO or unbenchmarked runner limit is invented.

## 4. Decision

Propose one additional CR-001 product increment limited to **Governed CI/CD Execution**.

Approval would authorize implementation, local verification, source control, and governed GitHub synchronization for this increment only. It would not authorize workflow advancement, pull request or merge, artifact publication, registry mutation, deployment, production effect, or any later station.

Decision: **Approved by the repository owner for Operational Increment 16**.

## 5. Master Specification Update

Upon approval, append the following paragraph to the CR-001 business implementation addendum:

> Operational Increment 16 is **Governed CI/CD Execution**. It defines a protected deterministic pipeline execution bound to the exact signed Git receipt and a delivery run stopped exactly at `Git`. Verified OPA policy authorizes the exact repository, commit, tree and change-set digests, immutable workflow definition and digest, pipeline profile, isolated runner pool, required stages and controls, tenant, purpose, environment, classification, and evidence scope before checkout or invocation. A vendor-neutral institutional CI/CD gateway executes only the approved workflow using locked dependencies and approved institutional sources, producing an exact evidence-bearing pipeline-output manifest with SBOM, checksums, provenance, attestations, and signatures. Result authorization and cryptographic evidence are mandatory. No source mutation, Artifact-stage publication, workflow advancement, deployment, or production effect is available. The result cannot advance to Artifact or perform production action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Proposed scope:

- Product: Create Internal Service Workspace.
- Increment: 16 — Governed CI/CD Execution.
- Authorization requested: bounded CI/CD execution subject to all configured controls, implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved on 2026-09-06**.

## Acceptance criteria

1. CI/CD requires governed identity, explicit permission, exact Git/prerequisite identities, digests and evidence, run, immutable workflow, pipeline profile, runner pool, required stages/controls, purpose, classification, environment, and authorization evidence.
2. Verified signed OPA authorizes every exact input before checkout, source disclosure, runner allocation, dependency access, or pipeline invocation.
3. Denial, invalid signature, mismatch, missing scope, or unavailable policy cannot access source or start execution.
4. Authoritative Git, Human Review chain, run, workflow, pipeline-profile, and runner-policy records are loaded only through deployment-controlled readers after permit and structurally revalidated.
5. The Git receipt proves one exact signed commit, parent, tree and change set on an authorized non-protected branch, with no prior CI/CD trigger, production effect, or advancement.
6. The run is tenant-matching, complete, ordered, evidence-bearing, and stopped exactly at `DeliveryStage.Git`.
7. The workflow is immutable, signed, digest-matching, ordered, least-privilege, and contains only exact approved task/action references, inputs, outputs, stages, and controls.
8. Workflow validation rejects mutable references, dynamic substitution, unapproved commands/downloads/registries, secrets, production credentials, privileged execution, and policy tampering.
9. The CI/CD gateway binds execution to the exact repository, signed commit, workflow digest, pipeline profile, isolated runner pool, and non-production environment.
10. The runner is ephemeral and policy-constrained, with no production credentials or host access, default-deny networking, exact institutional endpoints, and positive deployment-defined resource/time limits.
11. Dependencies restore only in locked mode from authorized institutional sources, and lock-state drift or undeclared acquisition fails closed.
12. Every required stage and supply-chain control is unique, ordered, completed, successful, digest-bound, and evidence-bearing; missing, duplicate, substituted, skipped, failed, timed-out, or unevidenced results are rejected.
13. The output manifest exactly binds source, workflow, runner, dependency state, build outputs, SBOM, checksums, provenance, attestations, signatures, and evidence.
14. Pipeline outputs remain non-released and cannot be represented, published, or registered as an Artifact-stage object.
15. No source mutation, pull request, merge, tag, protected-ref update, artifact/registry publication, deployment, infrastructure mutation, or production effect occurs.
16. The complete CI/CD result is re-authorized before disclosure.
17. Cryptographic evidence binds prerequisites, policy, workflow/runner validation, execution, controls, outputs, authorization, and time.
18. The receipt records CI/CD invocation but no source mutation, artifact publication, deployment, production effect, or advancement; no workflow completion is created.
19. Missing dependencies fail closed without checkout, disclosure, runner allocation, invocation, output creation, mutation, publication, advancement, or external effect.
20. No new package/project, concrete CI provider, mandatory external control plane, conditional technology approval, or architectural deviation is introduced.
21. OpenAPI 3.1 and Blazor expose the boundary without claiming runner, signing, Artifact, deployment, or production readiness.
22. All prior phase and Increment 01–15 gates remain satisfied.
23. All 15 projects build with zero warnings and zero errors.
24. Runtime verification proves the protected endpoint and all required deployment dependencies remain fail closed.
