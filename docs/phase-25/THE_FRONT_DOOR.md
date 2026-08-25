# Phase 25 — The Front Door

## Purpose

Phase 25 provides one intent-based Blazor WebAssembly entry experience across `BUILD`, `UNDERSTAND`, `OPERATE`, `ACT`, and `PROVE`. It translates platform complexity into governed outcomes while preserving server authorization as the only access authority.

## Fail-closed experience context

The browser begins with `IsGovernedIdentityEstablished = false`, no tenant, no permissions, and every destination locked. It cannot create a preview persona or infer access from navigation. Only `ApplyServerAuthorizedContext` accepts a context containing subject, tenant, persona, purpose, non-empty permissions, authorization evidence, issuance, and expiry; invalid or expired contexts fail closed.

`CanAccess` requires both an established context and the destination permission. Clearing the context removes identity, tenant, permissions, evidence, and expiry atomically.

## Intent catalog

Each Front Door destination defines a human intent, outcome, explicit permission, current capability statement, and stable page anchor:

- `BUILD` — governed software delivery;
- `UNDERSTAND` — authorized enterprise knowledge and impact;
- `OPERATE` — operations, resilience, and proactive findings;
- `ACT` — governed OPA/approval/MCP action requests;
- `PROVE` — the evidence and trust chain.

Restricted destinations remain visible as locked explanations so absence of navigation is never mistaken for authorization. Available buttons are enabled only from the server-authorized context, and every operation still requires API re-authorization.

## Accessible responsive shell

The Front Door uses semantic headings, sections, navigation, status regions, explicit labels, disabled controls, keyboard focus styling, reduced-motion support, and responsive one-column layouts. The active governed context remains visible in the shell.

## Phase boundary

Phase 25 introduces no new backend capability or permission. Developer tooling begins in Phase 26, and the final cryptographic proof experience remains incomplete until Phase 30.
