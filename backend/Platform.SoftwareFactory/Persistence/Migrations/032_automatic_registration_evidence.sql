BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.automatic_registration_evidence(
    tenant_id text NOT NULL,
    registration_id uuid NOT NULL,
    activation_id uuid NOT NULL,
    delivery_run_id uuid NOT NULL,
    request_fingerprint text NOT NULL CHECK(request_fingerprint ~ '^sha256:[0-9a-f]{64}$'),
    enterprise_object_id uuid NOT NULL,
    record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    evidence_references text[] NOT NULL,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY(tenant_id,registration_id),
    FOREIGN KEY(tenant_id,activation_id) REFERENCES software_factory.opentelemetry_evidence(tenant_id,activation_id),
    FOREIGN KEY(tenant_id,delivery_run_id) REFERENCES software_factory.automatic_registration_delivery_run_snapshots(tenant_id,run_id),
    FOREIGN KEY(tenant_id,registration_id) REFERENCES software_factory.automatic_registrations(tenant_id,request_id));
COMMIT;
