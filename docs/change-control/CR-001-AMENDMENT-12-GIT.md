# CR-001 Amendment 12 — Governed Git Boundary

Status: **Approved for Operational Increment 15**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-11-HUMAN-REVIEW.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next approved vertical-slice station:

`Human Review -> Git`

Proposed increment name: **Operational Increment 15 — Governed Git Source Commit**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can materialize an exact approved code candidate as one signed, immutable Git commit only after a recorded approving Human Review. Verified OPA policy authorizes the exact candidate and review-package digests, delivery-run identity, repository identity, base commit, change branch, paths, commit metadata, tenant, purpose, environment, classification, and evidence scope before repository read or source mutation. A deployment-controlled run must be stopped exactly at `HumanReview`. A vendor-neutral institutional Git gateway verifies the exact clean base, applies only the authorized normalized change set, rejects secrets and prohibited paths, creates one signed commit on an authorized non-protected change branch, and proves that no force update, protected-branch mutation, CI/CD invocation, pull request, merge, artifact publication, deployment, or production effect occurred. Result authorization and cryptographic evidence precede commit receipt release. The receipt cannot advance to CI/CD or any later station.

## 2. Impact Analysis

### In scope

- A protected REST contract for governed Git source commit after an approving Human Review receipt.
- Governed identity, tenant, purpose, classification clearance, explicit Git-write permission, authorization evidence, exact prerequisite identities/digests/evidence, delivery-run identity, repository identity, expected base commit, requested change branch, and commit metadata.
- A signed, verified, environment-aware OPA decision authorizing every exact Git input before repository read, candidate disclosure to the Git gateway, or source mutation.
- Deployment-controlled readers for the authoritative Human Review receipt, Tests receipt, Code Generation candidate, and deterministic delivery run.
- Structural proof that Human Review is recorded, approved, independent, attested, evidence-bearing, production-effect-free, and non-advancing.
- Structural proof that Tests and the candidate are exact, accepted/released, digest-matching, evidence-bearing, and non-advancing.
- A tenant-matching, complete, ordered, evidence-bearing delivery run stopped exactly at `DeliveryStage.HumanReview`.
- An immutable governed change set derived from the authoritative candidate with unique normalized repository-relative paths, exact per-file SHA-256 digests, declared create/update/delete intent, and a deterministic aggregate digest.
- A deployment-controlled change-policy validator that rejects secrets, credentials, prohibited paths, unauthorized deletions, binary substitution, generated artifacts, dependency drift, policy/configuration tampering, and any path not authorized by OPA and Human Review.
- A vendor-neutral institutional Git gateway requiring an exact clean repository snapshot and expected base commit before mutation.
- Creation of one signed commit on one authorized non-protected change branch with controlled author/committer identity and exact commit-message metadata.
- Explicit proof of commit signature, parent commit, tree digest, exact changed paths/digests, branch/ref, repository identity, and absence of force update or unrelated changes.
- Result authorization after commit creation and before receipt disclosure, covering all prerequisites, target, change set, commit proof, tenant, purpose, environment, classification, and evidence.
- Cryptographic evidence binding request, prerequisites, OPA, change policy, repository precondition, materialization, commit signature, result authorization, and time.
- Explicit `SourceMutationOccurred: true`, `ProductionEffectOccurred: false`, `CiCdTriggered: false`, `CanAdvance: false`, and no workflow stage completion; CI/CD remains separately governed.
- OpenAPI 3.1, Blazor boundary communication, runtime readiness, acceptance verification, and non-regression checks.
- Fail-closed behavior when policy, any prerequisite reader, change-set materializer/validator, Git gateway, signer, result authorization, or evidence dependency is unavailable.

### Out of scope

