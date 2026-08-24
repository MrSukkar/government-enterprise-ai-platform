# Phase 11 — AI Development Engine

Status: **Implemented**  
Depends on: **Phase 10 — Software Factory Engine**

## Vendor-neutral runtime

`IAiDevelopmentRuntime` is the only model-runtime boundary used for planning and code generation. The domain does not bind to a provider, SDK, model, network endpoint, or external control plane. A sovereign local runtime can implement the same contract.

## Governed request

An AI development request carries the deterministic delivery run, declared purpose, approved prompt template, authorized enterprise-context references, exact approved packages, and constraints. Planning is permitted only after `ApprovedPackages`; code generation is permitted only after `AiPlanning`.

The runtime produces a candidate artifact. It cannot advance the Software Factory workflow, execute code, write to production, approve itself, or deploy.

## Independent evaluation

Every candidate is evaluated independently for:

- grounding;
- correctness;
- security;
- policy compliance;
- package compliance;
- traceability.

An evaluation is accepted only when it is independent from the generation runtime, all criteria are present and pass, and every finding has an evidence reference. No unbenchmarked numerical threshold or SLO is invented.

An accepted candidate is eligible only as evidence for the deterministic next workflow stage. `IsExecutable` is always false.

## Deferred

- Provider/runtime selection and adapters.
- Code execution and Firecracker-class sandboxing: Phase 12.
- Durable agent execution and governed agent state: Phase 19.
- OPA action policy and tool gateway: Phase 20.

