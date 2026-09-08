BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.security_validation_delivery_run_snapshots
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    run_id uuid NOT NULL,
    generation_id uuid NOT NULL,
    static_validation_id uuid NOT NULL,
    candidate_sha256_digest text NOT NULL CHECK (candidate_sha256_digest ~ '^[0-9a-f]{64}$'),
    static_report_sha256_digest text NOT NULL CHECK (static_report_sha256_digest ~ '^[0-9a-f]{64}$'),
    purpose text NOT NULL CHECK (btrim(purpose) <> ''),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL CHECK (evidence_reference ~ '^evidence://delivery-runs/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_security_validation_delivery_run PRIMARY KEY (tenant_id, run_id),
    CONSTRAINT fk_security_validation_static FOREIGN KEY (tenant_id, static_validation_id)
        REFERENCES software_factory.static_validation_evidence (tenant_id, validation_id) ON UPDATE RESTRICT ON DELETE RESTRICT
);

COMMIT;
