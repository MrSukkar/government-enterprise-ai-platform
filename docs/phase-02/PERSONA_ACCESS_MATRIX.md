# Phase 02 — Persona Access Matrix

Status: **Approved design boundary; enforcement deferred to Phase 05**

Legend: `Primary` is the persona's default experience, `Scoped` requires explicit authorization and purpose, and `None` is not part of the persona experience by default.

| Persona | Enterprise context | Delivery | Operations | Policy / approval | Evidence |
|---|---:|---:|---:|---:|---:|
| Executive | Primary | Scoped | Summary | Decision summary | Summary |
| Enterprise Architect | Primary | Scoped | Scoped | Scoped | Scoped |
| IT Manager | Primary | Primary | Primary | Scoped | Scoped |
| Developer | Scoped | Primary | Scoped | Request only | Own activity |
| Operations Engineer | Scoped | Scoped | Primary | Request only | Operational scope |
| Security | Scoped | Scoped | Scoped | Security decision | Security scope |
| Governance | Scoped | None | Scoped | Primary | Primary |
| System Owner | Owned systems | Scoped | Owned systems | Owned-system decisions | Owned systems |
| Service Owner | Owned services | Scoped | Owned services | Owned-service decisions | Owned services |
| Approver | Decision context | None | Impact context | Primary | Decision evidence |
| Auditor | Read-only scope | Read-only scope | Read-only scope | Read-only scope | Primary |

## Non-authorizing nature

This matrix defines intended experiences; it is not an access-control list. Phase 05 must translate approved identity, RBAC, ABAC, purpose, classification, tenant, separation-of-duties, and policy requirements into enforceable authorization.

