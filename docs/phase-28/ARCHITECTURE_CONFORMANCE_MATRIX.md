# Phase 28 — Architecture Conformance Matrix

| Approved boundary | Implemented location | Conformance |
|---|---|---|
| .NET 10 modular monolith | Solution and 14 backend projects | Conformant |
| Blazor WebAssembly | `frontend/Platform.Web` | Conformant |
| REST + OpenAPI 3.1 | `Platform.Api` approved embedded contract | Conformant |
| OIDC/OAuth2 + RBAC/ABAC + PKI | `Platform.Identity` | Conformant foundation |
| Enterprise Model source of context | `Platform.EnterpriseModel` | Conformant |
| Controlled authorized GraphRAG | `Platform.Knowledge` | Conformant boundary |
| Approved package and Software Factory governance | `Platform.SoftwareFactory` | Conformant |
| Vendor-neutral durable agent work | `Platform.AgenticWork` | Conformant |
| OPA + governed actions + MCP | `Platform.Governance` | Conformant boundary |
| Firecracker-class sandbox policy | `Platform.SoftwareFactory/Sandbox` | Conformant runtime abstraction |
| OpenTelemetry + redaction | `Platform.Observability` | Conformant |
| OpenSearch + Prometheus central experience | `Platform.Observability/Central` | Conformant boundary |
| Automatic registration and closed loop | Enterprise Model + Software Factory | Conformant |
| Impact analysis and isolated digital twin | `Platform.Modeling` | Conformant |
| Air-gapped sovereign productization | `Platform.Infrastructure` | Conformant |
| Evidence cross-cutting | Evidence references and journals across modules | Conformant foundation; final engine Phase 30 |
| No direct AI to production | Software Factory, Agentic Work, Governance guards | Enforced |
| Conditional vector stores remain conditional | No mandatory pgvector/Qdrant binding | Enforced |
| No pre-benchmark numerical SLO | Observability, simulation, intelligence docs/contracts | Enforced |

No architectural deviation or new mandatory technology was introduced during finalization.