- Approval or implementation of CI/CD, Artifact, Deployment, or any later station.
- Direct write to `main`, another protected branch, release tag, production branch, or policy-prohibited ref.
- Force push/update, history rewrite, amend, rebase, reset, deletion of existing refs, unsigned commit, merge commit, tag creation, or bypass of branch protection.
- Pull request creation, review dismissal, merge, CI/CD trigger, workflow dispatch, runner execution, artifact creation/publication, registry mutation, or deployment.
- Source mutation before OPA permit, approving Human Review, exact clean-base verification, change-policy approval, and all deployment dependencies are available.
- Caller-supplied arbitrary filesystem path, shell command, Git argument, hook, executable, credential, remote URL, signing key, or environment variable.
- AI-generated repository choice, branch protection decision, signing decision, commit authorization, Git command, or workflow authority.
- Writing secrets, tokens, credentials, private keys, local IDE state, `bin`, `obj`, temporary packages, or unapproved generated assets.
- Applying a partial or substituted candidate, adding unrelated files, changing unreviewed content, or silently resolving conflicts.
- Automatic workflow advancement or treating a commit as CI/CD, artifact, deployment, or production approval.
- A concrete Git hosting provider, CLI process, libgit implementation, signing product, credential provider, external API, SaaS, or mandatory non-sovereign control plane.
- Fake commit identifiers, synthetic signatures, placeholder evidence, or hard-coded success.
- A new database, queue, package, .NET project, service boundary, microservice, conditional technology approval, or architectural deviation.
- Universal branch naming, commit size, file count, duration, or SLO before institutional policy and workload benchmarking.
- Changes to the fixed roadmap, Master Specification, or constitutional invariants before Change Control approval.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: binds Git mutation to the complete approved Human Review and Tests evidence chain.
- `Platform.SoftwareFactory/Delivery`: remains workflow authority; the run must already be stopped at `HumanReview`, and Git cannot advance it.
- `Platform.Identity` and `Platform.Governance`: retain governed identity and signed OPA authority over the exact material action.
- `Platform.SoftwareFactory/DeveloperExperience`: retains normalized workspace concepts but gains no mutation authority.
- `Platform.Api`: composes the protected boundary without acquiring policy, filesystem, repository, signing, result-authorization, workflow, or evidence authority.
- `Platform.Web`: communicates Git readiness without claiming repository connectivity, commit creation, CI/CD, or production effects.
- `Platform.Evidence`: remains cryptographic evidence authority.
- OpenAPI and verification scripts prove authorization ordering, exact change-set binding, repository preconditions, signed immutable commit proof, fail-closed dependencies, and non-advancement.

### Deployment prerequisites not supplied by this amendment

- A deployment-approved governed identity-provider configuration and workload identity for institutional Git.
- A sovereign signed-policy verifier and OPA adapter for Git source mutation.
- Deployment-controlled Human Review, Tests, candidate, and delivery-run readers.
- A governed change-set materializer and institutional source-change policy validator.
- An institutional Git repository gateway with protected-branch policy, exact-base concurrency, commit signing, and no mandatory external control plane.
- A policy-authorized Git-result authorizer.
- A sovereign cryptographic evidence implementation when authoritative persistence is required.

Until all dependencies are configured, Git must return unavailable or denied without repository read, candidate disclosure to a mutation gateway, filesystem write, ref update, commit, CI/CD invocation, workflow advancement, or external effect.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to approval and implementation verification**.

- The solution remains a 15-project .NET 10 Modular Monolith with Blazor WebAssembly and REST/OpenAPI 3.1.
- Git is the approved source authority, and the fixed sequence places it after Human Review and before CI/CD.
- OPA authorizes the exact material action; AI, API, Git gateway, and repository are not policy or workflow authority.
- Separation of duties is inherited from the exact approving Human Review receipt.
- Exact-base optimistic concurrency, normalized paths, deterministic digests, protected-branch prohibition, and signed commit proof preserve source integrity.
- The deterministic delivery engine remains the only workflow authority; Git cannot create a completion or advance to CI/CD.
- Source mutation is explicit and bounded, while production effect and direct AI-to-production remain prohibited.
- Evidence remains append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable.
- Sovereign and air-gapped operation gains no mandatory external Git host, API, SaaS, signing, licensing, or control plane.
- No numerical SLO or unbenchmarked Git limit is invented.

