# Environment Promotion Plan

Status: **Reference plan; institutional selections remain pending**

## Promotion principle

Promotion is evidence-based and one-way through controlled gates:

`Local Preview -> Integration Demo -> Non-Production Pilot -> Production`

Configuration is environment-specific. Source, policy identifiers, manifests, and contracts may be versioned; credentials, private keys, tokens, connection strings, certificates containing private material, and organization-specific secrets must stay in the approved secrets or key-management system.

## Environment gates

| Environment | Purpose | Permitted data/effects | Entry gate | Exit evidence |
|---|---|---|---|---|
| Local Preview | UI, build, OpenAPI, and fail-closed learning | Synthetic data; no external effect | Verified source checkout | Project verification, liveness, expected readiness denial |
| Integration Demo | End-to-end integration rehearsal | Synthetic data and isolated institutional test systems | Approved demo tenant, test identities, trust material, and isolated dependencies | Successful and denied-path receipts, logs, traces, and evidence-chain verification |
| Non-Production Pilot | Validate real operating model with selected users | Approved masked/synthetic or formally authorized non-production data | Security, privacy, architecture, operations, and business-owner approval | User acceptance, control evidence, rollback rehearsal, benchmark report, residual-risk decision |
| Production | Operate the approved service | Explicitly authorized production data and effects | Formal production authorization and change window | Continuous operational evidence, auditability, incident and rollback capability |

## Dependency connection order

### Gate A — Trust and contextual foundations

Connect first:

- sovereign OIDC/OAuth2 identity and claim mapping;
- RBAC/ABAC roles, tenant, purpose, classification, and separation of duties;
- signed policy-bundle verification and OPA;
- PostgreSQL append-only repositories;
- Neo4j Enterprise Graph read model;
- sovereign signing and pinned verification trust.

Do not proceed until authentication, authorization, tenant isolation, classification denial, signature failure, and unavailable-dependency behavior are tested.

### Gate B — Governed build isolation

Connect after Gate A:

- institutional package catalog and supply-chain verifiers;
- separately governed AI planning and code-generation runtimes;
- independent AI-output evaluators;
- signed static and security control profiles;
- approved Firecracker-class sandbox and test runtime;
- immutable governed test-manifest catalog;
- human attestation verifier.

AI endpoints receive only re-authorized context and have no tools, workflow authority, production credentials, or production route.

### Gate C — Source and supply chain

Connect after Gate B:

- institutional Git gateway and commit signing;
- immutable CI/CD workflow catalog and isolated runners;
- SBOM, provenance, attestation, and signature verifiers;
- immutable artifact registry and publication gateway.

Prove exact source-to-artifact traceability and reject mutable or mismatched inputs.

### Gate D — Runtime and institutional truth

Connect last:

- sovereign deployment profiles and gateway;
- rollback, workload identity, secrets, topology, and capacity policies;
- trusted OpenTelemetry collectors, routing, storage, and redaction;
- automatic registration manifest and repository;
- authorized Enterprise Model read path;
- final append-only evidence store, signer, verifier, and independent release authorization.

Production effects remain disabled during Integration Demo. Non-Production Pilot effects must target only the explicitly approved isolated environment.

## Decisions required from the participating entity

- Identity authority, token audience, claims, roles, and separation-of-duties owners.
- Tenant structure, purposes, classifications, data residency, retention, and deletion policy.
- OPA ownership, policy signing, policy review, and emergency revocation process.
- PostgreSQL, Neo4j, secrets manager, PKI/HSM, and trust-anchor ownership.
- Approved package, model, sandbox-image, runner, registry, and deployment providers.
- Human approvers, security reviewers, auditors, incident owners, and service owner.
- Telemetry destinations, redaction rules, access, retention, and incident correlation.
- Pilot dataset, lawful basis, privacy assessment, and prohibited data fields.
- Production change, rollback, continuity, disaster recovery, and support procedures.

These are institutional decisions. Repository defaults must not choose them.

## Promotion rules

1. Promote the same immutable source and verified artifact; do not rebuild differently for Production.
2. Re-evaluate policy and trust independently in every environment.
3. Use separate tenant data, identities, keys, certificates, stores, and endpoints per environment.
4. Exercise denial, timeout, signature failure, dependency loss, idempotency, concurrency, and rollback paths before promotion.
5. Establish performance and reliability targets only after representative Pilot benchmarking; do not invent numerical SLOs.
6. Record every approval, exception, deployment, rollback, and verification as evidence.
7. Stop promotion when any prerequisite is absent, expired, mismatched, unsigned, or unverifiable.

