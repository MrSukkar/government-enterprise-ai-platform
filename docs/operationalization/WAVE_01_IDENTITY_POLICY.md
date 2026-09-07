# Operationalization Wave 01 — Sovereign Identity and Policy Control Plane

Status: **Implemented under approved CR-002**

## Purpose and boundary

This wave supplies the shared identity and policy control-plane adapters required before institutional capability adapters can be connected. It does not select an identity provider, provision credentials, choose trust material, connect business data, or authorize a production action. The checked-in profile is deliberately unconfigured, exposes readiness state, and fails closed.

## Identity configuration

Configuration section: `IdentityProvider`.

- `Authority`: absolute HTTPS issuer authority with no user information, query, or fragment.
- `Audience`: non-empty token audience with no whitespace.
- `RequireHttpsMetadata`: must remain `true`.

Only complete, valid configuration selects the official `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.11 handler. It validates issuer, audience, lifetime, signing key, signed-token and expiration requirements with zero clock skew and without inbound claim remapping.

After cryptographic validation, the governed principal must contain `sub`, `tenant_id`, `iss`, `clearance`, and `authorization_evidence`; roles and permissions remain available for downstream authorization. Authentication alone never authorizes a governed action.

## Policy configuration

Configuration section: `PolicyControlPlane`.

- `OpaEndpoint`: absolute HTTPS OPA decision endpoint.
- `BundleVerificationEndpoint`: absolute HTTPS signed-bundle verification endpoint.
- `TrustAnchorReference`: deployment-owned reference interpreted by the sovereign verification service; no key material is stored here.
- `Environment`: exact deployment environment binding.
- `RequestTimeoutSeconds`: positive deployment-selected request bound.
- `MaximumResponseBytes`: positive deployment-selected response bound.

Both endpoints reject user information, query, and fragment components. Transport trust is supplied by the sovereign host trust configuration. The bundle verifier binds identity, version, SHA-256 digest, environment, signature validity, evidence, and activation time. OPA runs only after verification and revalidates the exact bundle, request, environment, decision, evidence, and decision time returned by the service.

Unavailable, timed-out, non-successful, oversized, empty, non-JSON, malformed, mismatched, or denied responses stop execution. OPA remains policy authority only and gains no workflow or action authority.

## Readiness states

`/health/ready` reports each control plane as `unconfigured`, `invalid`, or `configured` without returning configuration values, claims, tokens, credentials, or trust references. `configured` means the local configuration contract is complete; it does not claim that deployment integration passed.

Live provider validation, organizational claim mapping, trust-anchor selection, credentials, and deployment acceptance require separate authorization.
