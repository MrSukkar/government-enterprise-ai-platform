# Permit Renewal Demonstration Acceptance Checklist

Status: **Not yet executed**

## A. Local Preview entry

- [ ] Repository is on the expected commit with no unreviewed change.
- [ ] `scripts/verify-project.ps1` succeeds.
- [ ] Backend `/health` returns success.
- [ ] Frontend Create Internal Service page loads.
- [ ] Developer console loads.
- [ ] `/health/ready` accurately reports missing institutional dependencies.
- [ ] Only synthetic identifiers and data are prepared.
- [ ] Presenter states that local readiness is not production readiness.

## B. Business and governance story

- [ ] The permit-renewal outcome and non-autonomous decision boundary are explained.
- [ ] Developer, Approver, Security, Operations, and Auditor responsibilities are distinguishable.
- [ ] Separation of duties is explained before human approval.
- [ ] OPA is identified as policy authority; AI is not.
- [ ] The Enterprise Model is identified as contextual source of truth.
- [ ] The absence of a direct AI-to-Production path is demonstrated.

## C. Current local interaction

- [ ] Service name, mission, primary users, classification, and intent evidence are shown.
- [ ] **Evaluate intent** produces a deterministic completeness result.
- [ ] Locked governed submission is correctly explained as fail-closed behavior.
- [ ] The complete approved execution path is visible.
- [ ] OpenAPI, liveness, readiness, registered modules, and missing adapters are shown.

## D. Integration Demo acceptance

- [ ] Approved synthetic test identities and demo tenant are isolated from Production.
- [ ] Policy bundle, OPA, trust anchors, stores, and graph are deployment-controlled.
- [ ] Every connected runtime uses approved endpoints, profiles, bounds, and trust.
- [ ] Success receipts remain deterministic, scoped, classified, and evidence-bearing.
- [ ] Missing identity, permission, purpose, tenant, clearance, or policy denies access.
- [ ] Separation-of-duties conflict is rejected.
- [ ] Digest, signature, version, idempotency, and evidence-order mismatches are rejected.
- [ ] Sandbox and tests have no production credentials or unrestricted network.
- [ ] No external production effect occurs.
- [ ] The final ten-entry evidence chain verifies independently.

## E. Pilot exit

- [ ] Business owner accepts the user journey and outcome.
- [ ] Security, privacy, architecture, operations, governance, and audit reviews are recorded.
- [ ] Incident, recovery, rollback, key rotation, policy revocation, and dependency-loss exercises are complete.
- [ ] Representative workload benchmarking is recorded before proposing numerical SLOs.
- [ ] Residual risks, exceptions, owners, expiry, and remediation are documented.
- [ ] Production authorization is a distinct recorded decision.

## Result

Record the executed environment, immutable source commit, artifact digest, policy bundle, trust references, evidence-chain verification result, reviewers, decision, and date in the institution's approved evidence system. Do not place secrets or private trust material in this file.

