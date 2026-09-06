# CR-001 Amendment 10 — Governed Tests Boundary

Status: **Approved for Operational Increment 13**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-09-SANDBOX.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next approved vertical-slice station:

`Sandbox -> Tests`

Proposed increment name: **Operational Increment 13 — Governed Tests Execution**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can request deterministic tests for an authoritative code candidate only after an accepted, evidence-bearing Sandbox receipt. Verified OPA policy authorizes the exact candidate, Security-report and Sandbox-result digests, delivery-run identity, test manifest, test categories, institutional test image, isolation policy, non-secret environment references, network destinations, tenant, purpose, environment, classification, and evidence scope before prerequisite read, candidate disclosure, image access, or test invocation. A deployment-controlled delivery run must be stopped exactly at `Sandbox`. Tests run only through a vendor-neutral governed test-runtime boundary inside the approved Firecracker-class ephemeral isolation controls. Every required test must be discovered, executed, completed, uniquely identified, and evidence-bearing; skipped, missing, duplicated, failed, timed-out, or isolation-violating required tests fail closed. Result authorization and cryptographic evidence precede release. The receipt cannot advance to Human Review or any later station.

## 2. Impact Analysis

### In scope

- A protected REST contract for governed Tests execution after an accepted Sandbox receipt.
- Governed identity, tenant, purpose, classification clearance, explicit tests-execution permission, authorization evidence, exact prerequisite identities/digests/evidence, delivery-run identity, test manifest, required test identities/categories, image, isolation, environment references, and network scope.
- A signed, verified, environment-aware OPA decision over every exact test input before prerequisite read, candidate disclosure, image access, or invocation.
- Deployment-controlled readers for the authoritative Sandbox receipt, Security Validation receipt, immutable Code Generation candidate, and deterministic delivery run.
- Structural proof that Sandbox was permit-backed, accepted, zero-exit, free of timeout and isolation violation, production-effect-free, non-advancing, digest-matching, and evidence-bearing.
- Structural proof that the candidate and Security prerequisite are exact, permit-backed, accepted or released as applicable, inert outside the sandbox, non-advancing, and evidence-bearing.
- A tenant-matching, complete, ordered, evidence-bearing delivery run stopped exactly at `DeliveryStage.Sandbox`.
- An immutable governed test manifest containing unique test identities, categories, relative source references, required/optional designation, and an exact SHA-256 digest.
- Exact institutional `SandboxImage` selection with current package eligibility and complete sovereign supply-chain assurance before test execution.
- Reuse of `SandboxIsolationPolicy` requirements: Firecracker-class ephemeral microVM, no production credentials, no host filesystem, network default deny, policy-authorized destinations only, and configured positive CPU, memory, and time limits.
- A vendor-neutral `IGovernedTestRuntime` contract that executes only the exact authorized manifest inside the governed sandbox boundary; no concrete test framework, runner product, provider, or external control plane is selected.
- Test-result validation requiring exact manifest coverage, unique identities, completion, required-test pass, no unauthorized skip/substitution, no timeout or isolation violation, and non-placeholder evidence for every result.
- Per-result authorization before disclosure, covering all prerequisites, manifest, image assurance, isolation, environment/network scope, test outcomes, evidence, tenant, purpose, environment, and classification.
- A deterministic SHA-256 Tests-result digest and cryptographic evidence receipt bound to the complete prerequisite, policy, manifest, package assurance, isolation, invocation, result, authorization, and time chain.
- Explicit `IsAccepted`, `ProductionEffectOccurred: false`, `CanAdvance: false`, and no `StageCompletion`; Human Review remains separately governed.
- OpenAPI 3.1, Blazor boundary communication, runtime readiness, acceptance verification, and non-regression checks.
- Fail-closed behavior when any policy, prerequisite reader, manifest reader, registry/eligibility/assurance, test runtime, result authorization, or evidence dependency is unavailable.

### Out of scope

