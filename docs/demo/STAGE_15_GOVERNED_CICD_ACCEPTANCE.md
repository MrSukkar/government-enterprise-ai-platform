# Integration Demo Stage 15 — Governed CI/CD Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-037-INTEGRATION-DEMO-GOVERNED-CICD.md`

Preceding gate: `docs/demo/STAGE_14_GOVERNED_GIT_ACCEPTANCE.md`

## Verification record

All checks passed: exact signed Git evidence and `Git`-stopped delivery run; signed permit/deny OPA scope; immutable signed least-privilege workflow; exact isolated runner, ordered stages, controls, locked dependencies, and institutional endpoints; provider-neutral signed CI/CD gateway boundary; non-released SBOM/provenance/checksum/attestation/signature manifest; result authorization; fail-closed defaults; append-only PostgreSQL evidence; Compose; OPA bundle validation; and the 15-project build with 0 warnings and 0 errors.

No fake workflow, runner, execution, SBOM, provenance, attestation, signature, output manifest, gateway response, private key, credential, trust material, endpoint, institutional data, or fabricated success is included. Runtime material and the exact `Git`-stopped snapshot are deployment-provisioned outside Git. Missing or mismatched evidence, mutable workflow, unlocked dependency, unauthorized runner or endpoint, invalid signature/trust, or unavailable dependency fails closed.

The receipt records `CiCdTriggered: true`, `SourceMutationOccurred: false`, `ArtifactPublished: false`, `DeploymentOccurred: false`, `ProductionEffectOccurred: false`, and `CanAdvance: false`. Artifact publication and all later stages remain outside this gate.
