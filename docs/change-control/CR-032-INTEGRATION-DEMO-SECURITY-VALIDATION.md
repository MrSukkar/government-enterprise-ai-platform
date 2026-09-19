# CR-032 — Integration Demo Stage 10: Governed Security Validation

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`

Preceding gate: `docs/demo/STAGE_09_STATIC_VALIDATION_ACCEPTANCE.md`

## Outcome

Demonstrate only governed deterministic Security Validation bound to the exact accepted Stage 09 Static report, its authoritative inert Stage 08 candidate, and a deterministic delivery run stopped at `StaticValidation`.

## Invariants

1. Signed OPA authorizes the exact candidate and Static report digests, Static evidence, two Security control identities, tenant, purpose, environment, classification, and `security-report` output before prerequisite reads or control execution.
2. The accepted Static receipt, authoritative candidate, and delivery run are independently loaded and cryptographically rebound to the exact request.
3. Only deployment-configured, institutionally approved, signed deterministic in-process controls execute through `CodeValidationPipeline` at `ValidationGate.Security`.
4. Every required control must be unique, complete, passing, and evidence-bearing. Error or Critical findings fail closed.
5. The released report is independently result-authorized, append-only evidenced, non-executable, and non-advancing.
6. Repository defaults remain unconfigured and fail closed. Control profiles, trust material, keys, candidate content, logs, volumes, credentials, and institutional data remain outside Git.
7. No source mutation, compilation, command, process, dynamic acquisition, external analyzer, network control, candidate or Sandbox execution, workflow advancement, production action, architectural change, or Phase 31 is introduced.

Approved by the repository owner on 2026-09-19 for bounded Stage 10 implementation, local verification, source control, GitHub synchronization, and CI only.
