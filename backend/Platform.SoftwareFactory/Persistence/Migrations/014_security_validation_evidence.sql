BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.security_validation_evidence
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    validation_id uuid NOT NULL,
    static_validation_id uuid NOT NULL,
    generation_id uuid NOT NULL,
    delivery_run_id uuid NOT NULL,
    candidate_sha256_digest text NOT NULL CHECK (candidate_sha256_digest ~ '^[0-9a-f]{64}$'),
    static_report_sha256_digest text NOT NULL CHECK (static_report_sha256_digest ~ '^[0-9a-f]{64}$'),
    security_report_sha256_digest text NOT NULL CHECK (security_report_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL CHECK (evidence_reference ~ '^evidence://security-validation/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    evidence_references text[] NOT NULL CHECK (cardinality(evidence_references) > 0),
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_security_validation_evidence PRIMARY KEY (tenant_id, validation_id),
    CONSTRAINT uq_security_validation_evidence_reference UNIQUE (tenant_id, evidence_reference),
    CONSTRAINT fk_security_validation_static FOREIGN KEY (tenant_id, static_validation_id)
        REFERENCES software_factory.static_validation_evidence (tenant_id, validation_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_security_validation_run FOREIGN KEY (tenant_id, delivery_run_id)
        REFERENCES software_factory.security_validation_delivery_run_snapshots (tenant_id, run_id) ON UPDATE RESTRICT ON DELETE RESTRICT
);

COMMIT;