- Approval or implementation of Human Review, Git, CI/CD, Artifact, Deployment, or any later station.
- Production execution, credentials, endpoints, data mutation, network destinations, host access, or any direct AI-to-production path.
- Caller-authored or dynamically acquired tests, packages, test adapters, scripts, commands, plugins, images, frameworks, or dependencies outside the authorized manifest and institutional registry.
- Test discovery that expands the authorized manifest, floating image tags, mutable coordinates, package substitution, or downloads from unapproved sources.
- Secrets, tokens, passwords, private keys, raw credentials, production connection strings, or secret material in requests, test inputs, fixtures, or environment maps.
- Source-tree writes, applying generated code, snapshots or golden-file updates, durable workspace mutation, commits, branches, pull requests, CI/CD invocation, artifact publication, or deployment.
- Load, performance, penetration, destructive, production-data, or external-integration tests unless separately risk-approved and explicitly represented by future Change Control.
- Automatic creation, persistence, advancement, completion, retry, resume, or mutation of the Software Delivery Run or durable workflow.
- Allowing AI, test code, runtime, API, UI, or caller to choose policy, image approval, network scope, credentials, required-test disposition, next stage, or workflow authority.
- Treating test success as human approval, source approval, artifact approval, deployment approval, or production readiness.
- A concrete xUnit/NUnit/MSTest/Playwright runner adapter, microVM provider, container engine, cloud service, external API, telemetry service, or mandatory non-sovereign control plane.
- Fake test execution, fabricated pass results, placeholder evidence, or hard-coded successful outcomes.
- A new database, queue, package, .NET project, service boundary, microservice, conditional technology approval, or architectural deviation.
- Universal coverage percentage, test count, duration, performance threshold, or SLO before workload benchmarking and institutional risk configuration.
- Changes to the fixed roadmap, Master Specification, or constitutional invariants before Change Control approval.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: binds Tests to the complete authorized Sandbox and validation evidence chain.
- `Platform.SoftwareFactory/Sandbox`: retains mandatory isolation and vendor-neutral runtime constraints.
- `Platform.SoftwareFactory/Delivery`: remains workflow authority; the run must already be stopped at `Sandbox`, and Tests cannot advance it.
- `Platform.SoftwareFactory/Packages` and `SupplyChain`: retain institutional image approval and assurance authority.
- `Platform.Identity` and `Platform.Governance`: retain governed identity and signed OPA authority.
- `Platform.Api`: composes the protected boundary without acquiring policy, manifest, package, runtime, result-authorization, workflow, or evidence authority.
- `Platform.Web`: communicates Tests readiness without claiming runner/image connectivity, Human Review approval, or production effects.
- `Platform.Evidence`: remains cryptographic evidence authority.
- OpenAPI and verification scripts prove authorization ordering, exact test coverage, isolation invariants, fail-closed dependencies, and non-advancement.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity-provider configuration.
- A sovereign signed-policy verifier and OPA adapter for Tests execution.
- Deployment-controlled Sandbox, Security Validation, code-candidate, delivery-run, and governed test-manifest readers.
- An institutional exact package-registry reader, image eligibility evaluator, and supply-chain assurance verifier.
- A sovereign or air-gapped `IGovernedTestRuntime` operating inside approved Firecracker-class ephemeral microVM isolation.
- A policy-authorized Tests-result authorizer.
- A sovereign cryptographic evidence implementation when authoritative persistence is required.

Until all dependencies are configured, Tests must return unavailable or denied without prerequisite/candidate disclosure, image transfer, test invocation, code execution, workflow advancement, institutional mutation, or external effect.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to approval and implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- The fixed delivery sequence already places Tests immediately after Sandbox and before Human Review.
- OPA authorizes every exact test input before reads, image access, or invocation; test code and its runtime are not policy or workflow authority.
- Firecracker-class ephemeral isolation, no production credentials, no host filesystem, network default deny, and configured resource limits remain mandatory.
- Institutional package and supply-chain boundaries retain authority over the exact test image.
- The deterministic delivery engine remains the only workflow authority; Tests cannot create a completion or advance to Human Review.
- Test execution produces no production effect and cannot establish human approval or a direct AI-to-production path.
- Evidence remains classified, tenant-scoped, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable.
- Sovereign and air-gapped operation gains no mandatory external runner, image registry, API, SaaS, telemetry, licensing, or control plane.
- No numerical SLO, coverage threshold, or unbenchmarked test limit is invented.

