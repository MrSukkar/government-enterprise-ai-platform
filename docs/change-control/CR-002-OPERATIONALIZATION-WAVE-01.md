# CR-002 — Operationalization Wave 01: Sovereign Identity and Policy Control Plane

Status: **Approved for Operationalization Wave 01**

Preceding completed authority: `docs/change-control/CR-001-AMENDMENT-19-EVIDENCE-COMPLETION.md`

Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Begin operationalization of the completed Create Internal Service vertical-slice contracts without creating Phase 31 or changing the approved architecture.

Wave name: **Operationalization Wave 01 — Sovereign Identity and Policy Control Plane**.

### Outcome

Replace the current unconditional fail-closed authentication placeholder with a configuration-gated OIDC/OAuth2 bearer adapter and establish the shared, vendor-neutral OPA decision and signed-policy-bundle verification adapters required by later operational contracts. When deployment-approved configuration or trust material is absent or invalid, the platform continues to fail closed exactly as it does today.

This wave establishes who is calling and which signed policy authority may decide. It does not connect persistence, institutional data, AI, Git, CI/CD, registries, sandbox, deployment, telemetry, Enterprise Model mutation, or production action.

## 2. Current-state evidence

- The approved 30 phases and all 22 CR-001 operational increments are complete and verified.
- `Platform.Identity` currently registers `FailClosedAuthenticationHandler` as the default scheme.
- `IdentityProviderOptions` already defines deployment-configured `Authority`, `Audience`, and HTTPS metadata enforcement.
- `GovernedRequestContextFactory` already requires authenticated `sub`, `tenant_id`, `iss`, `clearance`, and `authorization_evidence` claims and extracts roles and permissions.
- `IPolicyBundleVerifier` and `IOpaPolicyDecisionPoint` are approved abstractions without registered operational implementations.
- Runtime readiness currently exposes 143 missing institutional dependencies and returns `503` fail closed.

## 3. Impact Analysis

### In scope

- Configuration-gated ASP.NET Core bearer authentication using OIDC/OAuth2 authority and audience.
- HTTPS metadata enforcement by default and rejection of incomplete or unsafe authority configuration.
- Strict issuer, audience, lifetime, signing-key, and authenticated-principal validation through the platform authentication boundary.
- Preservation of the exact governed claim contract: subject, tenant, issuer, clearance, authorization evidence, roles, and permissions.
- A vendor-neutral, sovereign-compatible OPA decision-point adapter with deployment-controlled endpoint and trust configuration.
- A signed-policy-bundle verifier with deployment-controlled trust anchors and no embedded keys.
- Explicit timeouts and bounded response handling as policy values; no invented service-level objective.
- Health/readiness disclosure that distinguishes unconfigured, invalid, and operational identity/policy boundaries without exposing secrets.
- Deterministic contract and repository-profile checks for configuration selection, missing configuration, strict issuer/audience/signature requirements, unavailable OPA, invalid policy signature, malformed response, tenant mismatch, and excessive classification. Live configured-provider success remains deployment-controlled acceptance because no provider or credential is authorized in this wave.
- Updated OpenAPI security description, operational documentation, acceptance evidence, and full repository verification.

### Out of scope

- Selecting or provisioning a specific government identity provider, tenant, issuer URL, client credential, certificate, key, or HSM.
- Committing tokens, passwords, client secrets, private keys, certificates, workstation state, or sample production identities.
- Development backdoors, hard-coded users, self-issued bearer tokens, authorization bypasses, permissive fallback, or fake OPA approval.
- Mapping an external provider's organization-specific claims without an approved deployment claim profile.
- Persistence adapters, PostgreSQL, Neo4j, institutional knowledge/data, AI/model runtime, Git, CI/CD, artifact registry, sandbox, deployment, OpenTelemetry, registration, or production effects.
- Public deployment, external account creation, network mutation, or credential acquisition.
- New service boundary, database, queue, mandatory SaaS dependency, conditional technology decision, or numerical SLO.

### Affected boundaries

- `Platform.Identity` owns authentication configuration, token validation, and governed-principal construction.
- `Platform.Governance` owns signed bundle verification and OPA evaluation abstractions.
- `Platform.Api` composes configured adapters and continues to expose fail-closed readiness.
- All business engines remain unchanged and continue to require their exact policy-gate adapters.
- Secrets and private trust material remain deployment concerns and are never stored in source control.

## 4. Security review

Required invariants:

1. Missing, incomplete, HTTP, mismatched, expired, unsigned, unverifiable, unavailable, or malformed identity/policy inputs deny access.
2. Authentication success alone grants no permission; RBAC/ABAC, purpose, tenant, classification, and endpoint-specific permissions remain mandatory.
3. OPA is policy authority but has no workflow authority and cannot directly perform an action.
4. Policy bundles are versioned, digest-bound, signed, environment-aware, and verified before evaluation.
5. No token, authorization header, secret, private key, raw policy input, or sensitive claim is logged.
6. Sovereign and air-gapped deployments can use locally operated identity, policy, PKI, and trust endpoints.
7. The existing fail-closed scheme remains the default whenever the operational profile is not validly configured.

## 5. Proposed implementation sequence

1. Define validated identity and policy operational options with safe defaults.
2. Add the conditional OIDC/OAuth2 bearer composition path while preserving the existing fail-closed path.
3. Add strict governed-principal validation and non-sensitive diagnostics.
4. Implement the deployment-neutral signed-policy-bundle verifier boundary.
5. Implement the deployment-neutral OPA decision-point boundary.
6. Add readiness checks and conformance verification.
7. Run the full project verifier; update status atomically only after success.
8. Commit and push verified source without credentials or deployment-specific values.

## 6. Architectural Review

Finding: **Conforms without architectural deviation, subject to explicit approval and implementation verification**.

- OIDC/OAuth2, RBAC/ABAC, OPA, signed policies, PKI/key management, sovereign operation, and fail-closed behavior are already approved in the Master Specification.
- The wave introduces adapters inside existing modules and no new service, phase, database, or authority.
- The LLM, UI, API, and workflow cannot substitute for identity or policy decisions.
- No mandatory external control plane is introduced.

## 7. Decision requested

Approve **CR-002 — Operationalization Wave 01: Sovereign Identity and Policy Control Plane** for bounded implementation, local verification, source control, and GitHub synchronization only.

Decision: **Approved by the repository owner for Operationalization Wave 01**.

No external credentials, public deployment, provider provisioning, trust-anchor selection, or organization-specific claim mapping is authorized by this decision.

Implementation package decision: the official Microsoft `Microsoft.AspNetCore.Authentication.JwtBearer` package is approved at version `10.0.11`, matching the installed .NET 10 runtime servicing level. No third-party identity SDK is approved.

Repository-owner decision: **Approved on 2026-09-07**.

## Acceptance gate

1. Invalid or absent configuration retains the current bearer challenge and fail-closed runtime posture.
2. Configured authentication validates authority, issuer, audience, lifetime, signature, and the complete governed claim contract.
3. Signed policy bundle verification precedes OPA evaluation.
4. OPA unavailability, denial, mismatch, malformed output, or invalid evidence fails closed.
5. No endpoint becomes authorized solely because authentication succeeds.
6. No secret or deployment-specific credential is committed or logged.
7. Existing 30 phase and 22 increment gates remain satisfied.
8. All 15 projects build with zero warnings and zero errors.
9. Runtime verification continues to prove safe behavior in the unconfigured repository profile.
