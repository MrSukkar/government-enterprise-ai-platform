# CR-038 — Integration Demo Stage 16: Governed Artifact Publication

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`, Operational Increment 17, `docs/change-control/CR-017-OPERATIONALIZATION-WAVE-16.md`, `docs/change-control/CR-001-AMENDMENT-14-ARTIFACT.md`, and `docs/demo/INTEGRATION_DEMO_STAGE_ROADMAP.md`.

Preceding accepted gate: `docs/demo/STAGE_15_GOVERNED_CICD_ACCEPTANCE.md`.

## Decision

Connect only the Stage 16 localhost Integration Demo boundary for Governed Artifact Publication. Exact accepted CI/CD evidence and the `CiCd`-stopped run may reach the existing Artifact boundary only after signed exact-action OPA authorization. A real signed institutional gateway must publish exactly one immutable coordinate without overwrite and prove all six mandatory supply-chain controls.

## Approved scope

1. Add signed OPA permit/deny scope for `internal-service.artifact.publish` and `artifact-publication-result` only.
2. Bind the exact CI/CD manifest, source commit, workflow, coordinate, content digest, registry, repository, signing policy, controls, tenant, purpose, environment, classification, and evidence.
3. Reuse the existing readers, package validator, provider-neutral `IInstitutionalArtifactRegistryGateway`, `SupplyChainVerificationPipeline`, result authorizer, signature/trust validation, and append-only PostgreSQL evidence.
4. Add migrations 025 and 026 to Stage 16 composition while keeping the `CiCd`-stopped run, manifest, registry endpoint, credentials, signer, verifiers, trust, private material, artifact, and publication outside Git.
5. Expose exact publication inputs without defaulting or fabricating a result.

No fake artifact, registry record, signature, provenance, SBOM, attestation, verification, gateway response, or success; no overwrite, mutable tag, source mutation, deployment, workflow advancement, production effect, architectural deviation, or Phase 31 is introduced. Defaults remain unconfigured and fail closed. Data is synthetic only.

## Acceptance authority

Stage 16 may be complete only after its independent acceptance artifact is satisfied, the complete verifier passes, the immutable commit is synchronized through a pull request, and required CI is green. Stage 17 remains separately governed and unauthorized.
