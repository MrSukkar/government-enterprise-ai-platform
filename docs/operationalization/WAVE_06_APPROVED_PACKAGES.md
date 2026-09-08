# Operationalization Wave 06 — Governed Approved Packages Runtime

Authority: approved `CR-007`; source of truth: `docs/PROJECT_MASTER_SPECIFICATION_V2.md`.

This wave connects only Governed Approved Packages Selection. It cryptographically revalidates the exact PostgreSQL Existing Architecture evidence, verifies a signed policy bundle, obtains an exact action-specific OPA coordinate scope before catalog access, reads only matching institutional records, applies the existing eligibility rules, verifies canonical digest-bound supply-chain attestations using deployment-pinned public-key trust, re-authorizes every package through RBAC/ABAC, and atomically records immutable PostgreSQL evidence.

`Authenticated request -> exact Existing Architecture proof -> signed bundle -> exact-coordinate OPA scope -> read-only institutional catalog -> eligibility -> cryptographic supply-chain assurance -> per-package RBAC/ABAC -> deterministic selection digest -> atomic evidence`

The catalog migration stores metadata only. Package content is never downloaded, restored, installed, unpacked, mounted, copied, cached, resolved, executed, published, or deleted. The runtime performs no public registry call and no catalog or approval mutation.

Adapters compose only when policy, PostgreSQL, and signed-attestation trust profiles are complete and valid. Repository defaults intentionally contain no connection, package record, credential, public key, institutional approval, or attestation and therefore report `unconfigured` and fail closed.

Migrations `005_institutional_package_catalog.sql` and `006_approved_packages_evidence.sql` are deployment-controlled and never applied automatically. Ambiguous mutations are never retried.

`IAuthorizedApprovedPackagesSnapshotReader`, AI Planning, Code Generation, later stations, workflow advancement, package transfer, AI invocation, and production action remain disconnected.
