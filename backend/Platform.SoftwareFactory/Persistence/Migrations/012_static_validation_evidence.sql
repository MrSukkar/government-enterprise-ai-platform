BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.static_validation_evidence
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    validation_id uuid NOT NULL,
    generation_id uuid NOT NULL,
    delivery_run_id uuid NOT NULL,
    candidate_sha256_digest text NOT NULL CHECK (candidate_sha256_digest ~ '^[0-9a-f]{64}$'),
    report_sha256_digest text NOT NULL CHECK (report_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL CHECK (evidence_reference ~ '^evidence://static-validation/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    evidence_references text[] NOT NULL CHECK (cardinality(evidence_references) > 0),
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_static_validation_evidence PRIMARY KEY (tenant_id, validation_id),
    CONSTRAINT uq_static_validation_evidence_reference UNIQUE (tenant_id, evidence_reference),
    CONSTRAINT fk_static_validation_generation FOREIGN KEY (tenant_id, generation_id)
        REFERENCES software_factory.code_generation_evidence (tenant_id, generation_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_static_validation_run FOREIGN KEY (tenant_id, delivery_run_id)
        REFERENCES software_factory.static_validation_delivery_run_snapshots (tenant_id, run_id) ON UPDATE RESTRICT ON DELETE RESTRICT
);

COMMIT;
