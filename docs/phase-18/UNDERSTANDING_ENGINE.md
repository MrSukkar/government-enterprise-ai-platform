# Phase 18 — Understanding Engine

## Purpose

The Understanding Engine produces a governed, read-only interpretation of authorized Enterprise Model objects and evidence-backed facts. It does not update the model, choose an action, or execute work.

## Authorization and context

Every request requires subject identity, `enterprise.understanding.read`, tenant, explicit Enterprise Object scope, maximum classification, purpose, and time. The context provider must return a snapshot tied to that request. The engine rejects objects, facts, evidence, classifications, or object references outside the authorized scope.

## Knowledge-state discipline

Every fact and output claim is labeled as one of the approved states:

- `Confirmed`: must exactly match a confirmed snapshot fact, classification, object references, and evidence.
- `Discovered`: must exactly match a discovered snapshot fact, classification, object references, and evidence.
- `Inferred`: must retain the inferred label, cite authorized evidence, and have grounded supporting facts.
- `Unknown`: has zero confidence; without supporting facts it receives the request's maximum classification to prevent accidental downgrade.

Confirmed, discovered, and inferred items require evidence. Claims cannot exceed the requested classification and cannot downgrade the classification of supporting facts.
The report summary carries its own classification and cannot be classified below any claim it summarizes.

## Analyzer boundary

`IUnderstandingAnalyzer` is the replaceable analysis boundary. Whether implemented deterministically or with an approved AI runtime, its candidate is treated as untrusted. The engine validates request identity, timing, authorization, grounding, evidence, object scope, classification, and knowledge-state semantics before producing a report.

Both `UnderstandingCandidate` and `UnderstandingReport` declare `IsExecutable => false`. This phase performs `UNDERSTAND` only; decision and action capabilities remain in their approved later phases.
