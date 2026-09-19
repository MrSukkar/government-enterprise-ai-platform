# CR-033 — Integration Demo Stage 11: Governed Security Sandbox

Status: **Approved for bounded implementation**

Authority: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`

Preceding acceptance: `docs/demo/STAGE_10_SECURITY_VALIDATION_ACCEPTANCE.md`

## Decision

Connect only the Stage 11 Integration Demo boundary for Governed Security Sandbox. The exact accepted Stage 10 Security report, authoritative inert candidate, immutable evidence, and `SecurityValidation`-stopped delivery run may reach the existing Sandbox boundary only after signed exact-action OPA authorization, exact institutional image approval, complete supply-chain assurance, and complete deployment configuration.

## Approved scope

1. Add the Stage 11 signed OPA bundle scope for `internal-service.sandbox.execute` and `sandbox-result` only.
2. Bind one exact synthetic `SandboxImage` coordinate, Firecracker-class ephemeral microVM isolation, no production credentials, no host filesystem, network default deny, an empty destination set, one non-secret configuration reference, and positive Integration Demo resource/time limits.
3. Reuse the existing institutional registry, supply-chain verifier, `GovernedSandboxService`, `ISecuritySandboxRuntime`, sovereign HTTPS protocol, result authorizer, and immutable PostgreSQL evidence.
4. Expose the existing protected Sandbox endpoint from the Integration Demo UI only after an accepted Security receipt.
5. Add migrations 015 and 016 to the Stage 11 localhost composition while keeping image records, runtime endpoint, trust, private material, and credentials outside Git.
6. Require a real institutionally approved signed Firecracker-class runtime response for success. Missing configuration remains unavailable and fail closed; no fake execution or fabricated isolation is permitted.
7. Keep the receipt production-effect-free and non-advancing. Tests and all later stages remain outside this gate.

## Prohibited

No fake runtime, synthetic isolation claim, fabricated approval/evidence, production credential, host filesystem, unrestricted network, API-host command/process execution, source mutation, workflow advancement, Tests, public deployment, production action, architectural change, or Phase 31.

## Acceptance authority

Stage 11 may be marked complete only when its independent acceptance artifact is satisfied, the complete project verifier passes, the immutable commit is synchronized to GitHub through a pull request, and required CI is green.
