# Phase 24 — Sovereign Government Productization

## Purpose

Phase 24 defines a verifiable government distribution package for the platform. It combines jurisdiction constraints, sovereign deployment topology, signed release artifacts, compliance mappings, and offline-operability guarantees without introducing a mandatory external service.

## Jurisdiction and compliance

A jurisdiction profile specifies data residency, supported languages, maximum classification, permitted deployment topologies, identity and policy authorities, trust bundle, and required compliance controls. Each required control must have exactly one evidenced mapping as implemented, inherited, or justified not applicable.

Publication requires `government.product.publish`, separation of duties between publisher and approver, and approval evidence. The sovereign deployment profile must belong to the tenant and use a topology permitted by the jurisdiction. Existing Phase 14 air-gap validation continues to require local dependencies, outbound default deny, and no external API, AI, SaaS, or control plane.

## Verifiable product manifest

The product manifest contains immutable product/version identity, SHA-256 digest, signature reference, release time, signed deployment artifacts, SBOM, build attestation, artifact signatures, supply-chain verification evidence, compliance evidence, and release evidence.

Government packages structurally require offline installation and prohibit mandatory external license checks and external telemetry. Artifact names are unique. `IProductManifestVerifier` validates the manifest against the jurisdiction trust bundle and returned verification is revalidated fail-closed.

## Atomic registration

After validation, an evidence-complete package record is registered atomically through `IGovernmentProductRegistry`. Returned state is checked for product, tenant, jurisdiction, topology, manifest digest, authorities, controls, evidence, and time so the registry cannot silently change the approved package.

## Phase boundary

This phase prepares distributable sovereign product metadata and contracts. It does not implement the unified user entry experience (Phase 25), developer tooling (Phase 26), or public deployment.
