BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.static_validation_delivery_run_snapshots
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    run_id uuid NOT NULL,
    generation_id uuid NOT NULL,
    candidate_sha256_digest text NOT NULL CHECK (candidate_sha256_digest ~ '^[0-9a-f]{64}$'),
    purpose text NOT NULL CHECK (btrim(purpose) <> ''),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL CHECK (evidence_reference ~ '^evidence://delivery-runs/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_static_validation_delivery_runs PRIMARY KEY (tenant_id, run_id),
    CONSTRAINT uq_static_validation_delivery_run_evidence UNIQUE (tenant_id, evidence_reference),
    CONSTRAINT fk_static_validation_run_generation FOREIGN KEY (tenant_id, generation_id)
        REFERENCES software_factory.code_generation_evidence (tenant_id, generation_id) ON UPDATE RESTRICT ON DELETE RESTRICT
);

COMMIT;
