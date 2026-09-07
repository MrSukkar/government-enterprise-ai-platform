# Operationalization Wave 02 — Governed Intent Registration Runtime

Status: **Implemented under approved CR-003**

## Operational boundary

This wave connects only the existing Governed Intent Registration station. It does not connect the Enterprise Context reader or any later station.

The endpoint becomes resolvable only when the Wave 01 policy profile and the `PostgreSqlIntentRegistration` profile are both valid. Authentication remains independently configuration gated. The checked-in profile supplies no endpoint, connection string, credential, certificate, or trust material, so runtime behavior remains fail closed.

## PostgreSQL configuration

Configuration section: `PostgreSqlIntentRegistration`.

- `ConnectionString`: deployment-owned PostgreSQL connection string. A valid profile requires host, database, username, `SSL Mode=VerifyFull`, and disabled detailed server errors.
- `CommandTimeoutSeconds`: positive deployment-selected command bound. No numerical SLO is defined by source.

The API never returns this configuration. Source control contains an empty connection string and no secret. Deployment operators must inject the connection through their approved secret/configuration boundary.

## Schema deployment

The reviewed migration is `backend/Platform.SoftwareFactory/Persistence/Migrations/001_governed_intent_registration.sql`.

The application does not run DDL or migrations at startup. An authorized database operator applies the migration through the deployment change process, validates the target and backup posture, grants least-privilege access to the workload identity, and records deployment evidence.

The schema uses tenant-scoped primary and unique keys, immutable registration rows, digest checks, non-empty evidence references, and UTC timestamps. It stores only the approved governed registration record.

## Policy-before-persistence sequence

`Authenticated request -> local RBAC/ABAC -> signed bundle verification -> exact OPA evaluation -> exact decision revalidation -> PostgreSQL transaction -> registration evidence digest -> commit -> receipt`

The Intent adapter sends only the approved decision fields and scoped attributes. A signed bundle must be verified before OPA. Only an exact `Permit` result reaches the repository. OPA denial or transport/response failure never opens a database transaction.

## Atomic repository behavior

- Every command is fixed SQL with typed parameters.
- Reads and conflicts include both tenant and registration identity.
- Creation requires expected version `-1` and candidate version `0`.
- `ON CONFLICT DO NOTHING` prevents overwrite; a conflict is re-read under the same transaction and accepted as unchanged only if the complete authorized candidate matches.
- The persisted registration and its SHA-256-qualified evidence reference are written in one transaction.
- Database unavailability produces a generic `503`; a governed concurrency conflict produces `409`.
- Cancellation and ambiguous commit failures are propagated without automatic mutation retry.

Live OPA/PostgreSQL connectivity and schema application remain deployment-controlled acceptance activities.
