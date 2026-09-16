# Government Enterprise AI Platform — Agent Operating Contract

This file is the persistent operating contract for every Codex session working in this repository.

## Authority and scope

1. `docs/PROJECT_MASTER_SPECIFICATION_V2.md` is the only implementation authority.
2. `docs/30_PHASE_ROADMAP.md` defines the fixed phase order.
3. `PROJECT_STATUS.md` and `project-os/project-state.json` define the current gate.
4. Do not use or restore any previous unapproved package.
5. Do not introduce an architectural deviation without the approved Change Control path.

## Product north star

The platform unifies one governed institutional cycle:

`BUILD <-> UNDERSTAND <-> OPERATE <-> ACT`, with Governance providing `CONTROL` and Evidence providing `PROVE` across the entire cycle.

Every implementation and demo increment must make this cycle clearer or more executable without weakening authorization, sovereignty, human approval, or cryptographic evidence.

## Engineering operating model

This repository uses **Governed AI-Assisted Software Engineering**. Software-engineering discipline is authoritative; AI accelerates analysis, implementation, testing, and documentation inside that discipline.

1. Define scope and acceptance before implementation.
2. Keep changes small, reviewable, and limited to the active gate.
3. Use Change Control for architecture or authority changes.
4. Verify both permitted and denied/fail-closed behavior.
5. Local success is insufficient: required CI must be green before a gate is complete.
6. Never commit secrets, credentials, tokens, private keys, or local runtime state.
7. Finish and record the current gate before starting the next gate.
8. Every completed gate requires acceptance evidence, synchronized status, an immutable commit, GitHub synchronization, and green CI.
9. Use a `codex/` branch and pull request for material changes; do not force-push or mutate protected history.
10. Resume work from the repository contract, status, acceptance evidence, and Git historyâ€”never from conversational memory alone.

Vibe coding is allowed only for disposable exploration or non-authoritative visual prototypes. It is prohibited for identity, authorization, policy, persistence, evidence, API contracts, supply chain, CI/CD, deployment, and pilot or production paths.

## Mandatory working sequence

Before changing files:

1. Read the Master Specification completely.
2. Read `PROJECT_STATUS.md` and `project-os/project-state.json`.
3. Read the acceptance gate for the active phase and all directly preceding phase gates.
4. Inspect the actual repository and Git state; never assume a prior operation succeeded.

For every phase:

1. Implement only the active phase.
2. Preserve all constitutional invariants and module boundaries.
3. Add or update the phase acceptance artifact.
4. Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-project.ps1`.
5. Do not mark the phase complete when build or acceptance verification fails.
6. Update both project status files atomically.
7. Commit and push only verified source; never commit secrets, `bin`, `obj`, temporary packages, or local IDE state.

## Architectural invariants

- Backend: .NET 10 / ASP.NET Core Modular Monolith.
- Frontend: Blazor WebAssembly.
- API: REST + OpenAPI 3.1.
- The Enterprise Model is the contextual source of truth.
- AI runtime is not policy authority; the LLM has no workflow authority.
- No direct `AI -> Production` path.
- Retrieval is authorized before access and re-authorized before AI context.
- Evidence is cross-cutting, append-only, tamper-evident, traceable, access-controlled, and cryptographically verifiable.
- Sovereign and air-gapped operation has no mandatory external control-plane dependency.
- Conditional technologies remain conditional until approved validation.
- No numerical SLO is invented before workload benchmarking.

## Human approval boundary

Continue autonomously for repository reads/writes, builds, tests, documentation, local verification, and approved GitHub synchronization. Stop for explicit user approval when Windows elevation, new external credentials, public deployment, destructive data operations, security-sensitive policy decisions, or an architectural Change Request is required.

## Visual Studio

Visual Studio is a development viewer, debugger, and interactive runner—not the source of truth. The filesystem, verification scripts, Git history, and GitHub repository are authoritative. Do not block repository work merely because an IDE window needs refresh or reload.
