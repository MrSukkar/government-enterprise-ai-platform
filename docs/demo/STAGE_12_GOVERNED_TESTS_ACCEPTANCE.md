# Integration Demo Stage 12 — Governed Tests Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-034-INTEGRATION-DEMO-GOVERNED-TESTS.md`

Preceding gate: `docs/demo/STAGE_11_SECURITY_SANDBOX_ACCEPTANCE.md`

## Verification record

All checks passed: exact accepted Sandbox and Security evidence chain, signed permit/deny OPA scope, immutable governed manifest, exact image assurance, Firecracker-class isolation, non-secret references, real-runtime-only boundary, exact required-test coverage, fail-closed defaults, result authorization, immutable evidence, Compose, OPA bundle validation, and the 15-project build with 0 warnings and 0 errors.

Every required test must be unique, discovered, completed, passed, unskipped, and evidence-bearing. Missing, duplicate, skipped, failed, timed-out, isolation-violating, or unevidenced results fail closed. No fake runtime or fabricated successful result is introduced.

The receipt records `ProductionEffectOccurred: false` and `CanAdvance: false`. Human Review and all later stages remain outside this gate.
