# CR-010 — Operationalization Wave 09: Governed Static Validation

Status: **Approved for Operationalization Wave 09**

Preceding completed authority: `docs/change-control/CR-009-OPERATIONALIZATION-WAVE-08.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## Change Request and decision

Operationalize only Static Validation by connecting Increment 10 to the immutable Wave 08 Code Generation evidence, an exact delivery-run snapshot stopped at `CodeGeneration`, signed-bundle/OPA authorization, deployment-configured cryptographically signed deterministic in-process Static control profiles, the existing `CodeValidationPipeline`, per-result RBAC/ABAC authorization, and immutable PostgreSQL evidence.

Decision: **Approved by the repository owner on 2026-09-08 for bounded Wave 09 implementation, local verification, source control, and GitHub synchronization.** The active instruction “كمل وابدا العمل والتنفيذ” supplies approval.

## Approved implementation boundary

- Revalidate the exact tenant/purpose/candidate-digest-bound Code Generation record and its inert, unapplied artifact only after OPA permit.
- Revalidate a read-only run snapshot bound to the exact generation/candidate/purpose and stopped at `CodeGeneration`; expose no run mutation.
- Add an action-specific Static Validation OPA scope containing exact control identities, required roles, maximum classification, and `static-report` output kind. Other actions reject the scope; denial carries none.
- Add deployment configuration for an exact set of signed institutional Static control profiles. Each profile has a unique control identity, explicit case-sensitive required and forbidden text rules, allowed source-file extensions, approval evidence, signature envelope, and deployment-pinned public-key trust.
- Cryptographically verify each canonical profile before inspecting candidate data. The platform-owned deterministic implementation performs only bounded in-memory text/path inspection through the existing `ICodeValidationControl` and `CodeValidationPipeline` boundaries with `ValidationGate.Static`.
- Required, OPA-authorized, configured, and executed control identity sets must match exactly. Missing, duplicate, wrong-gate, unsigned, malformed, or substituted controls fail closed.
- Findings are deterministic and evidence-bearing. Any Error or Critical finding rejects the report; warnings cannot grant authority.
- Re-authorize the complete report using authenticated identity, exact policy roles, candidate/report digests, control identities, classification, environment, purpose, and evidence before release.
- Append immutable, idempotent, tenant-scoped PostgreSQL validation evidence. Compose only when PostgreSQL, policy, Wave 08, Static trust, and exact control profiles are complete.
- `IAuthorizedStaticValidationReceiptReader` and Security Validation remain disconnected.

## Architecture and security review

This conforms without architectural deviation. It reuses .NET 10, PostgreSQL, OPA, RBAC/ABAC, built-in cryptography, and the existing validation pipeline. No compiler, analyzer SDK, process, command, filesystem writer, package restore, network control, external service, AI runtime, sandbox, Git, workflow mutation, or production effect is introduced. Candidate content remains in-memory data and controls have no capability surface beyond deterministic inspection.

No new package, project, database, service boundary, conditional technology, numerical threshold, warning budget, timeout, retry, SLO, provider, endpoint, credential, institutional profile, rule, key, or evidence value is selected in repository defaults.

## Out of scope

Security Validation and every later station; source/file mutation; compilation or durable artifact; command, process, package, network, tool, secret, sandbox, test, Git, CI/CD, deployment, workflow advancement, institutional mutation, external effect, or production action; live schema application; external provisioning; and institutional control data or trust issuance.

## Acceptance gate

1. Preceding gates remain satisfied and repository defaults keep exactly 142 dependencies fail closed.
2. Signed-bundle verification and exact `internal-service.static-validation.create` scope precede candidate read and all control execution.
3. Code Generation evidence and the `CodeGeneration`-stopped run are exact tenant/purpose/identity/digest/evidence bound and read only.
4. Every configured Static control profile is unique, structurally valid, canonically signed, pinned-trust verified, and exactly authorized by OPA.
5. Controls are deterministic in-process `ValidationGate.Static` implementations with no I/O, process, package, AI, network, or mutation capability.
6. The existing `CodeValidationPipeline` runs all and only exact required controls.
7. Every report and finding is structurally valid and evidence-bearing; any Error/Critical finding fails closed.
8. Result authorization binds identity, roles, scope, candidate/report digests, controls, findings, evidence, purpose, environment, and classification.
9. Evidence is immutable, idempotent, tenant scoped, atomic, and SHA-256 qualified.
10. Receipt is Static, non-executable, non-advancing; Security Validation and its reader remain disconnected.
11. Repository defaults contain no controls, rules, trust keys, institutional data, endpoints, or credentials.
12. All 15 projects build with zero warnings and zero errors and the complete verifier succeeds.
