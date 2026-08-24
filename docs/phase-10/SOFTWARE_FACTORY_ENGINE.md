# Phase 10 — Software Factory Engine

Status: **Implemented**  
Depends on: **Phase 09 — Institutional Package Registry**

## Deterministic delivery workflow

The engine enforces the approved sequence without allowing a caller or AI runtime to select the next stage:

`Intent -> Enterprise Context -> Existing Architecture -> Approved Packages -> AI Planning -> Code Generation -> Static Validation -> Security Validation -> Sandbox -> Tests -> Human Review -> Git -> CI/CD -> Artifact -> Deployment -> Registration -> Observability -> Evidence`

Only the exact next stage may be recorded. A rejected or failed stage does not advance the workflow, timestamps cannot move backward, and a completed run cannot be reopened through the transition engine.

## Mandatory controls

- Every completed stage carries an actor, timestamp, result, and evidence reference.
- Every declared package decision must be allowed at the Approved Packages stage.
- Human review must be performed by a subject other than the run initiator.
- Deployment is unreachable until validation, security, sandbox, tests, independent review, Git, CI/CD, and artifact stages have passed.
- Registration, observability, and evidence remain required after deployment.
- AI Planning and Code Generation are workflow stages, not workflow authorities.

## Persistence boundary

`ISoftwareDeliveryRunRepository` is asynchronous and tenant-scoped. No database, queue, Git provider, CI provider, AI runtime, sandbox runtime, or deployment target is selected in this phase.

