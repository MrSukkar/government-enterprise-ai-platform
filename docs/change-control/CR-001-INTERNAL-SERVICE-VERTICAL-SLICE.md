# CR-001 — Create Internal Service Workspace

Status: **Approved through Operational Increment 02**
Authority: Repository owner authorization in the active delivery record
Foundation authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## 1. Change Request

Authorize the first business/domain implementation after completion of the fixed 30-phase foundation. The product is **Create Internal Service Workspace**: a governed workspace that turns an internal government service intent into software, deployment, operational registration, and evidence.

This request creates a product increment, not Phase 31.

### Product promise

An authorized developer can describe a service outcome and progress through the approved vertical slice without bypassing enterprise context, policy, security, human review, Git, CI/CD, observability, registration, or evidence.

## 2. Impact Analysis

### In scope for Increment 01

- Governed Intent Workspace in the existing Blazor frontend.
- Public, non-authoritative product metadata endpoint.
- Display of the approved delivery sequence and control outcomes.
- Local draft completeness evaluation with no server-side institutional mutation.
- Explicit fail-closed execution state until identity and runtime boundaries are connected.
- Runtime verification of the product foundation endpoint.

### In scope for Increment 02

- A protected REST contract for governed intent submission.
- Mapping already-authenticated claims into a bounded governed request context; no token parsing is introduced.
- Server-side validation of subject, tenant, purpose, classification clearance, explicit permission, authorization evidence, and intent evidence.
- A deterministic validation digest and receipt that explicitly records no persistence, no OPA evaluation, and no execution authority.
- A fail-closed authentication scheme while the deployment-approved OIDC/OAuth2 adapter is unavailable.
- Blazor submission behavior that remains disabled without server-authorized context and required evidence.
- Runtime verification that anonymous submission returns `401` with a bearer challenge.

### Out of scope

- Production deployment.
- Fake or in-memory production adapters.
- Live identity-provider credentials or a custom token parser.
- Institutional intent persistence or Enterprise Model mutation.
- OPA evaluation, approval, workflow advancement, or material execution.
- Direct AI execution.
- New database or graph decisions.
- Microservices or new .NET projects.
- Bypassing OPA, human approval, Git, CI/CD, security, telemetry, or evidence.

### Affected boundaries

- `Platform.Web`: product experience.
- `Platform.Api`: governed API exposure.
- `Platform.SoftwareFactory`: product foundation contract.
- `Platform.Identity`: fail-closed authentication and governed claims-to-context mapping.
- OpenAPI and runtime verification: non-regression controls.

## 3. Architectural Review

Decision: **Conforms without architectural deviation through Increment 02**.

- The solution remains a 15-project .NET 10 Modular Monolith.
- PostgreSQL remains the primary database baseline; no persistence adapter is invented in Increment 01.
- Neo4j remains the Enterprise Graph baseline.
- OPA remains Policy Authority.
- AI remains a proposal runtime and has no policy or workflow authority.
- Material actions remain unavailable without governed identity, policy, approval, and evidence.
- No direct `AI -> Production` path exists.
- The public endpoint exposes product metadata only and grants no execution authority.
- The protected submission endpoint validates an authenticated request but produces no institutional state or executable command.
- Claims are consumed only after authentication; no JWT, bearer token, or credential parser is added.
- OPA remains unevaluated and therefore fail-closed for advancement.

## 4. Decision

Approve Create Internal Service Workspace as the first business/domain product. Approve Increment 01 as a safe product surface and approve Increment 02 as a protected, server-validated intent boundary while persistence, OPA, workflow advancement, and material execution remain fail-closed.

Future increments require the same control record to be extended or a new Change Request when scope or architecture changes.

## 5. Master Specification Update

`docs/PROJECT_MASTER_SPECIFICATION_V2.md` includes the CR-001 business implementation addendum. The constitutional architecture and the fixed 30-phase roadmap are unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 01 — Governed Intent Workspace.
- Increment: 02 — Governed Intent Submission.
- Authorization: implementation, verification, source control, and governed CI.
- Release authority: remains subject to successful CI and acceptance evidence.

## Acceptance criteria

1. All 15 projects build with zero warnings and zero errors.
2. Development API starts successfully.
3. Readiness remains fail-closed while required runtime adapters are absent.
4. OpenAPI, developer console, and Create Internal Service foundation are reachable.
5. The Create Internal Service page is usable as a local draft workspace.
6. No material execution is possible without governed identity.
7. No architecture or technology baseline is changed.

### Increment 02 acceptance criteria

1. Anonymous intent submission returns `401` and a bearer challenge.
2. Authenticated claims are required for subject, tenant, issuer, clearance, permission, and authorization evidence.
3. Tenant, purpose, classification clearance, permission, and evidence are revalidated on the server.
4. A successful validation receipt is deterministic, non-persisted, not policy-evaluated, and non-executable.
5. The API contract uses OpenAPI 3.1 and declares the protected security boundary and response shapes.
6. The UI cannot submit without governed context, permission, tenant, and intent evidence.
7. No identity credential, custom token parser, database adapter, OPA decision, workflow advancement, or material action is introduced.
8. All 15 projects build with zero warnings and zero errors and runtime verification succeeds.
