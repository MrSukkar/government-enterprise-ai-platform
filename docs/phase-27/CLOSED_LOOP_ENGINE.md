# Phase 27 — Closed Loop Engine

## Purpose

Phase 27 closes the learning loop from a specific delivered release through registration and operations back into a new governed software intent. It does not create self-modifying software or automatic production changes.

## Traceable evaluation request

Requests require `software.closedloop.evaluate`, tenant and subject identity, purpose, registered Enterprise Object reference, release artifact SHA-256 digest, release provenance, a completed observation window, and a signed/versioned closed-loop policy.

`IClosedLoopContextProvider` authorizes and correlates the release with delivery evidence, automatic-registration evidence, telemetry evidence, and policy-verification evidence. Returned identity, tenant, object, artifact, provenance, policy signature/version/digest, evidence, and time are revalidated fail-closed.

## Governed improvement proposal

`IClosedLoopAnalyzer` may propose reliability, security, performance, maintainability, or compliance improvements. Candidates must contain title, rationale, a new proposed intent, bounded confidence, and only evidence from the authorized correlated context.

A deterministic SHA-256 fingerprint over tenant, object, release, policy, kind, and intent prevents duplicate feedback from silently multiplying. Each proposal is registered atomically and returned state is revalidated.

Every proposal is structurally non-effecting, requires human review, and requires a new Software Factory delivery run. It must therefore repeat enterprise context, approved packages, planning, generation, validation, sandbox, tests, review, Git, CI/CD, deployment, registration, observability, and evidence as applicable.

## Phase boundary

Phase 27 adds no autonomous edit, commit, deployment, or action. Architecture consolidation begins in Phase 28.
