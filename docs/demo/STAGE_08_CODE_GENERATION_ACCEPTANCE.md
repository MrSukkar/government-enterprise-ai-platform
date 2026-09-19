# Integration Demo Stage 08 — Governed Code Generation Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-030-INTEGRATION-DEMO-CODE-GENERATION.md`

Preceding gate: `docs/demo/STAGE_07_AI_PLANNING_ACCEPTANCE.md`

## Verification record

| Check | Result |
|---|---|
| Exact Stage 07 and Stage 06 prerequisites | Pass; planning and selection identifiers, digests, delivery run, and five evidence references are bound |
| Signed exact-action OPA scope | Pass; the Stage 08 bundle compiles and its exact Code Generation permit and denied-classification paths both evaluate correctly |
| Governed prompt and context | Pass; the deployment-signed prompt seed is mandatory and every context reference is re-authorized before disclosure |
| Sovereign generation and independent evaluation | Pass; distinct endpoints, profiles, operator identities, and non-overlapping trust are configuration requirements |
| Authorized output boundary | Pass; only two normalized repository-relative paths are policy-authorized candidate data |
| Result boundary | Pass; the candidate is result-authorized, append-only evidenced, non-executable, unapplied, and non-advancing |
| Fail-closed behavior | Pass; missing or mismatched prerequisite, policy, seed, trust, runtime, evaluation, authorization, or evidence prevents release |
| Deployment composition | Pass; Compose configuration and pinned OPA bundle validation succeeded |
| Solution build | Pass — all 15 projects, 0 warnings, 0 errors |
| Project verification | Pass — `scripts/verify-project.ps1` completed successfully |

## Boundary assertions

- Repository defaults remain unconfigured; signed seeds, trust, endpoints, credentials, keys, certificates, responses, generated content, logs, and volumes remain outside Git.
- The UI exposes only a Code Generation call and receipt over the already accepted prerequisites. It does not write or apply generated files.
- `IsExecutable`, `IsApplied`, and `CanAdvance` remain `false`.
- No tool, command, package installation, Static Validation call, source mutation, workflow advancement, institutional mutation, or production effect is available.

Static Validation and all later stages remain outside this gate.
