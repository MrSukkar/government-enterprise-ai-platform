# CR-044 — Integration Platform Capability

Status: **Proposed — architectural decision required; implementation not authorized**

Authority baseline: `docs/PROJECT_MASTER_SPECIFICATION_V2.md` and the completed fixed 30-phase roadmap.

This request does not create Phase 31, change the approved architecture, select products, or authorize implementation, procurement, deployment, pilot, or Production use.

## 1. Requested decision

Decide whether the proposed Integration Platform Capability should be governed as:

1. a new approved **Project Master Specification V3**, replacing V2 as implementation authority while preserving all constitutional invariants; or
2. a **separate governed program after Phase 30**, with its own constitution, roadmap, funding, ownership, architecture authority, and acceptance gates.

## 2. Capability scope

The capability would cover six coordinated layers:

1. **Consumers & Channels** — governed internal, partner, machine, event, batch, and approved external consumption contracts.
2. **API Management & Gateway** — lifecycle, discovery, versioning, throttling, mediation, developer access, and policy enforcement without bypassing identity or OPA.
3. **Integration & Orchestration** — deterministic service composition, transformation, routing, long-running coordination, human approvals, and compensation; no AI workflow authority.
4. **Event & Messaging** — governed topics, queues, schemas, delivery semantics, replay, dead-letter handling, and tenant/classification isolation.
5. **Security & Governance** — identity, RBAC/ABAC, OPA, consent, data classification, sovereignty, supply chain, secrets references, audit, and append-only evidence.
6. **Observability & Operations** — OpenTelemetry, redaction, topology, health, capacity, incident, change, continuity, evidence, and benchmark-derived service objectives.

Excluded until separately approved: named vendor/product selection, public or Production deployment, credentials or trust material, migration execution, numerical SLOs without benchmarks, mandatory external control planes, architectural deviation, and any direct `AI -> Production` path.

## 3. Hard gates

An option is ineligible if any gate is not satisfied in its approved design and acceptance plan.

| Hard gate | Mandatory evidence |
|---|---|
| Security | Zero-trust identity, least privilege, RBAC/ABAC plus OPA, signed artifacts, secrets outside source, tenant isolation, deny-path tests, and no AI authority. |
| Compliance | Data classification, purpose limitation, retention and residency controls, auditable approvals, immutable evidence, and jurisdiction-specific control mapping. |
| Sovereignty | Fully operable sovereign/air-gapped profile with no mandatory external control plane, locally controlled trust and keys, and fail-closed adapters. |
| HA/DR | Approved workload model, failure domains, RTO/RPO derived from business impact, backup/restore proof, replay/idempotency, regional continuity, and tested recovery. |

Both governance options can pass these gates only conditionally through a future approved specification and evidence-backed acceptance. Neither option passes by assertion, and neither is authorized to implement now.

## 4. Weighted decision matrix

Score scale: 1 poor, 3 adequate, 5 strong. Weighted score = weight × score; maximum 500. Hard gates override the score.

| Criterion | Weight | Master Specification V3 | Weighted | Separate program after Phase 30 | Weighted |
|---|---:|---:|---:|---:|---:|
| Security and policy coherence | 25 | 5 | 125 | 4 | 100 |
| Compliance and evidence continuity | 20 | 5 | 100 | 4 | 80 |
| Sovereign and air-gapped coherence | 20 | 5 | 100 | 4 | 80 |
| HA/DR governance readiness | 15 | 4 | 60 | 4 | 60 |
| Alignment with the unified platform cycle | 10 | 5 | 50 | 3 | 30 |
| Delivery isolation and change containment | 10 | 3 | 30 | 5 | 50 |
| **Total** | **100** |  | **465 / 500 (93%)** |  | **400 / 500 (80%)** |

## 5. Analysis

Master Specification V3 provides one authority for contracts that cross identity, API, orchestration, messaging, Enterprise Model, observability, governance, and Evidence. It best prevents duplicated policy planes, inconsistent evidence chains, and parallel architectural truth. Its principal cost is a larger formal specification change and stronger regression burden across the completed platform.

A separate program provides stronger schedule, funding, and delivery isolation. Its principal risk is creation of a second architectural and governance center across boundaries that must remain unified, especially identity, OPA, classification, sovereignty, OpenTelemetry, Enterprise Model context, and cryptographic evidence.

## 6. Recommendation

**Recommend Project Master Specification V3**, subject to explicit repository-owner approval and satisfaction of all four hard gates in the proposed V3 architecture and acceptance model.

V3 should define a new governed roadmap without renaming the capability as Phase 31 and without altering the historical completion of Phases 01–30. It should retain V2 constitutional invariants, incorporate the six layers as first-class platform capabilities, and define product-neutral decision records before any technology selection.

If organizational ownership, funding, or regulatory accountability cannot be unified under the platform authority, the fallback is a separate governed program after Phase 30 with binding interface, identity, policy, sovereignty, observability, and evidence contracts back to the platform.

## 7. Decision boundary

Required owner decision: **Approve Master Specification V3**, **direct a separate governed program after Phase 30**, or **reject/defer the capability**.

Until that decision is recorded, no implementation, product selection, architectural change, new roadmap item, public deployment, Production action, or use of the name Phase 31 is authorized.
