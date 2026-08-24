# Phase 02 — UX & Roles

Status: **Implemented for approval**  
Depends on: **Phase 01 — Product Constitution**

## Governing UX rule

> Technical complexity belongs behind the API.

Every person uses the same governed platform, but does not use the same screen or receive the same operational scope. Experiences expose outcomes, decisions, responsibilities, and permitted actions—not internal platform complexity.

## Approved persona model

| Persona | Primary experience | Outcomes in scope |
|---|---|---|
| Executive | Enterprise overview | Enterprise health, critical services, risks, transformation, incidents, decisions |
| Enterprise Architect | Architecture and enterprise model | Systems, dependencies, architecture, technology, data flows, standards, scenarios |
| IT Manager | Technology portfolio and delivery oversight | Service portfolio, operational posture, delivery status, ownership, risks, escalations |
| Developer | Governed software delivery | Projects, code, AI workspace, approved packages, architecture, tests, pull requests, deployments |
| Operations Engineer | Service operations | Services, logs, metrics, traces, incidents, alerts, deployments |
| Security | Security posture and control enforcement | Security findings, identities, access decisions, secrets exposure, supply-chain controls, sandbox results |
| Governance | Policy and compliance | Policies, violations, AI usage, control status, evidence, audit visibility |
| System Owner | Owned system accountability | System health, dependencies, lifecycle, risks, requests, changes, approvals affecting the system |
| Service Owner | Owned service accountability | Service health, consumers, dependencies, incidents, requests, impact, changes |
| Approver | Governed decision queue | Requested action, purpose, risk, policy result, impact, evidence, approve or reject decision |
| Auditor | Independent verification | Who, what, when, why, policy, approval, result, and evidence |

## Experience boundaries

1. A persona is an experience model, not an authorization grant.
2. Identity, RBAC, ABAC, purpose, classification, policy, tenant, and resource scope determine access.
3. Navigation visibility never replaces server-side authorization.
4. The same person may hold multiple roles, but each action remains attributable to the active governed context.
5. Separation of duties prevents one context from silently combining creator, reviewer, approver, executor, and auditor authority.
6. Technical telemetry detail is available only to authorized technical roles.
7. Executive and owner experiences show operational meaning before implementation detail.
8. Auditor access is read-oriented and evidence-centered; it does not imply operational execution authority.
9. Approvals must show purpose, impact, risk, policy decision, and evidence before a decision is made.
10. UI filtering is never used to hide data that the API already disclosed.

## Shared experience model

All experiences use a consistent hierarchy:

`Intent -> Authorized Scope -> Outcome -> Explanation -> Evidence -> Permitted Action`

The platform may progressively reveal deeper technical detail only when the active identity and purpose authorize it.

## Deferred implementation

This phase defines personas and experience boundaries only. It does not implement:

- Frontend framework foundations, which belong to Phase 04.
- Identity, RBAC, or ABAC, which belong to Phase 05.
- The intent-based Front Door, which belongs to Phase 25.
- Persona dashboards or operational features from later phases.

