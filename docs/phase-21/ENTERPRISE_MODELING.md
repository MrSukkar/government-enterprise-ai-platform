# Phase 21 — Enterprise Modeling

## Purpose

Phase 21 turns the authorized Enterprise Model into a traceable structural impact analysis capability. It answers which governed objects are connected to a proposed change and through which known relationships. It does not predict outcomes or execute changes.

## Authorization and model snapshot

Every request requires `enterprise.modeling.analyze`, tenant and subject identity, purpose, explicit object scope, maximum classification, an explicit traversal-depth bound, time, and evidence-grounded change proposal. The change target must be inside the authorized scope.

`IEnterpriseModelSnapshotProvider` is responsible for authorization before access. Its returned snapshot is still treated as untrusted: request and tenant identity, object uniqueness, scope, classification, timestamps, object invariants, and authorization evidence are revalidated fail-closed before analysis.

## Deterministic impact analysis

The engine constructs an in-memory view only from authorized objects. Relationships whose targets are absent from that view are counted as excluded and never traversed. Starting at the proposed change target, bounded traversal produces a deterministic shortest structural path to each reachable object.

Each impact preserves:

- object identity, type, owner, and classification;
- distance from the change;
- the complete relationship path;
- `CONFIRMED`, `DISCOVERED`, `INFERRED`, or `UNKNOWN` knowledge basis;
- relationship confidence and supporting evidence.

The report explicitly states scope and depth limitations. It does not invent risk severity, business probability, performance targets, or simulated outcomes.

## Phase boundary

Phase 21 is read-only analysis over authorized enterprise context. Simulation, digital-twin behavior, resilience experiments, and disaster recovery begin in Phase 22. No action path is introduced here.
