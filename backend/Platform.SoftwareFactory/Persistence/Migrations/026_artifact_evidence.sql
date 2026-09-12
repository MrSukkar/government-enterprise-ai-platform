BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.artifact_evidence(
    tenant_id text NOT NULL,
    publication_id uuid NOT NULL,
    cicd_execution_id uuid NOT NULL,
    delivery_run_id uuid NOT NULL,
    content_sha256_digest text NOT NULL CHECK(content_sha256_digest ~ '^[0-9a-f]{64}$'),
    immutable_registry_reference text NOT NULL,
    record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    evidence_references text[] NOT NULL,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY(tenant_id,publication_id),
    FOREIGN KEY(tenant_id,cicd_execution_id) REFERENCES software_factory.cicd_evidence(tenant_id,execution_id),
    FOREIGN KEY(tenant_id,delivery_run_id) REFERENCES software_factory.artifact_delivery_run_snapshots(tenant_id,run_id));
COMMIT;
