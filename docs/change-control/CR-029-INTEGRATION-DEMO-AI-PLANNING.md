# CR-029 — Integration Demo Stage 07: Governed AI Planning

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`

Preceding gate: `docs/demo/STAGE_06_APPROVED_PACKAGES_ACCEPTANCE.md`

## Outcome

Demonstrate only a governed, non-executable AI Planning candidate bound to the exact evidence-bearing Stage 06 Approved Packages selection and a deterministic delivery run stopped at `ApprovedPackages`.

## Invariants

1. Signed OPA authorizes the exact prompt template, sovereign runtime profile, context references, approved package coordinates, constraints, tenant, purpose, environment, and classification before prompt/context release or AI invocation.
2. The provider-neutral planning runtime has no tools, filesystem, generated-file, package, Git, workflow, credential, deployment, or production capability.
3. Every context item is re-authorized before disclosure; generation and independent evaluation use distinct configured identities and pinned trust.
4. Only a fully evaluated, result-authorized, immutable evidence-bearing planning candidate may be released. `IsExecutable` and `CanAdvance` remain `false`.
5. Repository defaults remain unconfigured and fail closed. Runtime endpoints, credentials, prompts, trust material, responses, logs, and institutional data remain outside Git.
6. Code Generation, workflow advancement, institutional mutation, production action, new service boundaries, architectural change, and Phase 31 remain disconnected.

Approved by the repository owner on 2026-09-19 for bounded Stage 07 implementation, local verification, source control, GitHub synchronization, and CI only.
