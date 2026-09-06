# Operational Increment 12 — Acceptance

Status: **Satisfied**

Authority: `docs/change-control/CR-001-AMENDMENT-09-SANDBOX.md`

## Evidence

- [x] Sandbox requires exact Code Generation and accepted Security Validation identities, digests, and evidence.
- [x] Signed OPA authorization precedes every prerequisite read, image access, and runtime invocation.
- [x] Authoritative Security receipt, candidate, and run are loaded only through deployment-controlled readers after permit.
- [x] The delivery run must be stopped exactly at `SecurityValidation` with complete ordered evidence.
- [x] Only an exact institutional `SandboxImage` with eligibility and complete sovereign supply-chain assurance is accepted.
- [x] Isolation requires Firecracker-class ephemeral microVMs, no production credentials, no host filesystem, network default deny, and configured limits.
- [x] Secret-like environment material and policy-unapproved environment or network scope fail closed.
- [x] Only the existing `GovernedSandboxService` and vendor-neutral `ISecuritySandboxRuntime` may invoke execution.
- [x] Acceptance requires zero exit, no timeout, no isolation violation, unique artifact references, and execution evidence.
- [x] Result authorization and cryptographic evidence precede receipt release.
- [x] The receipt records `ProductionEffectOccurred: false`, `CanAdvance: false`, and creates no `StageCompletion`.
- [x] Missing dependencies remain visible and fail closed without disclosure, transfer, execution, mutation, or external effect.
- [x] No Tests approval, production effect, source write, Git, CI/CD, artifact publication, deployment, or workflow mutation is introduced.
- [x] OpenAPI and Blazor expose the boundary without claiming image/runtime readiness, Tests approval, or production effects.
- [x] All prior phase and Increment 01–11 gates remain satisfied.
- [x] All 15 projects build with zero warnings and zero errors.
- [x] Runtime verification proves the protected endpoint and all 70 required runtime dependencies remain fail closed.

## Exit decision

Operational Increment 12 is complete. Governed Security Sandbox Execution is OPA-authorized before prerequisite reads or execution, bound to accepted Security evidence, an authoritative inert code candidate, a deterministic run at `SecurityValidation`, an exact supply-chain-assured institutional image, and Firecracker-class ephemeral isolation. Accepted results require zero exit, no timeout or isolation violation, result authorization, and cryptographic evidence. No production effect or workflow advancement is possible; Tests remains separately governed and unauthorized.
