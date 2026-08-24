# Project Agent Operating System

This directory makes the project state explicit and repeatable across Codex desktop, Remote on mobile, local terminal work, and GitHub.

## Control files

- `AGENTS.md` — persistent agent rules.
- `project-state.json` — machine-readable phase and verification state.
- `PROJECT_STATUS.md` — human-readable status.
- `scripts/verify-project.ps1` — the mandatory local quality gate.
- `scripts/show-project-state.ps1` — concise operator status.
- `docs/REMOTE_WORKFLOW.md` — mobile Remote setup and usage.

## Operating loop

`Inspect -> Implement active phase -> Verify -> Update state -> Commit -> Push -> Report`

An IDE refresh is not a phase gate. A successful verification result and an accepted phase artifact are phase gates.

On Windows, run scripts with a process-only policy override; do not change the machine policy:

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-project.ps1`
