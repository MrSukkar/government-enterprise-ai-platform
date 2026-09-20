# CR-034 — Integration Demo Stage 12: Governed Tests

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 13, `docs/change-control/CR-013-OPERATIONALIZATION-WAVE-12.md`, and `docs/demo/INTEGRATION_DEMO_STAGE_ROADMAP.md`.

Preceding accepted gate: `docs/demo/STAGE_11_SECURITY_SANDBOX_ACCEPTANCE.md`.

## Decision

Connect only the Stage 12 localhost Integration Demo boundary for Governed Tests. Exact accepted Sandbox, Security Validation, and authoritative inert candidate evidence plus a `Sandbox`-stopped run may reach the existing Tests boundary only after signed exact-action OPA authorization, an immutable governed manifest, exact institutional image approval and supply-chain assurance, and complete deployment configuration.

## Approved scope

1. Add a signed OPA scope for `internal-service.tests.execute` and `tests-result` only.
2. Bind exactly two synthetic required tests, their categories, one immutable manifest digest, one exact synthetic test image, Firecracker-class ephemeral microVM isolation, no production credentials, no host filesystem, network default deny, an empty destination set, one non-secret reference, and positive Integration Demo resource/time limits.
3. Reuse the existing prerequisite readers, package controls, `IGovernedTestRuntime` provider-neutral sovereign HTTPS protocol, result authorizer, and immutable PostgreSQL evidence.
4. Expose the protected Tests endpoint in the Integration Demo UI only after an accepted Sandbox receipt.
5. Add migrations 017 and 018 to the Stage 12 localhost composition while keeping the manifest record, image record, runtime endpoint, trust, private material, credentials, and test results outside Git.
6. Require every exact required test to be unique, discovered, completed, passed, unskipped, and evidence-bearing. Missing, duplicate, skipped, failed, timed-out, isolation-violating, or unevidenced results fail closed.

No fake runtime, fabricated discovery, fabricated pass result, source mutation, workflow advancement, Human Review approval, external provisioning, production effect, architectural deviation, or Phase 31 is introduced. Repository defaults remain unconfigured and fail closed. Data is synthetic only.

## Acceptance authority

Stage 12 may be complete only after its independent acceptance artifact is satisfied, the complete project verifier passes, the immutable commit is synchronized through a pull request, and required CI is green. Stage 13 remains separately governed and unauthorized by this change.
