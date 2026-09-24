# PROJECT MASTER SPECIFICATION v3 — DRAFT FOR APPROVAL

Status: **Draft — not implementation authority**

Decision basis: `docs/change-control/CR-044-INTEGRATION-PLATFORM-CAPABILITY.md`.

Until this document is explicitly approved, `docs/PROJECT_MASTER_SPECIFICATION_V2.md` remains the sole implementation authority. This draft authorizes no implementation, procurement, institutional pilot, public deployment, Production action, credential, trust material, product selection, or Phase 31.

## 1. Purpose and continuity

V3 extends the Government Enterprise AI Platform with a governed Integration Platform Capability while preserving the completed V2 foundation, its accepted addenda, and its immutable Phase 01–30 history.

The product north star remains:

`BUILD <-> UNDERSTAND <-> OPERATE <-> ACT`

Governance provides `CONTROL`; Evidence provides `PROVE` across the entire cycle.

V3 incorporates the approved V2 constitutional baseline and completed capability contracts by reference. It does not reopen, renumber, or rename Phases 01–30. New delivery work uses **V3 Capability Increments**, never Phase 31.

## 2. Constitutional invariants

1. Backend remains .NET 10 / ASP.NET Core in a Modular Monolith unless a separately approved architectural decision proves a boundary change.
2. Frontend remains Blazor WebAssembly; APIs remain REST with OpenAPI 3.1.
3. The Enterprise Model remains the contextual source of truth.
4. OPA remains policy authority; AI Runtime and LLMs have no policy or workflow authority.
5. There is no direct `AI -> Production` path.
6. Identity, purpose, tenant, classification, RBAC, ABAC, policy, approval, and evidence remain explicit at every material boundary.
7. Retrieval and integration access are authorized before access and re-authorized before release or AI context.
8. Evidence remains append-only, tamper-evident, ordered, traceable, access-controlled, and cryptographically verifiable.
9. Repository defaults remain unconfigured and fail closed.
10. Sovereign and air-gapped profiles have no mandatory external API, SaaS, AI, identity, policy, observability, messaging, registry, evidence, or control-plane dependency.
11. Secrets, credentials, private keys, certificates, tokens, and trust material remain outside source control.
12. Numerical service objectives require approved workload measurements and business-impact analysis; none may be invented.
13. Product selections remain conditional until an approved, evidence-backed decision record passes the applicable hard gates.
14. Human approval and separation of duties remain mandatory where policy classifies an action as material.

## 3. Capability architecture

The Integration Platform Capability consists of six coordinated layers inside the existing governed platform authority.

### 3.1 Consumers & Channels

Provides governed access for internal applications, partner channels, machine clients, events, batches, and explicitly approved external consumers.

Mandatory contracts:

- registered consumer identity, owner, tenant, purpose, classification clearance, environment, and lifecycle;
- versioned channel contract with schema, compatibility, quotas, permitted operations, and evidence requirements;
- no anonymous or implicit institutional access;
- explicit consent and data-purpose constraints where applicable;
- consumer and channel registration in the Enterprise Model.

### 3.2 API Management & Gateway

Provides discovery, publication, versioning, mediation, throttling, routing, and enforcement without becoming an independent identity or policy authority.

Mandatory contracts:

- OpenAPI 3.1 is authoritative for synchronous API shape;
- gateway enforcement uses validated workload identity and exact OPA decisions;
- policy-before-routing and policy-before-response-release are mandatory;
- request, response, and error data are classification-aware and redacted;
- immutable API revisions, deprecation evidence, bounded retries, and idempotency are explicit;
- gateway failure, missing policy, invalid trust, or incomplete configuration fails closed.

### 3.3 Integration & Orchestration

Provides deterministic composition, transformation, routing, long-running coordination, human approval, compensation, and recovery.

Mandatory contracts:

- orchestrations are versioned deterministic definitions with explicit owners and policy scope;
- durable state, checkpoints, resume, retry, timeout, idempotency, compensation, and failure recovery are required;
- transformations are schema-bound, classification-preserving, testable, and evidence-bearing;
- human decisions cannot be supplied or modified by AI, policy, or runtime;
- AI may propose non-executable candidates only through governed contracts;
- no orchestration engine may bypass OPA, authorization, Evidence, or the existing durable-execution boundaries.

### 3.4 Event & Messaging

Provides governed topics, queues, schemas, delivery semantics, replay, dead-letter handling, and isolation.

Mandatory contracts:

- every topic, queue, schema, producer, consumer, and dead-letter route has an Enterprise Model identity and owner;
- tenant, purpose, environment, classification, residency, retention, and permitted producer/consumer scopes are explicit;
- delivery semantics, ordering scope, deduplication, idempotency, replay authorization, and poison-message handling are declared;
- schema compatibility is checked before publication and consumption;
- replay and dead-letter release are material actions requiring policy and evidence;
- missing broker, schema, policy, trust, or evidence configuration fails closed.

### 3.5 Security & Governance

Provides the shared constitutional control plane, not a parallel integration-only authority.

Mandatory controls:

- OIDC/OAuth2 workload and user identity; least privilege; RBAC plus ABAC; Zero Trust;
- signed, versioned, environment-aware OPA policy bundles with pinned verification;
- purpose limitation, classification, residency, retention, consent, tenant isolation, and jurisdiction mappings;
- secrets references only; sovereign key management and HSM integration through deployment configuration;
- signed source, dependencies, SBOMs, provenance, attestations, artifacts, schemas, policies, and deployment profiles;
- separation of duties, explicit human approvals, append-only evidence, and deny-path tests;
- no policy, gateway, broker, workflow, AI, administrator, or operator bypass.

