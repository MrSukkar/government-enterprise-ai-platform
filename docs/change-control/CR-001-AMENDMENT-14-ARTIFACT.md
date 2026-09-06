# CR-001 Amendment 14 — Governed Artifact Publication

Status: **Approved for Operational Increment 17**

Parent change record: `docs/change-control/CR-001-INTERNAL-SERVICE-VERTICAL-SLICE.md`

Preceding amendment: `docs/change-control/CR-001-AMENDMENT-13-CICD.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Extend CR-001 by one bounded product increment covering only the next approved vertical-slice station:

`CI/CD -> Artifact`

Increment name: **Operational Increment 17 — Governed Artifact Publication**.

This amendment does not create Phase 31.

### Product outcome

An authorized developer can publish one exact immutable artifact from the accepted, non-released pipeline-output manifest produced by Operational Increment 16. Verified OPA policy authorizes the exact CI/CD receipt, source commit, workflow and manifest digests, artifact coordinate and content digest, artifact kind, institutional registry, repository, signing policy, required supply-chain controls, tenant, purpose, environment, classification, and evidence scope before pipeline-output read or registry access. A deterministic delivery run must be stopped exactly at `CiCd`. The pipeline output is revalidated, the exact artifact is published once through a vendor-neutral institutional registry gateway, and every required source-provenance, SBOM, dependency-validation, build-attestation, artifact-signature, and registry-verification control must pass through the existing `SupplyChainVerificationPipeline`. Result authorization and cryptographic evidence precede receipt release. No deployment, production effect, or workflow advancement is available.

## 2. Impact Analysis

### In scope

- A protected REST contract for governed Artifact publication after an accepted CI/CD receipt.
- Governed identity, explicit Artifact-publish permission, tenant, purpose, environment, classification, authorization evidence, and exact prerequisite identities, digests, coordinate, registry, repository, signing policy, and controls.
- Signed and verified OPA authorization before CI/CD receipt read, pipeline-output read, artifact disclosure, or registry access.
- Deployment-controlled readers for the authoritative CI/CD receipt, pipeline-output manifest, and deterministic delivery run.
- Structural proof that CI/CD completed the exact signed commit and immutable workflow, produced the exact non-released manifest, and caused no source mutation, artifact publication, deployment, production effect, or advancement.
- A tenant-matching, ordered, evidence-bearing run stopped exactly at `DeliveryStage.CiCd`.
- A deployment-controlled artifact-package validator proving the requested content digest is exactly present in the pipeline-output manifest and that SBOM, checksums, provenance, build attestation, and signature references bind the same source and output.
- A vendor-neutral institutional artifact-registry gateway with immutable coordinate, digest, signer, registry, repository, idempotency, and no-overwrite proof.
- Reuse of the existing `SupplyChainVerificationPipeline`, requiring exactly one verifier for every `SupplyChainControl` and rejecting missing or duplicate verifiers.
- Exact source provenance, CycloneDX SBOM, dependency validation, build attestation, artifact signature, and registry verification before publication receipt release.
- Result authorization after registry publication and supply-chain verification.
- Cryptographic evidence binding request, prerequisites, OPA, package validation, immutable publication, supply-chain verification, result authorization, and time.
- Explicit `ArtifactPublished: true`, `RegistryMutated: true`, `SourceMutationOccurred: false`, `DeploymentOccurred: false`, `ProductionEffectOccurred: false`, `CanAdvance: false`, and no workflow completion.
- OpenAPI 3.1, Blazor boundary communication, readiness, acceptance, and non-regression verification.
- Fail-closed behavior when any policy, reader, validator, registry, verifier, authorizer, or evidence dependency is unavailable.

### Out of scope

- Approval or implementation of Deployment or any later station.
- Workflow advancement from CI/CD to Artifact or Artifact to Deployment.
- Source mutation, pull request, merge, tag, release branch, CI/CD rerun, or workflow substitution.
- Deployment, promotion to a deployment environment, infrastructure mutation, runtime activation, traffic, registration, telemetry ingestion, or production effect.
- Mutable tags, coordinate overwrite, digest substitution, partial output selection, unsigned artifact, unverified registry record, or bypass of any supply-chain control.
- Registry access or artifact disclosure before OPA permit and all deployment dependencies are available.
- Caller-supplied filesystem paths, commands, credentials, secrets, tokens, keys, arbitrary registry endpoints, or network destinations.
- A concrete registry vendor, external SaaS, mandatory public network, or mandatory non-sovereign control plane.
- AI-generated publication authority, artifact selection, signing decision, registry choice, release decision, or workflow advancement.
- Fake registry references, synthetic signatures, fabricated provenance/SBOM/attestations, placeholder evidence, or hard-coded success.
- New database, queue, package, .NET project, service boundary, microservice, conditional technology approval, architecture deviation, numerical SLO, or unbenchmarked limit.
- Changes to the fixed roadmap or constitutional invariants.

### Affected existing boundaries

- `Platform.SoftwareFactory/InternalService`: binds Artifact publication to the complete governed CI/CD and Git evidence chain.
- `Platform.SoftwareFactory/SupplyChain`: remains the vendor-neutral verification authority for all mandatory controls.
- `Platform.SoftwareFactory/Delivery`: remains workflow authority; Artifact cannot advance the run.
- `Platform.Identity` and `Platform.Governance`: retain identity and signed OPA authority.
- `Platform.Api`: composes the protected boundary without acquiring policy, registry, signing, workflow, or evidence authority.
- `Platform.Web`: communicates Artifact readiness without claiming registry or deployment connectivity.
- `Platform.Evidence`: remains cryptographic evidence authority.

### Deployment prerequisites not supplied by this amendment

- Deployment-approved identity and Artifact workload identity.
- Sovereign signed-policy verifier and OPA adapter for Artifact publication.
- Authorized CI/CD-receipt, pipeline-output-manifest, and delivery-run readers.
- Institutional artifact-package validator.
- Immutable sovereign-compatible artifact registry and signing gateway.
- Exactly one institutional verifier for every required supply-chain control.
- Artifact-result authorizer and sovereign evidence recorder.

Until all dependencies are configured, Artifact must return unavailable or denied without pipeline-output disclosure, registry access, publication, registry mutation, deployment, workflow advancement, production effect, or external action.

## 3. Architectural Review

Finding: **Conforms without architectural deviation, subject to implementation verification**.

- The 15-project .NET 10 Modular Monolith, Blazor WebAssembly, and REST/OpenAPI 3.1 baseline is unchanged.
- Artifact follows CI/CD and precedes Deployment in the fixed path.
- OPA remains policy authority; AI, API, registry, verifier, and artifact gateway have no workflow authority.
- Exact immutable coordinates and content digests, no-overwrite publication, signing, registry verification, and all mandatory supply-chain controls preserve integrity.
- The deterministic delivery engine remains the only workflow authority and receives no completion from this increment.
- Publication is bounded to the institutional registry; deployment and production remain prohibited.
- Evidence remains append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable.
- Sovereign and air-gapped operation gains no mandatory external dependency.
- No numerical SLO is invented.

## 4. Decision

Approve one additional CR-001 product increment limited to **Governed Artifact Publication**.

This approval authorizes bounded implementation, verification, source control, and governed GitHub synchronization for Increment 17 only. It does not authorize Deployment, production effect, workflow advancement, or any later station.

Decision: **Approved by the repository owner for Operational Increment 17**.

## 5. Master Specification Update

Append upon implementation verification:

> Operational Increment 17 is **Governed Artifact Publication**. It defines protected publication of one exact immutable artifact from an accepted, non-released CI/CD pipeline-output manifest and a deterministic delivery run stopped exactly at `CiCd`. Verified OPA policy authorizes the exact prerequisite, source, workflow, manifest, artifact coordinate and digest, institutional registry, signing policy, supply-chain controls, tenant, purpose, environment, classification, and evidence scope before reads or registry access. The exact package is validated, published without overwrite through a vendor-neutral institutional gateway, and must pass source provenance, SBOM, dependency validation, build attestation, artifact signature, and registry verification. Result authorization and cryptographic evidence are mandatory. No deployment, production effect, or workflow advancement is available. The result cannot advance to Deployment or perform production action.

The constitutional architecture and fixed 30-phase roadmap remain unchanged.

## 6. Approval

- Product: Create Internal Service Workspace.
- Increment: 17 — Governed Artifact Publication.
- Authorization: bounded Artifact publication, verification, source control, and governed GitHub synchronization for this increment only.
- Release authority: remains subject to successful verification and configured deployment prerequisites.

Approval state: **Approved**.

Repository-owner decision: **Approved on 2026-09-06**.

## Acceptance criteria

1. Exact governed identity, permission, prerequisites, digests, artifact coordinate, registry, signing policy, controls, purpose, classification, environment, and evidence are required.
2. Signed OPA authorization occurs before any prerequisite read, pipeline-output disclosure, or registry access.
3. CI/CD receipt and output manifest are authoritative, accepted, non-released, digest-matching, evidence-bearing, and non-advancing.
4. The run is complete, tenant-matching, ordered, evidence-bearing, and stopped exactly at `DeliveryStage.CiCd`.
5. Package validation binds the exact requested content to source, workflow, SBOM, checksums, provenance, attestation, and signature.
6. The institutional registry publishes one immutable coordinate and exact digest with signature, idempotency, and no-overwrite proof.
7. Every mandatory `SupplyChainControl` has exactly one verifier and passes with verifier identity and evidence.
8. Missing, duplicate, substituted, failed, or unevidenced supply-chain controls fail closed.
9. Artifact result authorization occurs after publication and verification and before disclosure.
10. Cryptographic evidence binds the complete request-to-result chain.
11. Receipt proves Artifact and registry mutation but no source mutation, deployment, production effect, or advancement.
12. No mutable reference, overwrite, partial output, unsigned content, unapproved registry, or conditional technology is introduced.
13. Missing dependencies fail closed without read, disclosure, registry access, publication, deployment, advancement, or external effect.
14. OpenAPI 3.1 and Blazor expose the boundary without claiming registry or deployment readiness.
15. All prior phase and Increment 01–16 gates remain satisfied.
16. All 15 projects build with zero warnings and zero errors.
17. Runtime verification proves the protected endpoint and all deployment dependencies remain fail closed.