## 4. Decision

Propose one additional CR-001 product increment limited to **Governed Git Source Commit**.

Approval would authorize the bounded source mutation, implementation, verification, source control, and governed GitHub synchronization for this increment only. It would not authorize protected-branch mutation, force update, pull request, merge, CI/CD invocation, artifact publication, deployment, production effect, workflow advancement, or any later station.

Decision: **Approved by the repository owner for Operational Increment 15**.

## 5. Master Specification Update

Upon approval, append the following paragraph to the CR-001 business implementation addendum:

> Operational Increment 15 is **Governed Git Source Commit**. It defines a protected source mutation bound to an approving Human Review receipt, accepted Tests, the authoritative code candidate, and a deterministic delivery run stopped exactly at `HumanReview`. Verified OPA policy authorizes the exact candidate/review digests, immutable change set, repository identity, base commit, non-protected change branch, commit metadata, tenant, purpose, environment, classification, and evidence scope before repository read or mutation. A vendor-neutral institutional Git gateway verifies the exact clean base, applies only policy-approved normalized paths and digests, rejects secrets and unrelated content, and creates one signed immutable commit without force update, protected-branch mutation, CI/CD invocation, or production effect. Result authorization and cryptographic evidence are mandatory. No workflow advancement is available. The result cannot advance to CI/CD or perform production action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

Proposed scope:

- Product: Create Internal Service Workspace.
- Increment: 15 — Governed Git Source Commit.
- Authorization requested: bounded source materialization and signed commit creation subject to all configured controls, implementation, acceptance verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and separately configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved on 2026-09-06**.

## Acceptance criteria

1. Git requires governed identity, explicit permission, exact prerequisite identities/digests/evidence, run, repository, base commit, change branch, commit metadata, purpose, classification, environment, and authorization evidence.
2. Verified signed OPA authorizes every exact input before repository read, candidate disclosure to the gateway, or mutation.
3. Denial, invalid signature, mismatch, missing scope, or unavailable policy cannot read the repository or mutate source.
4. Authoritative Human Review, Tests, candidate, and run records are loaded only through deployment-controlled readers after permit and structurally revalidated.
5. Human Review is recorded, approved, independent, attested, digest-matching, evidence-bearing, production-effect-free, and non-advancing.
6. The run is tenant-matching, complete, ordered, evidence-bearing, and stopped exactly at `DeliveryStage.HumanReview`.
7. The change set exactly matches the authoritative candidate and Human Review, with unique normalized paths, per-file SHA-256 digests, operations, and deterministic aggregate digest.
8. Change policy rejects secrets, credentials, prohibited paths, unauthorized deletes, binary substitution, dependency drift, unreviewed policy/configuration changes, and unrelated content.
9. The Git gateway verifies exact repository identity, clean base, expected parent commit, authorized non-protected branch, signing policy, and concurrency before mutation.
10. Exactly one signed commit is created with the exact authorized tree, parent, branch, author/committer, message metadata, and no unrelated changes.
11. No protected ref, force update, history rewrite, tag, merge, pull request, CI/CD, artifact, deployment, or production effect occurs.
12. The complete commit result is re-authorized before disclosure.
13. Cryptographic evidence binds prerequisites, policy, change validation, repository precondition, commit proof, authorization, and time.
14. The receipt records source mutation but no production effect, no CI/CD trigger, and `CanAdvance: false`; no workflow completion is created.
15. Missing dependencies fail closed without repository access, disclosure, mutation, ref update, commit, advancement, or external effect.
16. No new package/project, concrete Git provider, mandatory external control plane, or architectural deviation is introduced.
17. OpenAPI 3.1 and Blazor expose the boundary without claiming repository, signer, CI/CD, or production readiness.
18. All prior phase and Increment 01–14 gates remain satisfied.
19. All 15 projects build with zero warnings and zero errors.
20. Runtime verification proves the protected endpoint and all required deployment dependencies remain fail closed.
