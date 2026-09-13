# National Permit Renewal — Governed Demonstration Scenario

Status: **Reference demonstration scenario**

## 1. Demonstration objective

Show how a government entity can turn an approved service intent into governed software while preserving policy authority, separation of duties, supply-chain trust, sovereign execution, operational registration, and cryptographically verifiable evidence.

The scenario demonstrates the platform's approved path:

`Intent -> Enterprise Context -> Existing Systems -> Existing Architecture -> Approved Packages -> AI Planning -> Code Generation -> Static Validation -> Security Validation -> Sandbox -> Tests -> Human Review -> Git -> CI/CD -> Artifact -> Deployment -> OpenTelemetry -> Automatic Registration -> Enterprise Model -> Evidence`

## 2. Business story

A permit authority wants an internal service that helps permit officers process renewal requests with less manual effort. The service may summarize an application and present verified eligibility context, but it may not approve, reject, or publish a renewal. A human permit officer remains accountable for the administrative decision.

### Intended outcome

Reduce renewal handling effort while preserving eligibility rules, policy checks, classification controls, auditability, and human decision authority.

### Deliberate exclusions

- No real citizen, employee, permit, payment, or identity data.
- No autonomous permit approval or rejection.
- No production credential or production endpoint.
- No model training on institutional information.
- No numerical service-level objective before Pilot workload benchmarking.

## 3. Synthetic demonstration data

| Field | Demonstration value |
|---|---|
| Service name | National Permit Renewal |
| Tenant | `demo-permit-authority` |
| Purpose | `internal-service-delivery-demonstration` |
| Classification | `Internal` |
| Primary users | Permit officers and service operations staff |
| Business owner | Synthetic Permit Services Directorate |
| Evidence reference | `evidence://demo/permit-renewal/intent/001` |
| Environment | `demo` |

Every identifier must remain visibly marked as demo or synthetic.

## 4. Personas and separation of duties

| Persona | Demonstration responsibility | Must not do |
|---|---|---|
| Developer | Describe the service intent and inspect generated candidates | Approve own change or deploy directly |
| Enterprise Architect | Confirm systems, architecture, and approved technology context | Bypass policy or package controls |
| Security | Inspect validation, sandbox, and supply-chain results | Replace the human business approver |
| Approver | Review purpose, risk, evidence, and explicitly approve or reject | Be the originating developer |
| Operations Engineer | Verify deployment posture and telemetry | Change policy or evidence history |
| Auditor | Independently verify the complete evidence chain | Execute or mutate the delivery run |

Use separate synthetic identities for Developer and Approver even in a non-production Pilot.

## 5. Governed journey

| Station | What the audience sees | Control being proven |
|---|---|---|
| Intent | Mission, users, classification, and evidence source | A request starts with identity, purpose, and evidence |
| Enterprise Context | Only authorized organizational context is released | Authorization occurs before access and before AI context |
| Existing Systems | Scoped permit, identity, notification, and records dependencies | Inventory is tenant-, purpose-, source-, and classification-bound |
| Existing Architecture | Approved interfaces, constraints, and decisions | Generated work must conform to the institutional baseline |
| Approved Packages | Exact immutable package coordinates | Only approved, current, signed, sovereign packages are eligible |
| AI Planning | A non-executable implementation proposal | AI proposes; it has no workflow or policy authority |
| Code Generation | Inert files on authorized relative paths | No filesystem, tool, Git, or production access is granted to AI |
| Static Validation | Deterministic code-quality findings | Exact signed controls must complete without blocking findings |
| Security Validation | Security findings and signed control evidence | Security policy precedes execution |
| Sandbox | Isolated execution receipt | No production credentials, host filesystem, or unrestricted network |
| Tests | Exact governed test-manifest results | Required tests cannot be skipped, duplicated, or silently ignored |
| Human Review | Purpose, risk, diffs, test results, and explicit decision | Human accountability and separation of duties |
| Git | One signed commit on a non-protected branch | Immutable source history; no force update or hidden change |
| CI/CD | Approved immutable workflow and signed output manifest | Locked build inputs, isolated runner, SBOM, and provenance |
| Artifact | One immutable signed coordinate | Publication without overwrite and complete supply-chain proof |
| Deployment | Exact artifact, environment, preflight, and rollback evidence | Policy-approved sovereign deployment only |
| OpenTelemetry | Traces, metrics, logs, resource identity, and redaction proof | Trusted routing and strict sensitive-data redaction |
| Automatic Registration | The deployed service becomes a governed Enterprise Object | Operational reality is registered atomically |
| Enterprise Model | Read-only contextualized service and relationships | The Enterprise Model remains the contextual source of truth |
| Evidence | Ordered signed chain and independent verification result | The complete outcome is tamper-evident and auditable |

## 6. Presenter flow for the current local build

1. Open the frontend at `http://localhost:5130/build/internal-services`.
2. Explain that the populated permit-renewal example is synthetic.
3. Enter `evidence://demo/permit-renewal/intent/001` as the intent evidence reference.
4. Select **Evaluate intent** and show the completeness result.
5. Explain why **Submit governed intent** remains locked without governed identity, tenant, authorization evidence, and deployment adapters.
6. Walk through the approved execution path and emphasize that no station can be skipped.
7. Open the developer console at `http://localhost:5171/developers`.
8. Open `/health` and show that liveness proves the API process is running.
9. Open `/health/ready` and explain that `503` is the correct fail-closed result while institutional dependencies are absent.
10. Open `/openapi/v1.json` and show that the protected REST contract exists through final Evidence completion.
11. Close with the Pilot plan: connect approved institutional dependencies in controlled groups, test negative paths, then promote using evidence rather than verbal assurance.

## 7. Required negative demonstrations

A government demonstration should show at least one denied path; only showing success hides the platform's core value.

- Missing bearer identity returns an authentication challenge.
- Missing or invalid policy/trust configuration leaves readiness unavailable.
- Missing intent evidence prevents governed submission.
- A developer cannot approve the same change under separation of duties.
- A changed candidate, report, artifact, or evidence digest is rejected.
- An unapproved package, workflow, sandbox image, or deployment target is rejected.
- Evidence entries cannot be updated, deleted, truncated, or appended out of order.

## 8. Demonstration success criteria

- The audience can identify the business outcome, accountable roles, and data classification.
- Every released result is associated with purpose, policy, authorization, and evidence.
- AI output is visibly limited to proposal and inert candidate generation.
- Human approval, Git, CI/CD, artifact verification, deployment, telemetry, registration, and evidence remain distinct gates.
- The presenter accurately distinguishes local contract readiness from institutional runtime readiness.
- No real data, secret, key, credential, or unsupported production claim is used.

