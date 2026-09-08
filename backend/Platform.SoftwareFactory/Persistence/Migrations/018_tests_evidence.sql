BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.tests_evidence (
    tenant_id text NOT NULL,
    execution_id uuid NOT NULL,
    sandbox_execution_id uuid NOT NULL,
    security_validation_id uuid NOT NULL,
    generation_id uuid NOT NULL,
    delivery_run_id uuid NOT NULL,
    candidate_sha256_digest text NOT NULL CHECK (candidate_sha256_digest ~ '^[0-9a-f]{64}$'),
    security_report_sha256_digest text NOT NULL CHECK (security_report_sha256_digest ~ '^[0-9a-f]{64}$'),
    sandbox_result_sha256_digest text NOT NULL CHECK (sandbox_result_sha256_digest ~ '^[0-9a-f]{64}$'),
    test_manifest_sha256_digest text NOT NULL CHECK (test_manifest_sha256_digest ~ '^[0-9a-f]{64}$'),
    result_sha256_digest text NOT NULL CHECK (result_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL UNIQUE,
    evidence_references text[] NOT NULL,
    recorded_at timestamptz NOT NULL,
    PRIMARY KEY (tenant_id, execution_id),
    FOREIGN KEY (tenant_id, sandbox_execution_id)
        REFERENCES software_factory.sandbox_evidence (tenant_id, execution_id),
    FOREIGN KEY (tenant_id, delivery_run_id)
        REFERENCES software_factory.tests_delivery_run_snapshots (tenant_id, run_id)
);

COMMIT;
