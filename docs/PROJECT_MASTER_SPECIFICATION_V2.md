# PROJECT MASTER SPECIFICATION v2 — APPROVED

This is the only implementation reference. The approved 30-phase order is fixed.

## Core model

`BUILD <-> UNDERSTAND <-> OPERATE <-> ACT`

- Enterprise Model connects systems and institutional context.
- Governance connects intelligence to permitted action.
- Evidence connects action to trust.
- Software Factory connects AI to the software-delivery lifecycle.

## Approved technology baseline

| Area | Decision |
|---|---|
| Backend | .NET 10 / ASP.NET Core |
| Architecture | Modular Monolith |
| Frontend | Blazor WebAssembly |
| API | REST + OpenAPI 3.1 |
| Primary DB | PostgreSQL |
| Enterprise Graph | Neo4j |
| Retrieval | Abstraction; Neo4j baseline |
| pgvector / Qdrant | Conditional only |
| Knowledge | Controlled GraphRAG |
| Policy | OPA |
| Agent Runtime | Abstraction; Agent Framework candidate |
| Durable Execution | Required |
| Sandbox | Firecracker-class |
| Telemetry | OpenTelemetry |
| Logs / Traces | OpenSearch |
| Metrics | Prometheus |
| Identity | OIDC/OAuth2 + RBAC/ABAC |
| Source | Git |
| CI/CD | DevSecOps |
| Evidence | Cryptographic / tamper-evident |
| Deployment | Air-gapped / on-premises ready |

## Enterprise Model

An Enterprise Object contains Identity, Type, State, Owner, Classification, Relationships, Policies, Actions, Source, Confidence, Evidence, Lifecycle, and Timestamps. Relationships are `CONFIRMED`, `DISCOVERED`, `INFERRED`, or `UNKNOWN`.

## Retrieval authorization

`User/Agent -> Identity -> Purpose -> Policy -> Authorized Scope -> Graph/Vector/Lexical -> Fusion/Reranking -> Authorization Re-check -> AI Context`

Retrieval is never “search everything, filter later.”

## Agentic work and governance

`Enterprise Workflow -> Deterministic Step/Approval/AI Step -> Agent Runtime -> Tool Gateway -> Identity -> OPA -> Action -> Evidence`

The LLM has no workflow authority. AI Runtime is not the Policy Authority. OPA policies are versioned, signed, verified, auditable, and environment-aware. The domain core must not bind directly to a vendor runtime.

Durable execution requires persistent state, checkpoints, resume, retry, timeout, human approval, failure recovery, and idempotency. Agent state is enterprise data governed by classification, retention, encryption, authorization, tenant isolation, evidence, and deletion policy.

## Software Factory

`Intent -> Enterprise Context -> Existing Architecture -> Approved Packages -> AI Planning -> Code Generation -> Static Validation -> Security Validation -> Sandbox -> Tests -> Human Review -> Git -> CI/CD -> Artifact -> Deployment -> Registration -> Observability -> Evidence`

There is no direct `AI -> Production` path. The sandbox is ephemeral Firecracker-class isolation with no production credentials, restricted networking, isolated filesystems, and CPU, memory, and time limits. WASM is specialized, not the general .NET sandbox.

## Observability and evidence

`Applications -> OpenTelemetry SDK -> Collector Agent -> Trace-aware Routing -> Collector Gateway -> Processing -> Logs/Metrics/Traces -> Storage -> Enterprise Model`

No numerical SLO is approved before workload benchmarking.

Evidence is append-only, tamper-evident, cryptographically verifiable, signed, ordered, traceable, and access-controlled:

`Request -> Context -> Knowledge -> Decision -> Policy -> Approval -> Action -> Result -> Telemetry -> Evidence`

## Security, supply chain, and sovereignty

Cross-cutting foundations are Identity, RBAC, ABAC, Zero Trust, PKI, HSM/key management, secrets management, workload identity, encryption, separation of duties, audit, and evidence.

`Source -> Provenance -> SBOM -> Dependency Validation -> Build -> Build Attestation -> Artifact Signing -> Registry -> Verification -> Deployment`

This applies to containers, NuGet, AI models, policies, frontend dependencies, and sandbox images.

The platform supports cloud, private cloud, hybrid, on-premises, air-gapped, and sovereign operation. Air-gapped environments use local model runtime, registry, policy, identity, evidence, and observability, with no external API, AI, SaaS, or control-plane dependency.

## Phase capability mapping

| Phase | Final role |
|---|---|
| 01 | Product Constitution |
| 02 | UX & Roles |
| 03 | Backend Foundation |
| 04 | Frontend Foundation |
| 05 | Identity & Access + PKI + separation of duties |
| 06 | OpenAPI contract + platform boundaries |
| 07 | Enterprise Model base |
| 08 | Knowledge + GraphRAG + retrieval authorization |
| 09 | Institutional package registry |
| 10 | Software Factory governance + supply chain |
| 11 | AI development + agent runtime + AI evaluation |
| 12 | Validation + security sandbox |
| 13 | Git + CI + provenance + SBOM + signing |
| 14 | Sovereign deployment |
| 15 | OpenTelemetry + redaction |
| 16 | Central observability |
| 17 | Automatic registration |
| 18 | Understanding engine |
| 19 | Agentic work + durable execution + agent state |
| 20 | Governance + OPA + actions + MCP |
| 21 | Enterprise modeling + impact analysis |
| 22 | Simulation + resilience + disaster recovery |
| 23 | Proactive intelligence |
| 24 | Government productization + compliance |
| 25 | Front Door |
| 26 | Developer Experience |
| 27 | Closed Loop |
| 28 | Final System Architecture |
| 29 | Vertical Slice |
| 30 | Evidence Engine + cryptographic proof |

## Approved vertical slice

`Developer -> Create Internal Service -> Enterprise Context -> Existing Systems -> Existing Architecture -> Approved Packages -> AI Planning -> Code Generation -> Validation -> Security -> Sandbox -> Human Review -> Git -> CI/CD -> Deployment -> OpenTelemetry -> Automatic Registration -> Enterprise Model -> Evidence`

This proves `BUILD -> REGISTER -> OPERATE -> PROVE`, then expands to `UNDERSTAND -> ANALYZE -> DECIDE -> ACT`.

## Change control

`Change Request -> Impact Analysis -> Architectural Review -> Decision -> Master Specification Update -> Approval`

No architectural deviation is permitted outside this process.
