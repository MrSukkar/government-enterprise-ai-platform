# CR-036 — Integration Demo Stage 14: Governed Git

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 15, `docs/change-control/CR-015-OPERATIONALIZATION-WAVE-14.md`, `docs/change-control/CR-001-AMENDMENT-12-GIT.md`, and `docs/demo/INTEGRATION_DEMO_STAGE_ROADMAP.md`.

Preceding accepted gate: `docs/demo/STAGE_13_GOVERNED_HUMAN_REVIEW_ACCEPTANCE.md`.

## Decision

Connect only the Stage 14 localhost Integration Demo boundary for Governed Git Source Commit. Exact approving Human Review, accepted Tests, authoritative inert candidate, and `HumanReview`-stopped run evidence may reach the existing Git boundary only after signed exact-action OPA authorization. A real signed institutional gateway must validate and create exactly one immutable commit from the exact clean base on the approved non-protected change branch.

## Approved scope

1. Add signed OPA permit/deny scope for `internal-service.git.commit` and `git-commit-result` only.
2. Bind exact candidate, review, Tests, and change-set digests; two normalized reviewed paths; repository identity; 40-character base commit; non-protected branch; commit metadata; and signing-policy reference.
3. Reuse the existing prerequisite readers, change-set materializer and validator, provider-neutral `IInstitutionalGitGateway`, result authorizer, signature/trust validation, and append-only PostgreSQL evidence.
4. Add migrations 021 and 022 to Stage 14 composition while keeping the `HumanReview`-stopped run, repository, gateway endpoint, credentials, signer, trust, private material, and commits outside Git.
5. Expose the exact Git inputs without defaulting or fabricating a commit result.

No fake repository, base, commit, signature, gateway response, or successful result; no protected-branch mutation, force update, history rewrite, pull request, CI/CD trigger, workflow advancement, artifact, deployment, production effect, architectural deviation, or Phase 31 is introduced. Repository defaults remain unconfigured and fail closed. Data is synthetic only.

## Acceptance authority

Stage 14 may be complete only after its independent acceptance artifact is satisfied, the complete project verifier passes, the immutable commit is synchronized through a pull request, and required CI is green. Stage 15 remains separately governed and unauthorized by this change.
