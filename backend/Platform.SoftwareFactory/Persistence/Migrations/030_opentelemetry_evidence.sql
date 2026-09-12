BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.opentelemetry_evidence(
    tenant_id text NOT NULL,
    activation_id uuid NOT NULL,
    deployment_id uuid NOT NULL,
    delivery_run_id uuid NOT NULL,
    telemetry_profile_sha256_digest text NOT NULL CHECK(telemetry_profile_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    evidence_references text[] NOT NULL,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY(tenant_id,activation_id),
    FOREIGN KEY(tenant_id,deployment_id) REFERENCES software_factory.deployment_evidence(tenant_id,deployment_id),
    FOREIGN KEY(tenant_id,delivery_run_id) REFERENCES software_factory.opentelemetry_delivery_run_snapshots(tenant_id,run_id));
COMMIT;
