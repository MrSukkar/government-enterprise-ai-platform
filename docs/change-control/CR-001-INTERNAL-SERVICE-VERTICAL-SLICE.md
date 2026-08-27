# CR-001 — Create Internal Service Workspace

Status: **Approved for Operational Increment 01**
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

### Out of scope

- Production deployment.
- Fake or in-memory production adapters.
- Direct AI execution.
- New database or graph decisions.
- Microservices or new .NET projects.
- Bypassing OPA, human approval, Git, CI/CD, security, telemetry, or evidence.

### Affected boundaries

- `Platform.Web`: product experience.
- `Platform.Api`: governed API exposure.
- `Platform.SoftwareFactory`: product foundation contract.
- OpenAPI and runtime verification: non-regression controls.

## 3. Architectural Review

Decision: **Conforms without architectural deviation**.

- The solution remains a 15-project .NET 10 Modular Monolith.
- PostgreSQL remains the primary database baseline; no persistence adapter is invented in Increment 01.
- Neo4j remains the Enterprise Graph baseline.
- OPA remains Policy Authority.
- AI remains a proposal runtime and has no policy or workflow authority.
- Material actions remain unavailable without governed identity, policy, approval, and evidence.
- No direct `AI -> Production` path exists.
- The public endpoint exposes product metadata only and grants no execution authority.

## 4. Decision

Approve Create Internal Service Workspace as the first business/domain product. Approve Increment 01 as a safe product surface and contract that makes the governed delivery path visible and interactive while material execution remains fail-closed.

Future increments require the same control record to be extended or a new Change Request when scope or architecture changes.

## 5. Master Specification Update

`docs/PROJECT_MASTER_SPECIFICATION_V2.md` includes the CR-001 business implementation addendum. The constitutional architecture and the fixed 30-phase roadmap are unchanged.

## 6. Approval

Approved scope:

- Product: Create Internal Service Workspace.
- Increment: 01 — Governed Intent Workspace.
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
