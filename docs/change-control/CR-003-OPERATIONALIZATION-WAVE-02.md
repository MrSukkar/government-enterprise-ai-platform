# CR-003 — Operationalization Wave 02: Governed Intent Registration Runtime

Status: **Approved for Operationalization Wave 02**

Preceding completed authority: `docs/change-control/CR-002-OPERATIONALIZATION-WAVE-01.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Operationalize the first state-changing station of the approved Create Internal Service path by connecting the existing governed Intent Registration contract to the approved sovereign policy control plane and PostgreSQL primary database.

Wave name: **Operationalization Wave 02 — Governed Intent Registration Runtime**.

### Outcome

When—and only when—deployment-approved identity, policy, and PostgreSQL configuration are all valid, the protected Intent Registration endpoint can obtain an exact, signed-bundle-bound OPA decision and atomically create or return the same governed intent under tenant scope, deterministic idempotency, and optimistic concurrency.

Missing or invalid configuration, authentication failure, policy denial or mismatch, database unavailability, concurrency conflict, malformed data, or evidence failure leaves institutional state unchanged and fails closed.

## 2. Current-state evidence

- All 30 phases and CR-001 Operational Increments 01–22 are complete and verified.
- CR-002 Wave 01 provides configuration-gated JWT bearer authentication plus shared sovereign signed-policy-bundle and OPA transport behavior.
- `IGovernedIntentPolicyGate` remains unregistered.
- `IGovernedIntentRegistrationRepository` remains unregistered.
- The Intent Registration engine already enforces identity, tenant, permission, classification, purpose, signed policy identity, exact decision correlation, idempotency, version checks, and returned-state validation.
- PostgreSQL is the approved primary database, but no provider package, connection, schema, or repository adapter is currently selected or configured.
- Repository-default runtime readiness exposes 142 disconnected institutional dependencies and remains fail closed.

## 3. Impact Analysis

### In scope

- A configuration-gated PostgreSQL data-source boundary using the approved primary database decision.
- The `Npgsql` package pinned and locked at `10.0.3`; no ORM is introduced.
- Deployment-owned connection configuration with no checked-in connection string, password, certificate, or secret.
- Versioned, reviewable schema assets for governed intent registration; schema changes are not executed automatically at application startup.
- An `IGovernedIntentRegistrationRepository` adapter that enforces tenant-scoped keys, exact immutable payloads, deterministic idempotency, optimistic concurrency, atomic registration plus evidence reference, explicit transactions, cancellation, parameterized SQL, and bounded command timeout.
- An `IGovernedIntentPolicyGate` adapter that uses the Wave 01 signed-bundle verification and sovereign OPA boundary, maps only the approved Intent policy input, and revalidates the exact decision before returning it.
- Refactoring the Wave 01 policy transport only as needed to support typed policy inputs and outputs without weakening existing governed-action validation.
- Dependency-injection composition that registers these adapters only when the complete policy and database profiles are valid.
- Readiness disclosure for PostgreSQL configuration state without exposing host, database, username, connection string, or trust material.
- Deterministic repository/profile checks for unconfigured, invalid, idempotent, concurrency-conflict, tenant-isolation, policy-denial, malformed-response, unavailable-database, and cancellation paths.
- Updated operational documentation, acceptance evidence, UI status, and repository verification.

### Out of scope

- Provisioning PostgreSQL, creating a database, applying schema to an external environment, acquiring credentials, choosing a secret manager, or selecting deployment certificates.
- Automatic schema creation or migration during API startup.
- Entity Framework Core, Dapper, a new persistence framework, or a second database decision.
- Persisting submissions before OPA permit, weakening optimistic concurrency, cross-tenant reads, mutable evidence, or permissive retry after an ambiguous commit.
- `IGovernedIntentRegistrationReader`, Enterprise Context discovery, later vertical-slice repositories, durable Agentic Work, Enterprise Model persistence, Evidence Chain storage, Neo4j, GraphRAG, AI, Git, CI/CD, deployment, or production action.
- Organization-specific policy content, provider-specific claims, public deployment, external account creation, network mutation, or secret acquisition.
- A new service boundary, phase, queue, cache, mandatory SaaS dependency, conditional technology decision, or numerical SLO.

### Affected boundaries

- `Platform.Governance` owns the reusable sovereign policy transport and exact policy-decision validation boundary.
- `Platform.SoftwareFactory` owns the Intent policy adapter and governed intent repository implementation because it owns the contract and state semantics.
- `Platform.Api` composes valid configuration and continues to fail closed when any prerequisite is absent.
- PostgreSQL remains a deployment-controlled infrastructure dependency and gains no policy or workflow authority.

## 4. Data and security review

Required invariants:

1. OPA permit and signed bundle verification precede any database transaction or mutation.
2. Every SQL value is parameterized; dynamic identifiers and user-controlled SQL are prohibited.
3. Tenant identity is present in the primary/unique access path and in every read or conflict comparison.
4. The registration identifier, submission identifier, intent digest, and idempotency key are exact and immutable after creation.
5. An unchanged result is valid only when the complete stored record exactly matches the authorized candidate; otherwise the operation fails closed.
6. Optimistic version mismatch raises the existing governed concurrency failure without overwriting state.
7. Registration state and its evidence reference become visible atomically or not at all.
8. Connection strings, credentials, certificate material, SQL parameter values, governed intent content, raw OPA input, and sensitive claims are not logged or returned by readiness.
9. Database TLS and trust settings remain deployment controlled; insecure transport is not enabled by repository defaults.
10. Cancellation and ambiguous transaction outcomes never trigger an automatic duplicate mutation.

## 5. Proposed implementation sequence

1. Define validated PostgreSQL operational options with an empty, fail-closed repository profile.
2. Add and lock `Npgsql` 10.0.3 and build a singleton data source only for valid configuration.
3. Add reviewable governed-intent schema and explicit deployment migration guidance.
4. Extract a typed, bounded sovereign policy client from the Wave 01 transport while preserving current behavior.
5. Implement the exact Intent OPA policy-gate adapter.
6. Implement the atomic PostgreSQL governed-intent repository.
7. Register both exact runtime contracts only under complete configuration.
8. Update readiness, OpenAPI descriptions, UI status, verification, acceptance, and project state.
9. Run the full project verifier and commit/push only verified source.

## 6. Architectural Review

Finding: **Conforms without architectural deviation, subject to explicit approval and implementation verification**.

- PostgreSQL is already the approved primary database.
- OPA remains the sole policy authority, and its decision precedes persistence.
- The change activates existing contracts inside the Modular Monolith and introduces no service or workflow authority.
- The repository adapter owns persistence mechanics only; domain validation remains in the existing engine.
- The approach remains compatible with sovereign, on-premises, and air-gapped operation.

## 7. Package decision proposed

Approve the `Npgsql` package at version `10.0.3`, the current stable .NET-compatible release verified from the official Npgsql/NuGet publication. No ORM or additional database package is proposed.

## 8. Decision requested

Approve **CR-003 — Operationalization Wave 02: Governed Intent Registration Runtime** for bounded implementation, local verification, source control, and GitHub synchronization only.

Decision: **Approved by the repository owner for Operationalization Wave 02**.

Repository-owner decision: **Approved on 2026-09-07**.

No external credentials, database provisioning, schema application to an external database, public deployment, provider provisioning, trust-anchor selection, or organization-specific policy content is authorized by this request.

## Acceptance gate

1. Repository-default and incomplete profiles remain unconfigured and fail closed without database access.
2. Signed policy-bundle verification and exact OPA permit precede persistence.
3. Deny, mismatch, malformed response, timeout, or policy unavailability cannot open a transaction.
4. Atomic registration preserves exact tenant scope, idempotency, evidence, and optimistic concurrency.
5. SQL is parameterized and schema application is explicit rather than automatic at startup.
6. Database errors and ambiguous outcomes do not produce a success receipt or automatic duplicate write.
7. No secret, connection string, trust material, governed payload, or sensitive policy input is committed or logged.
8. Existing 30 phase, 22 increment, and Wave 01 gates remain satisfied.
9. All 15 projects build with zero warnings and zero errors.
10. Runtime verification proves safe behavior in the unconfigured repository profile.
