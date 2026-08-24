# ADR-001 — Platform Architectural Boundaries

Status: **Accepted by PROJECT MASTER SPECIFICATION v2**  
Phase: **01 — Product Constitution**

## Context

The platform combines software delivery, enterprise context, knowledge, observability, governed agentic work, and evidence. Without fixed boundaries, an implementation could incorrectly give AI policy authority, treat inferred data as fact, create an unsafe path to production, or introduce unapproved distributed architecture.

## Decision

1. Use `.NET 10 / ASP.NET Core` with a **Modular Monolith** backend.
2. Use **Blazor WebAssembly** for the frontend and **REST + OpenAPI 3.1** for the API boundary.
3. Keep the domain core independent of vendor-specific AI and agent runtimes.
4. Treat the Enterprise Model as the contextual source of truth connecting `BUILD`, `UNDERSTAND`, `OPERATE`, and `ACT`.
5. Keep AI orchestration, policy authority, action execution, and evidence as separate responsibilities.
6. Use OPA as the approved policy boundary, with versioned, signed, verified, auditable, and environment-aware policies.
7. Require durable execution capabilities without selecting an implementation product in this phase.
8. Use PostgreSQL as the primary relational store and Neo4j as the Enterprise Graph baseline.
9. Keep retrieval behind an abstraction. `pgvector` and Qdrant remain conditional and may not become mandatory without approved validation.
10. Use OpenTelemetry with agent/gateway collection, OpenSearch for logs/traces, and Prometheus for metrics.
11. Require Firecracker-class isolation for the general software-factory sandbox; WASM remains specialized rather than a general replacement.
12. Require cryptographic, tamper-evident evidence and sovereign/air-gapped readiness across platform capabilities.

## Consequences

- Projects in the Solution are module boundaries, not independently deployable microservices.
- No module may bypass governance to execute a material action.
- No AI-generated change may reach production without validation, security controls, required human review, Git, and CI/CD.
- No retrieval implementation may search unrestricted data and filter only after retrieval.
- No vendor SDK may become a domain dependency.
- No conditional technology or numerical SLO is promoted by assumption.

## Rejected alternatives

- A direct `AI -> Production` workflow.
- Treating the LLM or agent runtime as policy authority.
- Mandatory PostgreSQL + pgvector + Neo4j + Qdrant from the start.
- Microservices as the initial platform architecture.
- Evidence implemented as an ordinary mutable audit log.
- External SaaS dependencies required for sovereign or air-gapped operation.

## Compliance

Deviations require the approved Change Control path and an update to the Master Specification before implementation.

