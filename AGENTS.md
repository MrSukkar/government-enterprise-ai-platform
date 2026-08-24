# Government Enterprise AI Platform — Agent Operating Contract

This file is the persistent operating contract for every Codex session working in this repository.

## Authority and scope

1. `docs/PROJECT_MASTER_SPECIFICATION_V2.md` is the only implementation authority.
2. `docs/30_PHASE_ROADMAP.md` defines the fixed phase order.
3. `PROJECT_STATUS.md` and `project-os/project-state.json` define the current gate.
4. Do not use or restore any previous unapproved package.
5. Do not introduce an architectural deviation without the approved Change Control path.

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
