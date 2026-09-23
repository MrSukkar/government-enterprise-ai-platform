# Integration Demo Stage 16 — Governed Artifact Publication Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-038-INTEGRATION-DEMO-GOVERNED-ARTIFACT.md`

Preceding gate: `docs/demo/STAGE_15_GOVERNED_CICD_ACCEPTANCE.md`

## Verification record

All checks passed: exact accepted CI/CD evidence and `CiCd`-stopped run; signed permit/deny OPA scope; exact non-released manifest, immutable coordinate and content digest; no-overwrite provider-neutral registry gateway; all six unique supply-chain controls; result authorization; fail-closed defaults; append-only PostgreSQL evidence; Compose; OPA bundle validation; and the 15-project build with 0 warnings and 0 errors.

No fake artifact, registry publication, signature, provenance, SBOM, attestation, verification, gateway response, private key, credential, trust material, endpoint, institutional data, or fabricated success is included. Missing or mismatched evidence, partial output, digest substitution, overwrite, unsigned result, invalid trust, missing/duplicate control, or unavailable dependency fails closed.

The receipt records `ArtifactPublished: true`, `RegistryMutated: true`, `SourceMutationOccurred: false`, `DeploymentOccurred: false`, `ProductionEffectOccurred: false`, and `CanAdvance: false`. Deployment and all later stages remain outside this gate.
