# CR-030 — Integration Demo Stage 08: Governed Code Generation

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`

Preceding gate: `docs/demo/STAGE_07_AI_PLANNING_ACCEPTANCE.md`

## Outcome

Demonstrate only a governed, non-executable and unapplied Code Generation candidate bound to the exact accepted Stage 07 AI Planning receipt, Stage 06 Approved Packages selection, and deterministic delivery run stopped at `AiPlanning`.

## Invariants

1. Signed OPA authorizes the exact planning and package digests, prompt template, sovereign runtime profile, five evidence references, approved package coordinates, constraints, safe repository-relative output paths, tenant, purpose, environment, and classification before prompt/context release or AI invocation.
2. Every prerequisite context item is digest verified and independently RBAC/ABAC re-authorized before disclosure.
3. Generation and independent evaluation use distinct deployment-controlled endpoints, profiles, operator identities, and non-overlapping pinned trust.
4. The candidate is data only. It has no filesystem, tool, command, package, Git, CI/CD, sandbox, deployment, workflow, credential, or production capability.
5. Only a fully evaluated, result-authorized, immutable evidence-bearing candidate may be released. `IsExecutable`, `IsApplied`, and `CanAdvance` remain `false`.
6. Repository defaults remain unconfigured and fail closed. Runtime endpoints, credentials, prompts, trust material, responses, logs, generated content, and institutional data remain outside Git.
7. Static Validation and later stages, source mutation, workflow advancement, production action, architectural change, and Phase 31 remain disconnected.

Approved by the repository owner on 2026-09-19 for bounded Stage 08 implementation, local verification, source control, GitHub synchronization, and CI only.
