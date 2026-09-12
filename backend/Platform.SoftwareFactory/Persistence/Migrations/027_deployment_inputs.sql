BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.deployment_delivery_run_snapshots(
    tenant_id text NOT NULL,
    run_id uuid NOT NULL,
    artifact_publication_id uuid NOT NULL,
    purpose text NOT NULL,
    record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY(tenant_id,run_id),
    FOREIGN KEY(tenant_id,artifact_publication_id) REFERENCES software_factory.artifact_evidence(tenant_id,publication_id));
CREATE TABLE IF NOT EXISTS software_factory.sovereign_deployment_profiles(
    tenant_id text NOT NULL,
    profile_id uuid NOT NULL,
    version text NOT NULL,
    profile_sha256_digest text NOT NULL CHECK(profile_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY(tenant_id,profile_id,version));
COMMIT;
