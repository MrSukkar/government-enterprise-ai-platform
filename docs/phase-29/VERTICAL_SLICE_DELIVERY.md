# Phase 29 — Approved Vertical Slice Delivery

## Scope

This phase implements the approved internal-service vertical slice without changing the architecture established by PROJECT MASTER SPECIFICATION v2.

## Governed flow

The delivery run follows one deterministic, append-only sequence:

`Intent -> Enterprise Context -> Existing Systems -> Existing Architecture -> Approved Packages -> AI Planning -> Code Generation -> Static Validation -> Security Validation -> Sandbox -> Tests -> Human Review -> Git -> CI/CD -> Artifact -> Deployment -> OpenTelemetry -> Automatic Registration -> Enterprise Model -> Evidence`

Every stage produces a receipt containing a stable idempotency key, output digest, evidence references, actor, and completion time. The run store creates and appends state atomically with optimistic version checks. Persisted state is compared field-by-field with the governed state before it is accepted.

## Control boundaries

- A developer must hold `developer.internal-service.create` and provide tenant-scoped context, existing-system references, the approved architecture, approved packages, and intent evidence.
- Policy approval is mandatory from approved-package selection onward.
- Human approval is mandatory from human review onward, and the reviewer cannot be the initiating developer.
- Only the deployment stage may record an external effect.
- Artifact and later stages require verified supply-chain evidence.
- OpenTelemetry and later stages require emitted telemetry evidence.
- Automatic registration and later stages require Enterprise Model registration.
- No stage can be skipped, reordered, duplicated, or persisted under a different request or receipt.

## Architecture conformance

The implementation remains inside `Platform.SoftwareFactory`, uses the existing deterministic delivery-stage model, and introduces no alternate runtime, storage, policy authority, deployment path, or direct AI-to-production path.
