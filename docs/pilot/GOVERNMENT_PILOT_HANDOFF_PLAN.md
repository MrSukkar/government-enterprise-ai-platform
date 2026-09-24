# Government Pilot Handoff Plan

Status: **Planned — not yet authorized for institutional execution**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V3.md`.

This document fixes the post-V3 delivery path. It does not authorize an institutional pilot, Production access, public deployment, credentials, trust material, real government data, or a named product selection.

## 1. Fixed delivery path

1. Complete V3-04 through V3-12 sequentially with independent Change Control, acceptance, verification, PR, green CI, fast-forward merge, and final green `main` CI.
2. Close V3 with its independent final acceptance.
3. Create and approve a separate Government Pilot Change Request for the named government entity, systems, data classes, deployment profile, responsibilities, and test boundaries.
4. Produce a Pilot Readiness Acceptance covering security, compliance, sovereignty, HA/DR, installation, connectivity, rollback, evidence, support, and exit.
5. Deploy into an isolated, non-Production environment controlled by the government entity.
6. Begin with synthetic data, then approved masked or non-sensitive data and read-only integrations.
7. Permit bounded non-Production write tests only through separately approved policies and human approval. No direct AI-to-Production path exists.
8. Close the pilot with an evidence-backed evaluation. Production readiness, if requested, is a later independent authorization.

## 2. How the entity receives the platform

The entity must not install an arbitrary snapshot of `main` as the authoritative delivery mechanism.

The primary handoff is an immutable, signed release identified by an approved version, Git tag, and commit. It is imported through the entity's controlled software-supply-chain process and contains:

- signed OCI container images or equivalent immutable deployment artifacts copied into the entity's internal registry;
- deployment-profile manifests for the approved non-Production topology;
- OpenAPI 3.1 contracts plus event, message, schema, and orchestration contracts applicable to the release;
- SHA-256 checksums, SBOMs, provenance, signatures, attestations, dependency inventory, and verification instructions;
- database migrations, configuration schema, compatibility rules, and rollback instructions;
- installation, upgrade, backup, restore, failover, failback, incident, and support runbooks;
- threat model, trust boundaries, required ports, data flows, hardening baseline, and deny-path tests;
- synthetic test data, acceptance scenarios, expected evidence, and an evidence-verification tool or procedure;
- release notes, known limitations, support contacts, issue process, and exit/uninstall procedure.

Read-only source access may be granted separately for audit, code review, or approved extension work. It must be pinned to the delivered tag and commit. Git access is optional and is not a substitute for signed release artifacts. Forking, building, or deploying a different commit creates a different unaccepted release.

No secret, credential, private key, certificate, token, trust anchor, institutional dataset, or runtime state is delivered through Git.

## 3. Installation and responsibility split

| Area | Platform team supplies | Government entity supplies |
|---|---|---|
| Release | Signed artifacts, SBOM, provenance, hashes, manifests | Internal registry/repository and import approval |
| Compute | Resource and topology requirements | Approved isolated non-Production compute platform |
| Identity | OIDC/OAuth2 contract, claims and audience requirements | Tenant, issuer, client registrations, users/groups, MFA policy |
| Policy | OPA input/output contracts, bundle verification contract | Approved policies, owners, signed bundles, local policy service |
| Trust | Certificate and trust-reference requirements | Entity PKI certificates, CAs, HSM/KMS bindings and rotation |
| Secrets | Secret names and external references only | Values in the entity's approved secrets manager |
| Connectivity | Required flows, timeouts, schemas and health checks | DNS, firewall rules, proxies, endpoints and network approvals |
| Data | Classification, residency, retention and redaction contracts | Approved synthetic/masked datasets and data-owner approval |
| Systems | Connector contracts and test harnesses | Non-Production endpoints, owners, schemas and service accounts |
| Operations | Telemetry schema, dashboards/runbook definitions | Local collectors, SIEM/monitoring integration and operators |
| Continuity | Backup/restore/failover procedures and tests | Approved RTO/RPO from business analysis and recovery environment |
| Evidence | Append/verify/export contracts | Local evidence store, sovereign signing, reviewers and retention |

Missing identity, policy, trust, secrets, evidence, registry, storage, or connector binding makes the affected capability unavailable and fail closed.

## 4. Pilot connection dossier required from the entity

Before any connector is enabled, the approved Pilot Change Request must identify:

- system owner, technical owner, security owner, data owner, and pilot approver;
- non-Production system name, purpose, tenant, environment, classification, residency, retention, and jurisdiction;
- interface type: REST/OpenAPI, event, queue/topic, batch/file, database view, or other approved contract;
- endpoint/DNS, network zone, protocol, TLS requirements, certificate authority, and allow-listed flows;
- authentication method, token issuer, audience, claims, scopes/permissions, service identity, and rotation process;
- authoritative OpenAPI/schema version, compatibility policy, sample payloads, error model, rate limits, timeouts, and idempotency key;
- read/write operations, prohibited operations, approval boundary, rollback/compensation behavior, and test window;
- data minimization, masking, redaction, consent where applicable, and evidence requirements;
- expected load and business-impact evidence; RTO, RPO, SLO, and capacity values are accepted only when measured and approved;
- support contacts, incident escalation, change window, rollback trigger, and exit criteria.

## 5. Current API inventory

The current authoritative contract is:

- Source: `backend/Platform.Api/Contracts/openapi.v1.json`
- Runtime: `GET https://<api-host>/openapi/v1.json`
- Developer console: `GET https://<api-host>/developers`
- Format: OpenAPI **3.1.0**
- Security scheme: `oidcBearer` using JWT bearer tokens
- Current operations: **26** total — **5 GET**, **21 POST**
- Public contract endpoints: health, readiness, foundation metadata, and OpenAPI document
- Protected operations: identity context and all governed workflow mutations

