# Phase 22 — Simulation & Digital Twin

## Purpose

Phase 22 enables authorized what-if analysis, resilience exercises, and disaster-recovery assessment against an isolated digital-twin snapshot. Simulation output is advisory evidence, never policy authority, workflow authority, or permission to act.

## Governed scenario

Requests require `enterprise.simulation.run`, tenant and subject identity, purpose, explicit object scope, maximum classification, time, and a governed scenario. Each perturbation names an in-scope target, baseline and simulated state references, change type, and evidence.

A resilience scenario must reference a recovery plan by identity, version, SHA-256 digest, and supporting evidence. The platform does not invent RTO, RPO, availability, or other numerical SLOs before approved workload benchmarking.

## Isolated digital twin

`IDigitalTwinSnapshotProvider` supplies an authorized snapshot. Returned content is revalidated for request and tenant identity, version digest, time, evidence, uniqueness, object scope, classification, and perturbation coverage.

Isolation is structural: `IsProductionConnected`, `HasProductionCredentials`, and `AllowsExternalEffects` are always false, while network access is `None`. Any violation fails closed before the simulation runtime is called.

## Durable, evidenced execution

The engine persists a `Started` record before runtime invocation with a stable key derived from request, scenario, and model digest. It validates runtime identities, timestamps, scope, evidence, confidence bounds, and exact governed assumptions. Simulation results are structurally non-effecting and non-authoritative. Completion is persisted atomically against the expected prior state, and returned store state is revalidated.

Projected impacts retain object identity, explanation, confidence, and evidence. A recovery assessment reference is mandatory, but human and policy validation remain required before any governed action.

## Phase boundary

Phase 22 does not introduce proactive monitoring or recommendations; those begin in Phase 23. It does not execute recovery actions or connect the twin to production.
