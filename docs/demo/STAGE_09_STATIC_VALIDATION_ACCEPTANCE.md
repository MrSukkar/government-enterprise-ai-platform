# Integration Demo Stage 09 — Governed Static Validation Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-031-INTEGRATION-DEMO-STATIC-VALIDATION.md`

Preceding gate: `docs/demo/STAGE_08_CODE_GENERATION_ACCEPTANCE.md`

## Verification record

| Check | Result |
|---|---|
| Exact accepted Stage 08 prerequisite | Pass; generation identity, candidate digest, generation evidence, and CodeGeneration-stopped delivery run are bound |
| Signed exact-action OPA scope | Pass; Stage 09 bundle compiles and exact permit and denied-classification paths both evaluate correctly |
| Signed deterministic controls | Pass; exactly `demo-static-contract-shape` and `demo-static-source-boundary` are required and deployment trust remains mandatory |
| Complete report boundary | Pass; every required control must complete, pass, and carry evidence; blocking findings fail closed |
| Result boundary | Pass; the report is result-authorized, append-only evidenced, non-executable, and non-advancing |
| Fail-closed behavior | Pass; missing or mismatched prerequisite, policy, control, trust, authorization, or evidence prevents release |
| Deployment composition | Pass; Compose configuration and pinned OPA bundle validation succeeded |
| Solution build | Pass — all 15 projects, 0 warnings, 0 errors |
| Project verification | Pass — `scripts/verify-project.ps1` completed successfully |

## Boundary assertions

- Repository defaults remain unconfigured; control profiles, signatures, trust, keys, candidate content, logs, credentials, and volumes remain outside Git.
- The UI exposes only the Static Validation call and receipt over the accepted inert candidate.
- `IsExecutable` and `CanAdvance` remain `false`.
- No source mutation, compilation, command, process, package acquisition, external analyzer, network control, Security Validation call, workflow advancement, institutional mutation, or production effect is available.

Security Validation and all later stages remain outside this gate.
