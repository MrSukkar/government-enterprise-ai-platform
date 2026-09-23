# Integration Demo Stage 14 — Governed Git Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-036-INTEGRATION-DEMO-GOVERNED-GIT.md`

Preceding gate: `docs/demo/STAGE_13_GOVERNED_HUMAN_REVIEW_ACCEPTANCE.md`

## Verification record

All checks passed: exact approving Human Review, accepted Tests, authoritative inert candidate, and `HumanReview`-stopped delivery-run evidence; signed permit/deny OPA scope; exact immutable change set and normalized paths; clean immutable base and non-protected branch constraints; provider-neutral signed institutional Git gateway boundary; secret and unrelated-content rejection; result authorization; fail-closed defaults; append-only PostgreSQL evidence; Compose; OPA bundle validation; and the 15-project build with 0 warnings and 0 errors.

No fake repository, base, materializer, commit, signature, gateway response, private key, credential, trust material, endpoint, institutional data, or fabricated success is included. Runtime material and the exact `HumanReview`-stopped snapshot are deployment-provisioned outside Git. Missing or mismatched evidence, unauthorized content, dirty or stale base, protected branch, force update, unsigned response, invalid trust, or unavailable dependency fails closed.

The receipt records `SourceMutationOccurred: true`, `ProductionEffectOccurred: false`, `CiCdTriggered: false`, and `CanAdvance: false`. Pull requests, CI/CD, and all later stages remain outside this gate.