| Group | Operation IDs | Access |
|---|---|---|
| Platform health | `getPlatformHealth`, `getPlatformReadiness` | Public; readiness discloses configuration state, not secrets |
| Identity | `getGovernedExperienceContext` | Bearer |
| Foundation | `getInternalServiceFoundation` | Public metadata |
| Intent | `submitInternalServiceIntent`, `registerInternalServiceIntent` | Bearer |
| Enterprise discovery | `discoverInternalServiceEnterpriseContext`, `discoverInternalServiceExistingSystems`, `discoverInternalServiceExistingArchitecture` | Bearer |
| Package and AI candidates | `selectInternalServiceApprovedPackages`, `createGovernedAiPlanningCandidate`, `createGovernedCodeGenerationCandidate` | Bearer |
| Validation and execution | `runGovernedStaticValidation`, `runGovernedSecurityValidation`, `runGovernedSandbox`, `runGovernedTests` | Bearer |
| Human and Git gates | `recordGovernedHumanReview`, `createGovernedGitCommit` | Bearer |
| Delivery | `executeGovernedCiCd`, `publishGovernedArtifact`, `executeGovernedSovereignDeployment` | Bearer |
| Operations and registration | `activateGovernedOpenTelemetry`, `executeGovernedAutomaticRegistration`, `contextualizeGovernedEnterpriseModel` | Bearer |
| Evidence | `completeGovernedEvidence` | Bearer |
| Contract discovery | `getOpenApiContract` | Public |

The full paths, request/response schemas, status codes, and security declarations are defined only by the OpenAPI document. Consumers must generate or validate clients against the exact contract digest shipped with the accepted release, not a mutable URL or handwritten copy.

### Export and verification

From source, copy the accepted release's `backend/Platform.Api/Contracts/openapi.v1.json` and verify its checksum against the release manifest. From an approved running instance, download `/openapi/v1.json` over trusted TLS and compare its SHA-256 digest with the same manifest. A mismatch blocks testing.

The current 26 operations represent the accepted Create Internal Service demonstration. V3-01 through V3-03 currently add product-neutral integration contracts and validation libraries; they do not yet expose live institutional integration endpoints. The final pilot API/event catalog is frozen only after V3-12 acceptance and may extend this inventory through the remaining V3 gates.

## 6. Reference localhost topology — not a pilot deployment profile

The completed synthetic demo currently uses frontend `7071`, API `7200`, identity `8443`, OPA `8181`, PostgreSQL `5433`, and Neo4j `7687` on localhost. These ports and products are evidence for the local demonstration only. They are not an approved government topology or product decision.

The entity-specific Pilot Change Request must select and bind the actual sovereign deployment profile using the V3 hard gates and approved technology-decision process.

## 7. Pilot readiness exit criteria

The entity may begin its evaluation only when all of the following are accepted:

- V3-12 and V3 final acceptance are complete and green on `main`;
- the entity-specific Pilot Change Request and Pilot Readiness Acceptance are approved;
- the delivered tag, commit, artifact digests, signatures, SBOM, provenance, and OpenAPI/schema digests verify;
- installation, upgrade, rollback, backup, restore, and evidence verification pass in the isolated environment;
- identity, OPA, trust, secrets, storage, telemetry, and evidence bindings are local and healthy;
- every connector has an approved owner, scope, classification, policy, test data, and deny-path result;
- no Production endpoint, uncontrolled external dependency, real secret in source, or AI authority is present;
- the entity and platform team sign the test plan, responsibility matrix, support model, and exit criteria.

Passing these criteria means **ready for a bounded government non-Production pilot**. It does not mean Production authorization.
