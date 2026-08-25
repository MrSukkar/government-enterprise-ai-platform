# Phase 17 — Automatic Registration Engine

## Purpose

After governed deployment and OpenTelemetry activation, the registration engine creates or updates the deployed service in the Enterprise Model. Registration is evidence-backed and deterministic; it does not perform understanding, inference, impact analysis, or autonomous action.

## Required registration evidence

Every request requires:

- tenant, environment, and stable service identity;
- Enterprise Object type, owner, classification, policies, and permitted actions;
- algorithm-qualified SHA-256 artifact digest and registry reference;
- deployment evidence;
- software-supply-chain evidence;
- observability evidence;
- human-approval evidence;
- explicit relationship and relationship-evidence collections;
- registration timestamp.

Automatically registered relationships are `Confirmed` only because each relationship requires explicit evidence. The registered object is active, has source `automatic-registration`, and retains all policy, permission, relationship, artifact, deployment, observability, approval, and general evidence references.

## Idempotency and atomicity

The registration key is the tenant, environment, and service identity. The engine builds a canonical, order-stable SHA-256 fingerprint from the governed request. Timestamps and request IDs are excluded from the fingerprint so a legitimate retry resolves to the same registration intent.

`IAutomaticRegistrationRepository.RegisterAtomicallyAsync` is the persistence boundary. It must atomically return `Created`, `Updated`, or `Unchanged` for the registration key and fingerprint. The engine validates the returned request identity, key, fingerprint, disposition, tenant, source, commit time, Enterprise Object, and evidence reference before accepting the result.

## Phase boundary

This phase implements `Automatic Registration -> Enterprise Model`. It does not infer undocumented relationships or interpret operational meaning; those capabilities begin with the approved Understanding Engine in Phase 18.
