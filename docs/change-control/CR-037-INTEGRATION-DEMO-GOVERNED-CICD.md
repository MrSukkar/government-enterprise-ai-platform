# CR-037 — Integration Demo Stage 15: Governed CI/CD

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 16, `docs/change-control/CR-016-OPERATIONALIZATION-WAVE-15.md`, `docs/change-control/CR-001-AMENDMENT-13-CICD.md`, and `docs/demo/INTEGRATION_DEMO_STAGE_ROADMAP.md`.

Preceding accepted gate: `docs/demo/STAGE_14_GOVERNED_GIT_ACCEPTANCE.md`.

## Decision

Connect only the Stage 15 localhost Integration Demo boundary for Governed CI/CD Execution. The exact signed Git receipt and `Git`-stopped run may reach the existing CI/CD boundary only after signed exact-action OPA authorization. A real signed institutional gateway must validate and execute only the immutable approved workflow in the approved isolated runner pool and return an exact non-released pipeline-output manifest.

## Approved scope

1. Add signed OPA permit/deny scope for `internal-service.cicd.execute` and `cicd-result` only.
2. Bind the exact repository, signed commit, tree and change-set digests, immutable workflow identity/version/digest/signature, pipeline profile, runner pool, ordered stages, controls, tenant, purpose, environment, classification, and evidence.
3. Reuse the existing prerequisite readers, immutable workflow validator, provider-neutral `IInstitutionalCiCdGateway`, result authorizer, signature/trust validation, and append-only PostgreSQL evidence.
4. Add migrations 023 and 024 to Stage 15 composition while keeping the `Git`-stopped run, workflow record, gateway endpoint, runner, credentials, signer, trust, private material, execution, and outputs outside Git.
5. Expose the exact CI/CD inputs without defaulting or fabricating an execution result.

No fake workflow, runner, execution, SBOM, provenance, attestation, signature, manifest, gateway response, or success; no source mutation, PR, merge, tag, Artifact publication, registry mutation, deployment, workflow advancement, production effect, architectural deviation, or Phase 31 is introduced. Repository defaults remain unconfigured and fail closed. Data is synthetic only.

## Acceptance authority

Stage 15 may be complete only after its independent acceptance artifact is satisfied, the complete project verifier passes, the immutable commit is synchronized through a pull request, and required CI is green. Stage 16 remains separately governed and unauthorized by this change.
