BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.deployment_evidence(
    tenant_id text NOT NULL,
    deployment_id uuid NOT NULL,
    artifact_publication_id uuid NOT NULL,
    delivery_run_id uuid NOT NULL,
    artifact_content_sha256_digest text NOT NULL CHECK(artifact_content_sha256_digest ~ '^[0-9a-f]{64}$'),
    runtime_identity text NOT NULL,
    record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    evidence_references text[] NOT NULL,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY(tenant_id,deployment_id),
    FOREIGN KEY(tenant_id,artifact_publication_id) REFERENCES software_factory.artifact_evidence(tenant_id,publication_id),
    FOREIGN KEY(tenant_id,delivery_run_id) REFERENCES software_factory.deployment_delivery_run_snapshots(tenant_id,run_id));
COMMIT;
