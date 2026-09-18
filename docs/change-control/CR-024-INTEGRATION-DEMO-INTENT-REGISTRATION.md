# CR-024 — Integration Demo Stage 02: Governed Intent Registration

Status: **Approved for bounded implementation**

Authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

Preceding gate: `docs/demo/STAGE_01_GOVERNED_INTENT_ACCEPTANCE.md`

## Outcome

Demonstrate the existing Governed Intent Registration station end to end using a signed OPA bundle and atomic PostgreSQL persistence after the authenticated Stage 01 experience.

## Approved scope

- OPA `1.20.2-static`, pinned by multi-platform image digest.
- PostgreSQL `18.6-bookworm`, pinned by multi-platform image digest.
- Localhost HTTPS/TLS only, using runtime-generated and locally trusted material.
- OPA startup must fail when the policy bundle signature is absent or invalid.
- PostgreSQL must require TLS; the application connection uses `SSL Mode=VerifyFull`.
- The existing migration is applied by the demo database initializer, never by application startup.
- The UI may submit the exact validated intent for registration and display created, unchanged, or denied outcomes.
- Acceptance covers permit, deny, deterministic idempotency, optimistic conflict, and no database mutation on policy or dependency failure.

## Invariants

1. Local RBAC/ABAC and signed-bundle-bound OPA permit precede database access.
2. OPA denial cannot call the registration repository.
3. Registration and its evidence reference commit atomically.
4. Repeating the exact request returns the unchanged record; a mismatched conflict fails closed.
5. No secret, token, private key, password, generated bundle, certificate private key, log, or database volume is committed.
6. Repository-default behavior remains unconfigured and fail closed.
7. No later vertical-slice station, workflow advancement, AI invocation, production action, new service boundary, or Phase 31 is introduced.

## Image decision

- `openpolicyagent/opa:1.20.2-static@sha256:bb245e9e36be0d0ed486c240b606c56be7aba96014a4a87895fed4ba7a6dfa8d`
- `postgres:18.6-bookworm@sha256:1c59e2c3c818eaa0f0628f695b36e7c9e362d6b219b36a54a32df645cbd7e1af`

These are official images and are limited to the local Integration Demo.

## Decision

Approved by the repository owner on 2026-09-18 for implementation, local verification, source control, GitHub synchronization, and CI only.
