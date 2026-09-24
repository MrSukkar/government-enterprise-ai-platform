# CR-046 — V3-01 Constitutional Contracts and Enterprise Model Types

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V3.md` and `docs/V3_CAPABILITY_ROADMAP.md`.

## Scope

Add product-neutral constitutional contract types for Consumer, Channel, API, Message, Schema, Integration, and Orchestration assets. Each contract binds identity, version, owner, tenant, purpose, environment, lifecycle, classification, schemas, compatibility, authentication, permissions, OPA policy, data governance, delivery semantics, OpenTelemetry identity, Evidence, and Enterprise Model registration.

## Hard gates

- **Security:** anonymous access, AI authority, missing OPA scope, and Production authorization fail closed.
- **Compliance:** classification, purpose, residency, retention, encryption, redaction, and Evidence references are mandatory.
- **Sovereignty:** contracts select no external control plane, product, credential, or trust material.
- **HA/DR:** idempotency, ordering, retry, timeout, compensation, and replay are explicit policy-bound dispositions; no numeric objective is invented.

## Boundary

This increment adds inert contracts and deterministic validation only. It adds no endpoint, persistence, gateway, broker, orchestration runtime, deployment, external access, institutional data, credential, trust material, Production effect, AI authority, or Phase 31.

## Acceptance

Build all existing projects; execute an actual contract verification covering a permitted contract and denied anonymous, AI-authority, Production-effect, missing-policy, and duplicate-evidence cases; verify all seven Enterprise Model type mappings; run the official verifier.
