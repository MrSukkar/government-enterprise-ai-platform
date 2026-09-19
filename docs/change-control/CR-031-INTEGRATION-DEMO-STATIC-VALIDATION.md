# CR-031 — Integration Demo Stage 09: Governed Static Validation

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`

Preceding gate: `docs/demo/STAGE_08_CODE_GENERATION_ACCEPTANCE.md`

## Outcome

Demonstrate only governed deterministic Static Validation bound to the exact accepted Stage 08 inert Code Generation candidate and a deterministic delivery run stopped at `CodeGeneration`.

## Invariants

1. Signed OPA authorizes the exact candidate digest, generation evidence, two Static control identities, tenant, purpose, environment, classification, and `static-report` output before candidate read or control execution.
2. The authoritative candidate and delivery run are independently loaded and cryptographically rebound to the exact request.
3. Only deployment-configured, institutionally approved, signed deterministic in-process controls execute through `CodeValidationPipeline` at `ValidationGate.Static`.
4. Every required control must be unique, complete, passing, and evidence-bearing. Error or Critical findings fail closed.
5. The released report is independently result-authorized, append-only evidenced, non-executable, and non-advancing.
6. Repository defaults remain unconfigured and fail closed. Control profiles, trust material, keys, candidate content, logs, volumes, credentials, and institutional data remain outside Git.
7. No source mutation, compilation, command, process, package acquisition, external analyzer, network control, Security Validation, workflow advancement, production action, architectural change, or Phase 31 is introduced.

Approved by the repository owner on 2026-09-19 for bounded Stage 09 implementation, local verification, source control, GitHub synchronization, and CI only.
