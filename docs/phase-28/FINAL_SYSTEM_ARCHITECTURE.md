# Phase 28 — Final System Architecture

## Authority

The final architecture is a consolidation of `PROJECT MASTER SPECIFICATION v2 — APPROVED`; it does not replace or amend it. The machine-readable companion is `architecture/system-architecture.v2.json`. Any deviation requires the approved Change Control path.

## System shape

The platform is a .NET 10 ASP.NET Core modular monolith with a Blazor WebAssembly Front Door. Fifteen projects preserve explicit boundaries for Domain, Application, API, Identity, Governance, Knowledge, Enterprise Model, Software Factory, Agentic Work, Observability, Integrations, Modeling, Infrastructure, Evidence, and Web.

The core operating model remains:

`BUILD <-> UNDERSTAND <-> OPERATE <-> ACT`

- Enterprise Model connects systems and institutional context.
- Governance connects intelligence to permitted action.
- Evidence connects action to trust.
- Software Factory connects AI to the software-delivery lifecycle.

## Trust boundaries

All external providers, repositories, runtimes, stores, analyzers, registries, and tool clients are treated as boundary interfaces whose returned state is revalidated. Identity, tenant, purpose, classification, explicit scope, signed/versioned policy, separation of duties, evidence, and time are carried through the relevant workflows.

The LLM has no workflow authority. AI output cannot create an executable command. Agent Runtime is not Policy Authority. An effect can occur only through Identity, verified OPA policy, approval, action evidence, and the governed MCP executor. There is no direct `AI -> Production` path.

## Data and platform boundaries

PostgreSQL remains the primary database decision; Neo4j is the Enterprise Graph and retrieval baseline. Retrieval stays abstract and authorized before access and again before AI context. pgvector and Qdrant remain conditional only.

OpenTelemetry feeds sovereign collector layers; OpenSearch stores logs/traces and Prometheus stores metrics. Numerical SLOs remain prohibited until workload benchmarking.

Evidence remains cross-cutting through all phases, while the complete cryptographic proof implementation is intentionally reserved for Phase 30.

## Deployment and supply chain

The platform supports cloud, private cloud, hybrid, on-premises, air-gapped, and sovereign operation. Air-gapped deployments require local identity, policy, model runtime, registry, evidence, and observability without external API, AI, SaaS, licensing, telemetry, or control-plane dependency.

Source, packages, policies, models, frontend dependencies, containers, and sandbox images remain governed by provenance, SBOM, dependency validation, attestation, signing, registry, and pre-deployment verification.

## Remaining delivery gates

Architecture is finalized, but platform delivery is not yet complete. Phase 29 must prove the approved vertical slice, and Phase 30 must implement the final cryptographic Evidence Engine.