## 4. Decision

Propose one additional CR-001 product increment limited to **Governed Tests Execution**.

Approval would authorize implementation, verification, source control, and governed GitHub synchronization for this increment only. It would not authorize production credentials/effects, unrestricted networking, host access, Human Review, workflow advancement, Git, CI/CD, deployment, institutional mutation, public deployment, or any later station.

Decision: **Approved by the repository owner for Operational Increment 13**.

## 5. Master Specification Update

Upon approval, append the following paragraph to the CR-001 business implementation addendum:

> Operational Increment 13 is **Governed Tests Execution**. It defines protected deterministic tests bound to an accepted Sandbox receipt, the complete Security Validation evidence chain, an authoritative inert code candidate, and a deterministic delivery run stopped exactly at `Sandbox`. Verified OPA policy authorizes the exact prerequisite digests, governed test manifest, institutional image, isolation policy, non-secret environment references, network destinations, tenant, purpose, environment, classification, and evidence scope before reads, image access, or invocation. A vendor-neutral governed test runtime executes only the exact authorized manifest inside Firecracker-class ephemeral microVM isolation. Every required test must be uniquely discovered, completed, passed, and evidence-bearing; missing, duplicate, skipped, failed, timed-out, or isolation-violating results fail closed. Result authorization and cryptographic evidence are mandatory. No production effect or workflow advancement is available. The result cannot advance to Human Review or perform production action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Proposed scope:

- Product: Create Internal Service Workspace.
- Increment: 13 — Governed Tests Execution.
- Authorization requested: implementation, isolated deterministic test invocation subject to all configured controls, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved on 2026-09-06**.

## Acceptance criteria

1. Tests require governed identity, explicit permission, exact prerequisite identities/digests/evidence, run, manifest, image, isolation, environment/network scope, purpose, classification, and authorization evidence.
2. Verified signed OPA policy authorizes every exact input before prerequisite read, candidate disclosure, image access, or test invocation.
3. Denial, invalid signature, mismatch, missing scope, or unavailable policy cannot read prerequisites, access an image, or invoke tests.
4. Authoritative Sandbox, Security, candidate, and run records are loaded only through deployment-controlled readers after permit and structurally revalidated.
5. Sandbox is accepted, digest-matching, evidence-bearing, zero-exit, free of timeout/isolation violation, production-effect-free, and non-advancing.
6. The run is tenant-matching, complete, ordered, evidence-bearing, and stopped exactly at `DeliveryStage.Sandbox`.
7. The governed test manifest has an exact digest and unique, complete, policy-authorized test identities/categories and relative references.
8. The image is an exact immutable institutional `SandboxImage` coordinate with current approval and complete sovereign supply-chain assurance.
9. Tests run only inside Firecracker-class ephemeral microVM isolation with no production credentials, no host filesystem, network default deny, authorized destinations only, and configured positive limits.
10. Only the exact authorized manifest is invoked through `IGovernedTestRuntime`; no dynamic test, tool, package, command, or dependency acquisition is possible.
11. Every required test is present, unique, completed, passed, and evidence-bearing; missing, duplicate, skipped, failed, timed-out, or isolation-violating results fail closed.
12. The complete Tests result is re-authorized before disclosure.
13. A deterministic digest and cryptographic evidence bind prerequisites, policy, manifest, image assurance, isolation, invocation, results, authorization, and time.
14. The receipt records no production effect and `CanAdvance: false`; no stage completion or workflow mutation is created.
15. Missing dependencies fail closed without disclosure, transfer, execution, mutation, or external effect.
16. No production access/effect, source write, Human Review, Git, CI/CD, deployment, new package/project, or deviation is introduced.
17. OpenAPI 3.1 and Blazor expose the boundary without claiming test/image/runtime readiness, Human Review approval, or production effects.
18. All prior phase and Increment 01–12 gates remain satisfied.
19. All 15 projects build with zero warnings and zero errors.
20. Runtime verification proves the protected endpoint and all required deployment dependencies remain fail closed.
