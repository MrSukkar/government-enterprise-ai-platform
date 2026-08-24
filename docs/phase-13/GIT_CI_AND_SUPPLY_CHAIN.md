# Phase 13 — Git & CI Workflows

Status: **Implemented**  
Depends on: **Phase 12 — Code Validation & Security Sandbox**

## Governed CI

`.github/workflows/ci.yml` performs immutable checkout, SDK setup from `global.json`, locked dependency restore, Project OS verification, Release publication, CycloneDX SBOM generation, SHA-256 checksums, provenance recording, signed build/SBOM attestations, and evidence-artifact upload.

Every third-party GitHub Action is pinned to a full commit SHA. The workflow grants read-only repository contents by default and scopes `id-token` and `attestations` write permissions to the build job. Pull requests build and verify; trusted pushes to `main` additionally create signed attestations.

## Dependency reproducibility

All 15 .NET projects carry `packages.lock.json`. CI restores with `--locked-mode`, preventing an undeclared dependency change. `scripts/generate-sbom.ps1` emits CycloneDX 1.6 and includes source projects plus resolved NuGet dependencies.

## Verification chain

The runtime-neutral chain is:

`Source -> Provenance -> SBOM -> Dependency Validation -> Build -> Build Attestation -> Artifact Signature -> Registry Verification -> Deployment`

`SupplyChainVerificationPipeline` requires exactly one verifier for every control and rejects missing or duplicate controls. A verified record requires an algorithm-qualified artifact digest, source repository and commit, SBOM, build attestation, signature, registry reference, verifier identity, and evidence for every control.

GitHub Attestations is the connected repository implementation. Sovereign and air-gapped environments provide local PKI/HSM, attestation, registry, and verification adapters through the same interfaces; no application-domain dependency on GitHub or Sigstore is introduced.

