# Phase 05 — Identity & Access

Status: **Implemented**  
Depends on: **Phase 04 — Frontend Foundation**

## Trust boundary

The platform uses standards-based OIDC/OAuth2 identity. A sovereign deployment supplies its trusted authority and audience; the domain does not bind to a commercial identity provider. Token signature, issuer, audience, lifetime, and cryptographic validation must be performed by an approved ASP.NET Core authentication handler at the API boundary before a `GovernedIdentity` is created.

## Implemented authorization foundation

Every access evaluation includes:

- authenticated subject and issuer;
- tenant scope;
- declared purpose;
- requested action and resource;
- identity clearance and resource classification;
- required RBAC roles;
- ABAC attributes carried in the governed identity;
- initiator identity and distinct-approver requirement;
- optional certificate thumbprint for PKI-bound assurance context.

The default policy evaluator denies missing identity, purpose, tenant mismatch, insufficient classification clearance, missing roles, and separation-of-duties conflicts. Unknown or incomplete context therefore fails closed.

## API posture

- ASP.NET Core authentication and authorization services are enabled.
- A fallback policy requires an authenticated user for future endpoints.
- `/health` is the only explicitly anonymous endpoint.
- No custom token parser or cryptography is implemented.
- Identity-provider configuration contains no credentials or environment-specific authority.

## Deferred integrations

- Selecting and configuring the approved sovereign OIDC provider is an environment decision.
- External policy evaluation with OPA is implemented in Phase 20; Phase 05 establishes its identity and access input boundary.
- Evidence persistence and cryptographic proof are implemented in Phase 30.

