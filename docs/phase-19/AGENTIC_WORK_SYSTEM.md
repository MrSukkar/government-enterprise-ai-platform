# Phase 19 — Agentic Work System

## Purpose

The Agentic Work System executes governed, non-effecting agent computations as durable, resumable work. Agent runtime selection remains behind `IAgentRuntime`; no candidate framework is promoted to a mandatory platform dependency.

## Durable state machine

The persisted states are `AwaitingApproval`, `Ready`, `Running`, `Suspended`, `Completed`, `Failed`, and `Cancelled`. Work definitions require tenant, initiator, purpose, classification, policy references, evidence, and an ordered plan with unique, contiguous step identities.

Every work item starts in `AwaitingApproval`. Approval requires `agentic.work.approve`, a human evidence reference, and separation of duties from the initiator. Suspended work can return to `Ready` only through `agentic.work.resume` and operator-review evidence.

## Crash-safe execution

Before a runtime is invoked, the engine atomically persists `Running` with an expected version. A step has a stable idempotency key derived from work identity, step identity, and durable ordinal. If execution is interrupted, reloading `Running` repeats the same step with the same key and checkpoint.

Each runtime result must match the work, step, and idempotency key; contain a durable checkpoint and evidence; have valid time and outcome; and remain `IsExternallyEffecting => false`. Successful results advance one durable step. Suspended and failed results do not advance the cursor.

The store atomically appends every transition with source and destination state, expected version, actor, reason, step, idempotency key, checkpoint, evidence, and time. Returned persisted state is revalidated, including the complete immutable definition, so the store cannot silently change governed state.

## Phase boundary

Phase 19 provides durable agent state and computation only. It cannot call an external action or convert understanding directly into an effect. Governed actions, OPA decisions, and MCP boundaries begin in Phase 20.
