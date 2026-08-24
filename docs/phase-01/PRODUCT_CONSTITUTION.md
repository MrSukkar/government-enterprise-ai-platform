# Phase 01 — Product Constitution

Status: **Implemented for approval**  
Authority: `PROJECT MASTER SPECIFICATION v2 — APPROVED`

## Product purpose

Government Enterprise AI Platform is a sovereign enterprise platform that connects the complete institutional lifecycle:

`BUILD <-> UNDERSTAND <-> OPERATE <-> ACT`

The platform exists to let authorized people and governed agents create, understand, operate, and improve government systems using trusted enterprise context, explicit policy, human approval where required, and verifiable evidence.

## Product invariants

The following rules are constitutional and may not be bypassed by an implementation detail:

1. The Enterprise Model is the platform's contextual source of truth.
2. AI is a consumer of governed enterprise context; it is not the source of institutional truth.
3. The AI runtime is not the policy authority.
4. The LLM has no workflow authority.
5. There is no direct `AI -> Production` path.
6. Retrieval is authorized before access and re-authorized before context reaches AI.
7. Evidence is cross-cutting, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable.
8. Automatically discovered or inferred relationships never become confirmed institutional facts without the required governance.
9. Every material action has an identity, purpose, policy decision, approval state, result, and evidence trail.
10. Sovereign and air-gapped operation must not depend on an external API, AI service, SaaS service, or external control plane.
11. The approved architecture is a Modular Monolith; project boundaries must not be converted into distributed services without Change Control.
12. Technology choices marked conditional remain conditional until their approved benchmark or design-validation decision.
13. No numerical performance SLO is accepted before workload benchmarking.
14. The approved 30-phase order is fixed.

## Trust model

- **Enterprise Model:** what the institution knows.
- **Knowledge:** what institutional documents and policies say.
- **Telemetry:** what is happening in operation.
- **Discovery:** what the platform has observed.
- **Evidence:** why a fact or action can be trusted and proven.
- **Governance:** what an identity is permitted to do for a declared purpose.
- **AI:** what may be proposed or executed inside those boundaries.

Enterprise relationships retain their epistemic state: `CONFIRMED`, `DISCOVERED`, `INFERRED`, or `UNKNOWN`.

## Governing execution path

`Intent -> Authorized Enterprise Context -> Approved Architecture -> Approved Packages -> AI Planning -> Generated Change -> Validation -> Security -> Sandbox -> Human Review -> Git -> CI/CD -> Deployment -> Telemetry -> Registration -> Enterprise Model -> Evidence`

No step may silently bypass identity, authorization, policy, required approval, or evidence.

## Sovereignty and security

The platform must support cloud, private cloud, hybrid, on-premises, air-gapped, and sovereign deployment. Cross-cutting foundations include Zero Trust, OIDC/OAuth2, RBAC/ABAC, PKI, workload identity, key and secret management, encryption, separation of duties, audit, provenance, SBOM, artifact signing, and verification.

## Product success condition

The first approved proof of the product is the vertical slice:

`Developer -> Create Internal Service -> Enterprise Context -> Existing Systems -> Existing Architecture -> Approved Packages -> AI Planning -> Code Generation -> Validation -> Security -> Sandbox -> Human Review -> Git -> CI/CD -> Deployment -> OpenTelemetry -> Automatic Registration -> Enterprise Model -> Evidence`

It proves `BUILD -> REGISTER -> OPERATE -> PROVE` before expansion to `UNDERSTAND -> ANALYZE -> DECIDE -> ACT`.

## Change authority

Any change to these constitutional rules requires:

`Change Request -> Impact Analysis -> Architectural Review -> Decision -> Master Specification Update -> Approval`

