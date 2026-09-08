BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.ai_planning_evidence
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    planning_id uuid NOT NULL,
    package_selection_id uuid NOT NULL,
    delivery_run_id uuid NOT NULL,
    candidate_sha256_digest text NOT NULL CHECK (candidate_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL CHECK (evidence_reference ~ '^evidence://ai-planning/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    evidence_references text[] NOT NULL CHECK (cardinality(evidence_references) > 0),
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_ai_planning_evidence PRIMARY KEY (tenant_id, planning_id),
    CONSTRAINT uq_ai_planning_evidence_reference UNIQUE (tenant_id, evidence_reference),
    CONSTRAINT fk_ai_planning_packages FOREIGN KEY (tenant_id, package_selection_id)
        REFERENCES software_factory.approved_packages_evidence (tenant_id, selection_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_ai_planning_run FOREIGN KEY (tenant_id, delivery_run_id)
        REFERENCES software_factory.ai_planning_delivery_run_snapshots (tenant_id, run_id) ON UPDATE RESTRICT ON DELETE RESTRICT
);

COMMIT;
