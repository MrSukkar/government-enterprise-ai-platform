# Integration Demo Stage 03 — Authorized Enterprise Context Acceptance

Status: **Satisfied**

Change authority: `docs/change-control/CR-025-INTEGRATION-DEMO-ENTERPRISE-CONTEXT.md`

Preceding gate: `docs/demo/STAGE_02_GOVERNED_INTENT_REGISTRATION_ACCEPTANCE.md`

## Accepted scope

Stage 03 demonstrates only the existing Enterprise Context discovery station. It binds an exact registered Intent to a signed OPA permit and a bounded, read-only synthetic Enterprise Graph query, then records immutable PostgreSQL evidence. It does not authorize Existing Systems or any later station.

## Verification record

| Check | Result |
|---|---|
| Signed Stage 03 OPA bundle build and verification | Pass |
| OPA policy compilation | Pass |
| Local OPA, PostgreSQL, and Neo4j containers | Pass; localhost-only and pinned images |
| Synthetic graph fixtures | Pass; two permitted resources plus one out-of-scope negative fixture |
| PostgreSQL schema | Pass; governed Intent and Enterprise Context evidence tables present |
| UI boundary | Pass; discovery requires persisted registration and the exact context permission |
| Scope boundary | Pass; Graph-only, `Internal`, two named resources, maximum two results |
| Advancement boundary | Pass; receipt remains `CanAdvance: false` and names Existing Systems as separately approved |
| Solution build | Pass — all 15 projects, 0 warnings, 0 errors |

## Required boundary assertions

- Registration, policy verification, and OPA scope precede any graph access.
- The retrieval adapter uses a fixed parameterized read query and per-result RBAC/ABAC reauthorization.
- The out-of-scope graph fixture cannot be returned because it is absent from the signed OPA resource scope.
- Policy denial has no scope and no Enterprise Context evidence mutation.
- No Existing Systems, Existing Architecture, AI, workflow advancement, production effect, architectural deviation, or Phase 31 is introduced.

## Next gate

Stage 04 may only be separately approved as a bounded Existing Systems discovery demonstration. It is not authorized by this acceptance.
