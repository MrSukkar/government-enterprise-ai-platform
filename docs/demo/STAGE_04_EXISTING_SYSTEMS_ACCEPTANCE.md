# Integration Demo Stage 04 — Existing Systems Discovery Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-026-INTEGRATION-DEMO-EXISTING-SYSTEMS.md`

Preceding gate: `docs/demo/STAGE_03_AUTHORIZED_ENTERPRISE_CONTEXT_ACCEPTANCE.md`

## Verification record

| Check | Result |
|---|---|
| Signed Stage 04 OPA bundle build and verification | Pass |
| OPA Existing Systems permit | Pass — exact two UUID systems, `DependsOn`, `enterprise-graph`, `Developer`, `Internal`, maximum two results |
| Local OPA, PostgreSQL, and Neo4j containers | Pass; localhost-only and pinned images |
| PostgreSQL schema | Pass; Governed Intent, Enterprise Context, and Existing Systems evidence tables present |
| Synthetic graph fixtures | Pass; two scoped systems plus one out-of-scope fixture |
| UI boundary | Pass; discovery requires a released, non-advancing Enterprise Context receipt and the systems permission |
| Solution build | Pass — all 15 projects, 0 warnings, 0 errors |

## Boundary assertions

- OPA scope is action-specific; it cannot reuse the Intent or Enterprise Context scope.
- The Existing Systems source uses fixed parameterized read-only Cypher and per-result authorization.
- The out-of-scope fixture is absent from OPA's authorized system identifiers and cannot be released.
- A denied policy carries no scope and cannot create inventory evidence.
- `CanAdvance` remains `false`; Existing Architecture and every later station remain disconnected.

## Next gate

Stage 05 may only be separately approved as Existing Architecture discovery.
