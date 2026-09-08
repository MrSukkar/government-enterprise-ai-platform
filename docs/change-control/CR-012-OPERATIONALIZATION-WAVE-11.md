# CR-012 — Operationalization Wave 11: Governed Security Sandbox

Status: **Approved for Operationalization Wave 11**

Preceding completed authority: `docs/change-control/CR-011-OPERATIONALIZATION-WAVE-10.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## Change Request and decision

Operationalize only Governed Security Sandbox Execution by connecting Increment 12 to the immutable accepted Wave 10 Security Validation evidence, its exact authoritative Code Generation candidate, a delivery-run snapshot stopped at `SecurityValidation`, signed-bundle/OPA authorization, the existing institutional package registry and cryptographic supply-chain verification, a provider-neutral sovereign HTTPS sandbox protocol, per-result RBAC/ABAC authorization, and immutable PostgreSQL evidence.

Decision: **Approved by the repository owner on 2026-09-08 for bounded Wave 11 implementation, isolated runtime invocation only under complete deployment controls, local verification, source control, and GitHub synchronization.** The active instruction to adopt CR-012 and begin only Wave 11 supplies approval.

## Approved implementation boundary

- Verify the signed policy bundle and exact `internal-service.sandbox.execute` scope before any Security receipt, candidate, run, image, or runtime access.
- Read and revalidate the exact accepted Security receipt and authoritative inert Code Generation candidate through the immutable Security evidence chain, bound by tenant, purpose, identities, digests, and evidence.
- Read only a deployment-supplied run snapshot bound to the same tenant, purpose, identities, and digests and stopped exactly at `SecurityValidation`.
- Require OPA to authorize the exact immutable institutional Sandbox image, Firecracker-class isolation policy, non-secret environment references, network destinations, configured positive CPU/memory/time limits, required roles, classification, and `sandbox-result` output kind. Denial carries no scope and other actions reject Sandbox scope.
- Reuse the existing read-only PostgreSQL institutional package catalog, eligibility evaluator, and cryptographic supply-chain verifier. The exact image must be current, sovereign, digest-bound, provenance-bearing, SBOM-bearing, signed, and attested before runtime use.
- Invoke only the existing `GovernedSandboxService` and `ISecuritySandboxRuntime` through a provider-neutral sovereign HTTPS protocol configured by deployment. The signed response must bind the request digest, exact image and candidate, runtime profile/operator, isolation posture, allowed network destinations, exit state, produced references, and execution evidence.
- Accept only an attested Firecracker-class ephemeral microVM result with no production credentials, no host filesystem, enforced network default deny, exact allowed destinations, zero exit, no timeout, no isolation violation, unique non-placeholder produced references, and non-placeholder evidence.
- Re-authorize the complete result using authenticated identity, exact policy roles, prerequisites, image, isolation, environment/network scope, result digest, classification, purpose, environment, and evidence before release.
- Append immutable, idempotent, tenant-scoped PostgreSQL Sandbox evidence. Compose only when PostgreSQL, policy, Wave 10, institutional package trust, exact runtime endpoint/profile/operator, response-signing trust, and positive deployment bounds are complete.
- `IAuthorizedSandboxExecutionReceiptReader`, Tests, and later stations remain disconnected.

## Architecture and security review

This conforms without architectural deviation. It reuses .NET 10, PostgreSQL, OPA, RBAC/ABAC, built-in cryptography, the existing package and supply-chain boundaries, `GovernedSandboxService`, and `ISecuritySandboxRuntime`. The HTTPS protocol is provider-neutral and may terminate inside a sovereign or air-gapped deployment; it adds no mandatory external control plane. The runtime is neither policy nor workflow authority.

No provider, image, registry product, VM manager, credential, endpoint, institutional key, trust anchor, environment reference, network destination, CPU/memory/time value, request/response bound, retry, SLO, package, project, database, or service boundary is selected in repository defaults.

## Out of scope

Tests and every later station; production credentials, production data, production endpoint, host filesystem, unrestricted egress, inbound exposure, privileged or persistent execution; caller-selected unverified images; secrets or raw credentials; source mutation; command or process execution on the API host; package transfer by the platform; workflow advancement; Git, CI/CD, Artifact, Deployment, institutional mutation, public deployment, production action, live schema application, external provisioning, and institutional configuration or trust issuance.

## Acceptance gate

1. CR-011 and all prior gates remain satisfied; repository defaults fail closed.
2. Signed-bundle verification and exact action-specific OPA permit precede every prerequisite, image, and runtime access; denial carries no scope.
3. Security evidence, candidate evidence, and the `SecurityValidation`-stopped run are exact tenant/purpose/identity/digest/evidence bound, immutable, and read only.
4. OPA authorizes the exact image, isolation, environment/network scope, positive resource/time limits, roles, classification, and `sandbox-result` output.
5. Image approval and supply-chain assurance are exact, current, sovereign, digest-bound, provenance/SBOM/signature complete, and perform no transfer or execution.
6. Only `GovernedSandboxService` invokes the provider-neutral `ISecuritySandboxRuntime` after all controls pass.
7. Runtime requests and signed responses are bounded and bind the exact candidate, image, isolation, operator/profile, allowed destinations, result, and evidence.
8. Runtime attestation proves Firecracker-class ephemeral microVM isolation, no production credentials, no host filesystem, and enforced network default deny with exact allowed destinations.
9. Acceptance requires zero exit, no timeout, no isolation violation, unique non-placeholder produced references, and execution evidence.
10. Result authorization binds identity, roles, purpose, environment, classification, prerequisites, image, isolation, environment/network scope, result digest, and evidence.
11. Evidence is immutable, idempotent, tenant scoped, atomic, and SHA-256 qualified.
12. The receipt records no production effect and cannot advance; Tests and its Sandbox receipt reader remain disconnected.
13. Repository defaults contain no runtime, image, limits, environment/network scope, keys, endpoints, credentials, or institutional data.
14. All 15 projects build with zero warnings and zero errors and the complete verifier succeeds.
