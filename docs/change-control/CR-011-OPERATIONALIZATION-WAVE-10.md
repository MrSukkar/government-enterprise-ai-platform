# CR-011 — Operationalization Wave 10: Governed Security Validation

Status: **Approved for Operationalization Wave 10**

Preceding completed authority: `docs/change-control/CR-010-OPERATIONALIZATION-WAVE-09.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## Change Request and decision

Operationalize only Security Validation by connecting Increment 11 to the immutable accepted Wave 09 Static Validation evidence, its exact Code Generation candidate, a delivery-run snapshot stopped at `StaticValidation`, signed-bundle/OPA authorization, deployment-configured cryptographically signed deterministic in-process Security control profiles, the existing `CodeValidationPipeline`, per-result RBAC/ABAC authorization, and immutable PostgreSQL evidence.

Decision: **Approved by the repository owner on 2026-09-08 for bounded Wave 10 implementation, local verification, source control, and GitHub synchronization.** The active instruction “كمل وابدا العمل والتنفيذ” supplies approval.

## Approved implementation boundary

- Verify the signed policy bundle and exact `internal-service.security-validation.create` scope before any Static receipt, candidate, or run read and before any control execution.
- Read and revalidate the exact accepted Static receipt by tenant, purpose, validation/generation/run identities, candidate digest, Static-report digest, and evidence reference.
- Read the authoritative inert Code Generation candidate through the exact Static evidence chain; caller content is never authoritative.
- Read only a deployment-supplied run snapshot bound to the same tenant, purpose, identities, and digests and stopped exactly at `StaticValidation`.
- Add an action-specific Security Validation OPA scope containing exact control identities, required roles, maximum classification, and `security-report` output kind. Other actions reject this scope; denial carries none.
- Configure an exact set of signed institutional Security control profiles. Profiles are unique, canonically signed, pinned-trust verified, and contain only deployment-selected deterministic required/forbidden text and allowed-extension rules.
- Execute all and only the exact authorized controls through `CodeValidationPipeline` at `ValidationGate.Security`. Missing, wrong-gate, incomplete, unevidenced, Error, or Critical results fail closed.
- Re-authorize the complete report using authenticated identity, roles, purpose, environment, classification, exact prerequisite/report digests, controls, findings, and evidence before release.
- Append immutable, idempotent, tenant-scoped PostgreSQL evidence. Compose only when every Wave 09, PostgreSQL, policy, trust, profile, reader, authorization, and evidence dependency is complete.
- `IAuthorizedSecurityValidationReceiptReader`, Sandbox, and later stations remain disconnected.

## Architecture and security review

This conforms without architectural deviation. It reuses .NET 10, PostgreSQL, OPA, RBAC/ABAC, built-in cryptography, and the existing validation pipeline. Candidate data remains inert and in memory. No source write, compiler, process, command, dynamic acquisition, package operation, network control, external analyzer, AI runtime, candidate or sandbox execution, workflow mutation, or production effect is introduced.

No scanner vendor, external service, package, project, database, credential, endpoint, institutional control, rule, key, numerical threshold, timeout, retry, SLO, or warning waiver is selected in repository defaults.

## Out of scope

Sandbox and every later station; source/file mutation; compilation; command, process, package, network, tool, secret, external analyzer, AI, candidate or sandbox execution; Git, CI/CD, deployment, workflow advancement, institutional mutation, external effect, production action, live schema application, external provisioning, and institutional control data or trust issuance.

## Acceptance gate

1. CR-010 and all prior gates remain satisfied; repository defaults fail closed.
2. Signed-bundle verification and exact action-specific OPA permit precede every prerequisite read and control execution; denial carries no scope.
3. Static evidence, candidate evidence, and the `StaticValidation`-stopped run are exact tenant/purpose/identity/digest/evidence bound and read only.
4. Requested, authorized, configured, and executed Security-control sets match exactly.
5. Profiles are unique, structurally valid, canonically signed, and verified using deployment-pinned RS256 or ES256 trust before candidate inspection.
6. Controls are deterministic in-process `ValidationGate.Security` implementations with no I/O, process, package, AI, network, mutation, or execution capability.
7. Every report and finding is structurally valid and evidence-bearing; any Error or Critical finding fails closed.
8. Result authorization binds identity, roles, purpose, environment, classification, prerequisites, report, controls, findings, and evidence.
9. Evidence is immutable, idempotent, tenant scoped, atomic, and SHA-256 qualified.
10. The receipt is Security, non-executable, non-advancing; Sandbox and its receipt reader remain disconnected.
11. Repository defaults contain no controls, rules, keys, endpoints, credentials, or institutional data.
12. All 15 projects build with zero warnings and zero errors and the complete verifier succeeds.
