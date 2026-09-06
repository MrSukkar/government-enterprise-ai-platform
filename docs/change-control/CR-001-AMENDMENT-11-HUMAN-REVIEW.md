# CR-001 Amendment 11 — Governed Human Review Boundary

Status: **Approved for Operational Increment 14**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-10-TESTS.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next approved vertical-slice station:

`Tests -> Human Review`

Approved increment name: **Operational Increment 14 — Governed Human Review**.

This amendment does not create Phase 31.

### Product outcome

An authorized human reviewer can approve or reject an exact, evidence-bearing code candidate only after accepted governed Tests. Verified OPA policy authorizes the reviewer, exact candidate/validation/Sandbox/Tests digests, delivery-run identity, tenant, purpose, environment, classification, review action, evidence scope, and separation-of-duties constraints before prerequisite read or candidate disclosure. A deployment-controlled run must be stopped exactly at `Tests`. The reviewer must differ from the initiating developer, provide an explicit decision and rationale, and present a deployment-verifiable, non-repudiable human attestation bound to the entire review package. AI, runtime, API, or policy cannot supply or alter the human decision. The decision and cryptographic evidence are recorded atomically with deterministic idempotency and optimistic version protection. The receipt cannot advance to Git or any later station.

## 2. Impact Analysis

### In scope

- A protected REST contract for governed Human Review after accepted Tests.
- Governed reviewer identity, tenant, purpose, classification clearance, explicit review permission, authorization evidence, review identity, expected version, explicit approve/reject decision, rationale, and human-attestation reference.
- Exact Tests, Sandbox, Security Validation, and Code Generation identities, digests, and evidence references.
- A signed, verified, environment-aware OPA decision authorizing the exact reviewer and review package before prerequisite read or candidate disclosure.
- Deployment-controlled readers for the authoritative Tests receipt, Sandbox receipt, Security receipt, code candidate, and deterministic delivery run.
- Structural proof that all prior receipts are permit-backed, accepted, digest-matching, evidence-bearing, production-effect-free, and non-advancing.
- A tenant-matching, complete, ordered, evidence-bearing delivery run stopped exactly at `DeliveryStage.Tests`.
- Strict separation of duties: the reviewer cannot be the run initiator, candidate requester, or another policy-declared conflicting actor.
- A deployment-controlled human-attestation verifier binding reviewer, decision, rationale, complete digest chain, purpose, tenant, environment, classification, and time.
- An append-only atomic review repository that records the human decision and cryptographic evidence together with deterministic idempotency and optimistic concurrency.
- Structural verification of persisted reviewer, decision, rationale, digest chain, policy, attestation, evidence, version, and time.
- Explicit `IsApproved`, `ProductionEffectOccurred: false`, `CanAdvance: false`, and no workflow stage completion; Git remains separately governed.
- OpenAPI 3.1, Blazor boundary communication, runtime readiness, acceptance verification, and non-regression checks.
- Fail-closed behavior when policy, any prerequisite reader, attestation verification, atomic repository, or evidence dependency is unavailable.

### Out of scope

- Approval or implementation of Git, CI/CD, Artifact, Deployment, or any later station.
- Self-approval, delegated AI approval, service-account approval, anonymous approval, shared identity, or approval by the initiating developer.
- AI-generated or runtime-generated reviewer decisions, rationale, attestations, signatures, or identity evidence.
- Editing the candidate, test manifest, test result, validation report, Sandbox output, review package, or prior evidence during review.
- Production access/effect, credentials, endpoints, data mutation, network action, source write, commit, branch, pull request, CI/CD invocation, artifact publication, or deployment.
- Automatic workflow advancement, creation of a delivery completion, or treating approval as Git authorization.
- Overwriting, deleting, backdating, silently superseding, or mutating an existing review record; a new decision requires a new governed review identity.
- A concrete identity provider, signature provider, approval UI product, database provider, external API, SaaS workflow, or mandatory non-sovereign control plane.
- Fake reviewers, synthetic attestations, placeholder evidence, pre-approved results, or hard-coded approval.
- A new database, queue, package, .NET project, service boundary, microservice, conditional technology approval, or architectural deviation.
- Universal reviewer quorum, expiry, duration, or SLO before institutional policy and workload benchmarking.
- Changes to the fixed roadmap, Master Specification, or constitutional invariants outside this approved amendment.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: binds Human Review to the complete authorized Tests and delivery evidence chain.
- `Platform.SoftwareFactory/Delivery`: retains workflow authority and separation-of-duties invariant; Human Review cannot advance to Git.
- `Platform.Identity` and `Platform.Governance`: retain authenticated human identity, authorization, conflict scope, and signed OPA authority.
- `Platform.Api`: composes the protected boundary without acquiring policy, attestation, repository, workflow, or evidence authority.
- `Platform.Web`: communicates review readiness without claiming an established reviewer, completed approval, or Git authority.
- `Platform.Evidence`: remains cryptographic evidence authority through the atomic persistence boundary.
- OpenAPI and verification scripts prove authorization ordering, human provenance, separation of duties, atomicity, fail-closed dependencies, and non-advancement.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved OIDC/OAuth2 human identity configuration with institutional subject assurance.
- A sovereign signed-policy verifier and OPA adapter for Human Review.
- Deployment-controlled Tests, Sandbox, Security, code-candidate, and delivery-run readers.
- A sovereign human-attestation verifier backed by approved PKI/HSM or equivalent institutional trust.
- An append-only atomic review-and-evidence repository adapter with idempotency and optimistic concurrency.