### 3.6 Observability & Operations

Provides OpenTelemetry traces, metrics, and logs; topology; health; capacity; incident; change; continuity; and operational evidence.

Mandatory contracts:

- exact resource identity connects API, message, orchestration, deployment, Enterprise Model, and Evidence records;
- strict redaction occurs before export; authorization controls query and release;
- trace context propagation is bounded and baggage is cleared or allow-listed across trust boundaries;
- collector agent and gateway routing remain sovereign-capable and deployment configured;
- service objectives are benchmark-derived; alerting and capacity thresholds are not invented;
- incident, recovery, replay, failover, restoration, and change actions produce governed evidence.

## 4. Canonical interaction model

Synchronous path:

`Consumer -> Identity -> Gateway -> OPA -> Authorized Route -> Integration/Service -> Result Authorization -> Evidence -> Response`

Asynchronous path:

`Producer -> Identity -> OPA -> Schema/Classification Validation -> Governed Message -> Broker -> Authorized Consumer -> Idempotent Action -> Evidence`

Orchestration path:

`Governed Intent -> Deterministic Definition -> OPA -> Durable Step -> Human Approval/Compensation -> Result Authorization -> Evidence`

No path may reorder authorization after access, treat telemetry as evidence by assertion, or permit AI to invoke a material effect without the deterministic governed path.

## 5. Shared information contracts

Every API, event, message, transformation, and orchestration definition must bind:

- immutable identity and version;
- owner, tenant, purpose, environment, lifecycle, and classification;
- input/output schema and compatibility policy;
- policy bundle identity and decision reference;
- authentication and authorization requirements;
- residency, retention, encryption, and redaction rules;
- idempotency, ordering, retry, timeout, compensation, and replay semantics where applicable;
- OpenTelemetry resource and correlation identifiers;
- Evidence requirements and Enterprise Model registration references.

Contract registries are authoritative metadata stores only when deployment configured. Repository defaults choose no live registry and fail closed.

## 6. Hard gates

No Capability Increment may begin unless its acceptance design passes all applicable gates. A weighted score cannot override a failed hard gate.

### 6.1 Security gate

Requires threat model, trust boundaries, least privilege, RBAC/ABAC plus OPA, tenant isolation, signed supply chain, secrets externalization, deny-path tests, and proof that AI has no authority.

### 6.2 Compliance gate

Requires classification, purpose limitation, consent where applicable, retention, residency, jurisdiction mapping, auditable approvals, immutable evidence, and authorized data release.

### 6.3 Sovereignty gate

Requires a fully operable sovereign/air-gapped profile, local control planes and trust, no mandatory external dependency, deterministic import/export controls, and fail-closed configuration.

### 6.4 HA/DR gate

Requires workload and business-impact evidence, failure-domain design, benchmark-derived capacity, approved RTO/RPO, backup/restore proof, replication semantics, replay/idempotency, failover/failback, and tested recovery. No numeric RTO, RPO, SLO, or capacity target is approved by this specification.

## 7. Deployment profiles

The same governed contracts support cloud, private cloud, hybrid, on-premises, sovereign, and air-gapped profiles. Profiles may differ in adapters and topology, not constitutional controls.

Each deployment profile must explicitly bind local identity, OPA, trust, secrets, gateway, orchestration, messaging, schema, registry, telemetry, storage, Enterprise Model, Evidence, backup, recovery, and operator authorities. An absent binding makes the affected capability unavailable.

## 8. Technology decision policy

V3 approves capability contracts, not named products. A product decision requires an independent record containing:

1. workload and data characteristics;
2. mandatory functional fit;
3. security, compliance, sovereignty, and HA/DR hard-gate evidence;
4. interoperability and exit strategy;
5. lifecycle, support, supply-chain, and air-gap evidence;
6. weighted decision matrix and disqualifying conditions;
7. proof-of-concept results using synthetic local data;
8. owner approval before implementation.

Conditional candidates do not become defaults by appearing in research, examples, or tests.

## 9. Delivery governance

Delivery follows `docs/V3_CAPABILITY_ROADMAP.md` only after V3 approval. Every Capability Increment requires:

- an independent `codex/` branch;
- an approved Change Control record;
- scope and acceptance defined before implementation;
- permitted and denied/fail-closed verification;
- the official repository verifier;
- immutable commit, push, pull request, and green PR CI;
- verification that `origin/main` did not unexpectedly change;
- fast-forward-only merge without force push or history rewrite;
- green final CI on `main` and synchronized project state.

An increment cannot begin before the preceding increment is accepted, merged, and green on `main`.

## 10. Acceptance boundary

V3 becomes implementation authority only after:

1. CR-045 is explicitly approved by the repository owner;
2. all four hard gates are represented in the specification and roadmap;
3. the V3 acceptance artifact is satisfied;
4. the official verifier is green;
5. the approved document is merged fast-forward with green final CI on `main`;
6. `PROJECT_STATUS.md`, `project-os/project-state.json`, and `AGENTS.md` identify V3 as the sole implementation authority.

Until all six conditions are met, this document remains a non-authoritative draft and no Capability Increment may be implemented.
