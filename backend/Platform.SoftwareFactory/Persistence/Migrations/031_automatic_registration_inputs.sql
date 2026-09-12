BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.automatic_registration_delivery_run_snapshots(
    tenant_id text NOT NULL,
    run_id uuid NOT NULL,
    activation_id uuid NOT NULL,
    purpose text NOT NULL,
    record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY(tenant_id,run_id),
    FOREIGN KEY(tenant_id,activation_id) REFERENCES software_factory.opentelemetry_evidence(tenant_id,activation_id));
CREATE TABLE IF NOT EXISTS software_factory.automatic_registration_manifests(
    tenant_id text NOT NULL,
    manifest_id uuid NOT NULL,
    version text NOT NULL,
    activation_id uuid NOT NULL,
    manifest_sha256_digest text NOT NULL CHECK(manifest_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY(tenant_id,manifest_id,version),
    FOREIGN KEY(tenant_id,activation_id) REFERENCES software_factory.opentelemetry_evidence(tenant_id,activation_id));
CREATE TABLE IF NOT EXISTS software_factory.automatic_registrations(
    tenant_id text NOT NULL,
    environment_name text NOT NULL,
    service_identity text NOT NULL,
    request_id uuid NOT NULL,
    request_fingerprint text NOT NULL CHECK(request_fingerprint ~ '^sha256:[0-9a-f]{64}$'),
    enterprise_object_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    committed_at timestamptz NOT NULL,
    PRIMARY KEY(tenant_id,environment_name,service_identity),
    UNIQUE(tenant_id,request_id));
COMMIT;
