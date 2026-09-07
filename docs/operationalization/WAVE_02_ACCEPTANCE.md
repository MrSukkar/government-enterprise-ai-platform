# Operationalization Wave 02 Acceptance

Status: **Satisfied**

Change control: `docs/change-control/CR-003-OPERATIONALIZATION-WAVE-02.md`

## Acceptance evidence

- CR-003 is explicitly approved by the repository owner.
- Npgsql is pinned and locked at `10.0.3`; no ORM or second database technology is introduced.
- The checked-in PostgreSQL profile is empty and unconfigured, contains no secret, and creates no data source.
- Valid configuration requires PostgreSQL host, database, username, `SSL Mode=VerifyFull`, disabled detailed errors, and a positive deployment-selected command timeout.
- Signed policy-bundle verification precedes the exact typed OPA Intent decision.
- Only the exact `internal-service.intent.register` action is accepted by the Intent policy adapter.
- OPA identity, resource, bundle, digest, environment, outcome, evidence, and decision time are revalidated.
- Both exact runtime contracts are registered only when policy and PostgreSQL profiles are valid.
- No repository access occurs before OPA permit in the existing Governed Intent Registration engine.
- The migration is versioned and reviewable but cannot execute automatically at API startup.
- PostgreSQL access uses fixed parameterized SQL, tenant-scoped keys, explicit transactions, bounded commands, conflict-safe insertion, and no overwrite path.
- Creation enforces version `-1 -> 0`; non-identical conflicts fail with governed optimistic concurrency.
- Registration and its SHA-256-qualified evidence reference are committed atomically.
- Database unavailability, timeouts, malformed stored classification, cancellation, and ambiguous commit outcomes cannot return success or trigger an automatic retry.
- Readiness reports PostgreSQL state without disclosing configuration.
- Repository-default runtime remains safely unconfigured with all 142 disconnected institutional dependencies fail closed.
- Live provider, OPA, PostgreSQL, schema, credential, PKI, and trust integration remains deployment-controlled acceptance.
- Existing 30 phase, 22 operational-increment, and Wave 01 gates remain unchanged.
- All 15 projects build with zero warnings and zero errors.

## Verification result

`scripts/verify-project.ps1` verifies the complete historical acceptance chain, the approved and locked package, policy-before-persistence ordering, configuration and SQL guards, build, repository-default runtime behavior, readiness disclosure, and OpenAPI contract.
