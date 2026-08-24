# Phase 09 — Institutional Package Registry

Status: **Implemented**  
Depends on: **Phase 08 — Organization Knowledge Engine**

## Registry scope

The institutional registry governs exact immutable package coordinates across:

- NuGet packages;
- frontend dependencies;
- container images;
- AI models;
- policy bundles;
- sandbox images.

Every coordinate includes kind, name, version, and algorithm-qualified content digest. Approval of one version or digest never authorizes another.

## Institutional record

An `InstitutionalPackage` records provenance, publisher, license expression, tenant scope, environment scope, sovereign-registry availability, approval history, and optional SBOM and signature references. SBOM generation and signing are deliberately optional here because their enforcement is introduced in Phase 13.

## Use decision

The eligibility evaluator fails closed unless:

1. the requested coordinate exactly matches the registered version and digest;
2. tenant and environment are explicitly allowed;
3. an approved sovereign registry copy exists;
4. the latest decision is `Approved`;
5. the approval has not expired.

Rejected, pending, suspended, revoked, expired, mismatched, externally-only, or out-of-scope packages cannot enter a governed build.

## Boundaries

- The registry interface is asynchronous and storage-vendor neutral.
- No package is downloaded or executed by this phase.
- No external SaaS or public registry is mandatory.
- Provenance, SBOM generation, attestation, signing, and CI enforcement are completed in Phase 13.