Until all dependencies are configured, Human Review must return unavailable or denied without prerequisite/candidate disclosure, approval persistence, workflow advancement, source mutation, or external effect.

## 3. Architectural Review

Finding: **Conforms without architectural deviation**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- The fixed delivery sequence places Human Review after Tests and before Git.
- The human supplies the decision; OPA authorizes the action but does not replace the reviewer.
- Separation of duties is preserved and strengthened through exact actor-conflict checks and signed human attestation.
- The deterministic delivery engine remains the only workflow authority; Human Review creates no completion and cannot advance to Git.
- Atomic append-only persistence binds the decision to cryptographic evidence without adding a database technology choice.
- No production effect or direct AI-to-production path is introduced.
- Sovereign and air-gapped operation gains no mandatory external identity, approval, API, SaaS, telemetry, or control plane.
- No numerical SLO or unapproved quorum is invented.

## 4. Decision

Approve one additional CR-001 product increment limited to **Governed Human Review**.

This approval authorizes implementation, verification, source control, and governed GitHub synchronization for this increment only. It does not authorize Git operations on generated candidate content, workflow advancement, CI/CD, deployment, institutional mutation outside the review record, public deployment, or any later station.

Decision: **Approved by the repository owner for Operational Increment 14**.

## 5. Master Specification Update

Append the following paragraph to the CR-001 business implementation addendum:

> Operational Increment 14 is **Governed Human Review**. It defines a protected human approve/reject decision bound to accepted governed Tests, the complete Sandbox and validation evidence chain, an authoritative inert code candidate, and a deterministic delivery run stopped exactly at `Tests`. Verified OPA policy authorizes the exact human reviewer, review action, prerequisite digests, tenant, purpose, environment, classification, evidence scope, and separation-of-duties constraints before prerequisite read or candidate disclosure. The reviewer must differ from conflicting actors and provide explicit rationale plus a deployment-verifiable non-repudiable attestation bound to the complete review package. AI, policy, runtime, and API cannot supply or alter the human decision. The decision and cryptographic evidence are recorded atomically with deterministic idempotency and optimistic concurrency. No production effect or workflow advancement is available. The result cannot advance to Git or perform production action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 14 — Governed Human Review.
- Authorization: implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved on 2026-09-06**.

## Acceptance criteria

1. Human Review requires a governed human identity, explicit permission, exact prerequisite identities/digests/evidence, run, explicit decision/rationale, attestation, purpose, classification, environment, and authorization evidence.
2. Verified signed OPA authorizes the exact reviewer, action, package, and conflict scope before prerequisite read or candidate disclosure.
3. Denial, invalid signature, mismatch, missing scope, or unavailable policy cannot read prerequisites or persist a decision.
4. Authoritative Tests, Sandbox, Security, candidate, and run records are loaded only through deployment-controlled readers after permit and structurally revalidated.
5. Tests and all earlier prerequisites are accepted, digest-matching, evidence-bearing, production-effect-free, and non-advancing.
6. The run is tenant-matching, complete, ordered, evidence-bearing, and stopped exactly at `DeliveryStage.Tests`.
7. The reviewer is not the initiator or any policy-declared conflicting actor; service accounts and non-human identities are rejected.
8. The human explicitly supplies Approve or Reject and a non-empty rationale; AI and runtime cannot generate or change them.
9. A deployment-controlled verifier proves a non-repudiable human attestation over the exact complete review package.
10. Review identity, expected version, and package digest provide deterministic idempotency and optimistic concurrency.
11. Decision and cryptographic evidence are appended atomically; no partial decision or evidence write is possible.
12. Persisted decision, reviewer, rationale, policy, attestation, digests, evidence, version, and time are structurally revalidated.
13. Rejection remains evidenced but cannot be represented as approval; neither outcome advances workflow automatically.
14. The receipt records no production effect and `CanAdvance: false`; no workflow stage completion is created.
15. Missing dependencies fail closed without disclosure, persistence, mutation, advancement, or external effect.
16. No production access/effect, candidate edit, source write, Git, CI/CD, deployment, new package/project, or deviation is introduced.
17. OpenAPI 3.1 and Blazor expose the boundary without claiming reviewer, attestation, repository, Git, or production readiness.
18. All prior phase and Increment 01–13 gates remain satisfied.
19. All 15 projects build with zero warnings and zero errors.
20. Runtime verification proves the protected endpoint and all required deployment dependencies remain fail closed.
