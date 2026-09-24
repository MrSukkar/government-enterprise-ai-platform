# CR-047 — V3-02 Governed Consumer and Channel Lifecycle

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V3.md`; predecessor acceptance: `docs/v3/V3_01_CONSTITUTIONAL_CONTRACTS_ACCEPTANCE.md`.

## Scope

Add product-neutral Consumer and Channel registration and lifecycle contracts with deterministic fingerprints, exact OPA decision binding, optimistic version checks, atomic mutation-plus-Evidence repository boundaries, idempotent registration disposition, and forward-only lifecycle transitions.

## Hard gates

- **Security:** policy authorization occurs before mutation; denial or exact-scope mismatch cannot reach persistence.
- **Compliance:** tenant, purpose, environment, classification, actor, lifecycle, policy, and Evidence stay bound to the constitutional contract.
- **Sovereignty:** no provider, external control plane, credential, trust material, or live adapter is selected.
- **HA/DR:** deterministic idempotency, optimistic concurrency, atomic evidence, and forward-only lifecycle rules are explicit; no numerical objective is invented.

## Boundary

Only inert contracts, ports, and deterministic orchestration are added. Repository defaults supply no policy or persistence adapter and therefore remain unavailable and fail closed. No endpoint, database, gateway, broker, Production effect, AI authority, institutional data, public deployment, Phase 31, or direct AI-to-Production path is introduced.

## Acceptance

Verify permitted atomic registration and Proposed-to-Active transition; verify OPA denial, policy-scope mismatch, and lifecycle rollback fail closed without mutation; run the official verifier and full CI.
