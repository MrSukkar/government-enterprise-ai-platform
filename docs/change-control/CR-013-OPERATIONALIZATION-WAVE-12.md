# CR-013 — Operationalization Wave 12: Governed Tests

Status: **Approved for Operationalization Wave 12**

Preceding completed authority: `docs/change-control/CR-012-OPERATIONALIZATION-WAVE-11.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## Change Request and decision

Operationalize only Governed Tests Execution by connecting Increment 13 to immutable accepted Wave 11 Sandbox evidence and its Security/Code Generation chain, a run stopped at `Sandbox`, signed OPA authorization, an immutable governed test-manifest catalog, institutional image assurance, a provider-neutral sovereign HTTPS test runtime, result authorization, and PostgreSQL evidence.

Decision: **Approved by the repository owner on 2026-09-08 for bounded Wave 12 implementation, isolated deterministic test invocation only under complete deployment controls, local verification, source control, and GitHub synchronization.** The active instruction to continue with the next station supplies approval.

## Approved boundary

- OPA verifies the exact Sandbox/Security/candidate digests, manifest reference/digest, required tests/categories, institutional image, Firecracker-class isolation, non-secret environment/network scope, positive configured limits, roles, classification, and `tests-result` output before every read or invocation; denial carries no scope.
- Immutable PostgreSQL readers revalidate the exact Sandbox chain, Security evidence, inert candidate, `Sandbox`-stopped run, and governed manifest after permit.
- Existing package eligibility and cryptographic supply-chain assurance govern the exact image without transfer or execution authority.
- Only `IGovernedTestRuntime` is invoked through a bounded provider-neutral sovereign HTTPS protocol. Its signed response must bind operator/profile, request, candidate, image, manifest, exact test coverage/results, Firecracker-class isolation, network default deny, and evidence.
- Every required test must be unique, discovered, completed, passed, unskipped, and evidenced. Timeout, isolation violation, missing/substituted/duplicate/failed/skipped required tests fail closed.
- The result is RBAC/ABAC-authorized and recorded as immutable, idempotent, tenant-scoped PostgreSQL evidence. It has no production effect and cannot advance.
- `IAuthorizedTestsExecutionReceiptReader`, Human Review, and later stations remain disconnected.

## Architecture and security review

This conforms without architectural deviation and reuses .NET 10, PostgreSQL, OPA, RBAC/ABAC, built-in cryptography, existing package controls, isolation policy, and the vendor-neutral runtime contract. The HTTPS endpoint may be sovereign or air-gapped and is not a mandatory external control plane. No framework, runner, provider, image, manifest, endpoint, credential, key, environment/network value, resource/time value, request/response bound, coverage threshold, SLO, package, project, database, or service boundary is selected by repository defaults.

## Out of scope

Human Review and later stations; caller-authored or dynamically acquired tests/tools/packages; load, penetration, destructive, production-data, or external-integration tests; production credentials/data/endpoints/effects; host access; unrestricted networking; source or workspace mutation; retries or workflow mutation; Git, CI/CD, deployment, public deployment, live schema application, external provisioning, and institutional configuration or trust issuance.

## Acceptance gate

1. CR-012 and prior gates remain satisfied and repository defaults fail closed.
2. Verified action-specific OPA permit precedes all reads, image access, and test invocation.
3. Sandbox, Security, candidate, run, and manifest are exact tenant/purpose/identity/digest/evidence bound, immutable, and read only.
4. OPA scope exactly authorizes manifest, tests/categories, image, isolation, environment/network scope, roles, classification, and output kind.
5. Image assurance is exact, current, sovereign, digest/provenance/SBOM/signature complete, and effect free.
6. Only the bounded signed sovereign `IGovernedTestRuntime` protocol executes the exact manifest under attested Firecracker-class isolation.
7. Required test coverage/results and all evidence are exact; any missing, duplicate, failed, skipped, timed-out, isolated-violating, or unevidenced required test fails closed.
8. Result authorization and append-only PostgreSQL evidence bind the complete chain.
9. Receipt has no production effect and cannot advance; Human Review remains disconnected.
10. Defaults contain no runtime, manifest, image, limits, environment/network scope, keys, endpoint, credentials, or institutional data.
11. All 15 projects build with zero warnings and errors and the complete verifier succeeds.
