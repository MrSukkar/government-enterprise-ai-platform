BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.code_generation_evidence
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    generation_id uuid NOT NULL,
    planning_id uuid NOT NULL,
    package_selection_id uuid NOT NULL,
    delivery_run_id uuid NOT NULL,
    candidate_sha256_digest text NOT NULL CHECK (candidate_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL CHECK (evidence_reference ~ '^evidence://code-generation/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    evidence_references text[] NOT NULL CHECK (cardinality(evidence_references) > 0),
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_code_generation_evidence PRIMARY KEY (tenant_id, generation_id),
    CONSTRAINT uq_code_generation_evidence_reference UNIQUE (tenant_id, evidence_reference),
    CONSTRAINT fk_code_generation_planning FOREIGN KEY (tenant_id, planning_id)
        REFERENCES software_factory.ai_planning_evidence (tenant_id, planning_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_code_generation_packages FOREIGN KEY (tenant_id, package_selection_id)
        REFERENCES software_factory.approved_packages_evidence (tenant_id, selection_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_code_generation_run FOREIGN KEY (tenant_id, delivery_run_id)
        REFERENCES software_factory.code_generation_delivery_run_snapshots (tenant_id, run_id) ON UPDATE RESTRICT ON DELETE RESTRICT
);

COMMIT;
