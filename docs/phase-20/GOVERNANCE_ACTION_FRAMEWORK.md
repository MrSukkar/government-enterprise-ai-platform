# Phase 20 — Governance & Action Framework

## Purpose

Phase 20 introduces the only permitted path from an approved enterprise workflow to an external effect:

`Authorized request -> signed policy verification -> OPA decision -> approval evidence -> action intent evidence -> governed executor -> MCP tool -> result evidence`

There is no direct AI-to-action, runtime-to-tool, or MCP-to-production path. The AI runtime is not the policy authority and cannot manufacture an authorized command.

## Policy boundary

Every action names a versioned policy bundle, SHA-256 digest, signature reference, activation time, and environment. `IPolicyBundleVerifier` verifies the signed bundle before `IOpaPolicyDecisionPoint` evaluates it. Bundle identity, version, digest, and environment are revalidated on every returned verification and OPA decision. Missing, invalid, stale, or mismatched data fails closed.

OPA results require explicit `Permit` or `Deny`, reasons, evidence, and evaluation time. A denial is recorded and never reaches an executor.

## Identity, approval, and evidence

Requests require `governance.action.execute`, tenant identity, actor, purpose, classification, environment, target, source evidence, and a human approval evidence reference. The approver must differ from the requester.

The evidence journal is called for request, policy verification, policy decision, approval, action intent, denial, and result. Action intent is durably requested before an effect is attempted. A stable request-based idempotency key permits safe retry and reconciliation.

## MCP tool gateway

`McpGovernedActionExecutor` accepts only an `AuthorizedActionCommand` created after a verified OPA permit. The registered tool binding must match tenant, action, and environment; be enabled; allow the request classification; and carry a SHA-256 input-schema digest and registration evidence. MCP responses are treated as untrusted and revalidated against request, server, tool, schema, and idempotency identities.

The MCP client, registry, OPA decision point, policy verifier, evidence journal, and action executor remain abstractions so sovereign and air-gapped deployments can provide local implementations without an external control plane.

## Phase boundary

This phase establishes governance and tool-execution contracts. It does not add enterprise impact modeling, simulation, proactive intelligence, final productization, or the Phase 30 cryptographic Evidence Engine.
