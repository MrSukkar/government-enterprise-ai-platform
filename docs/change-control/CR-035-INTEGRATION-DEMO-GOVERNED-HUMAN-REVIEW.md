# CR-035 — Integration Demo Stage 13: Governed Human Review

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 14, `docs/change-control/CR-014-OPERATIONALIZATION-WAVE-13.md`, `docs/change-control/CR-001-AMENDMENT-11-HUMAN-REVIEW.md`, and `docs/demo/INTEGRATION_DEMO_STAGE_ROADMAP.md`.

Preceding accepted gate: `docs/demo/STAGE_12_GOVERNED_TESTS_ACCEPTANCE.md`.

## Decision

Connect only the Stage 13 localhost Integration Demo boundary for Governed Human Review. An exact Tests-stopped delivery run and its accepted Tests, Sandbox, Security, and inert candidate evidence may reach the existing Human Review boundary only after signed exact-action OPA authorization by a distinct authenticated human reviewer and independently verified signed sovereign attestation.

## Approved scope

1. Add signed OPA permit/deny scope for `internal-service.human-review.decide` and `human-review-decision` only.
2. Bind the exact reviewer, initiator, explicit `Approve` or `Reject` decision, rationale digest, attestation reference, conflict set, HumanReviewer role, and optimistic version.
3. Add a distinct synthetic reviewer identity and stable synthetic subject identifiers; separation of duties remains mandatory and same-actor review fails closed.
4. Reuse the existing prerequisite readers, provider-neutral `IHumanReviewAttestationVerifier`, signature and trust validation, and atomic append-only PostgreSQL repository.
5. Add migrations 019 and 020 to Stage 13 composition while keeping the Tests-stopped run snapshot, attestation endpoint, trust, private material, credentials, and decisions outside Git.
6. Expose explicit decision, rationale, and attestation-reference inputs without defaulting or fabricating attestation success.

No fake reviewer, verifier, signature, attestation, or decision; no source mutation, Git operation, CI/CD trigger, workflow advancement, external provisioning, production effect, architectural deviation, or Phase 31 is introduced. Repository defaults remain unconfigured and fail closed. Data is synthetic only.

## Acceptance authority

Stage 13 may be complete only after its independent acceptance artifact is satisfied, the complete project verifier passes, the immutable commit is synchronized through a pull request, and required CI is green. Stage 14 remains separately governed and unauthorized by this change.
