BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.code_generation_delivery_run_snapshots
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    run_id uuid NOT NULL,
    planning_id uuid NOT NULL,
    planning_sha256_digest text NOT NULL CHECK (planning_sha256_digest ~ '^[0-9a-f]{64}$'),
    package_selection_id uuid NOT NULL,
    selection_sha256_digest text NOT NULL CHECK (selection_sha256_digest ~ '^[0-9a-f]{64}$'),
    purpose text NOT NULL CHECK (btrim(purpose) <> ''),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL CHECK (evidence_reference ~ '^evidence://delivery-runs/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_code_generation_delivery_run_snapshots PRIMARY KEY (tenant_id, run_id),
    CONSTRAINT uq_code_generation_delivery_run_evidence UNIQUE (tenant_id, evidence_reference),
    CONSTRAINT fk_code_generation_run_planning FOREIGN KEY (tenant_id, planning_id)
        REFERENCES software_factory.ai_planning_evidence (tenant_id, planning_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_code_generation_run_packages FOREIGN KEY (tenant_id, package_selection_id)
        REFERENCES software_factory.approved_packages_evidence (tenant_id, selection_id) ON UPDATE RESTRICT ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS software_factory.governed_code_generation_prompt_templates
(
    template_id text NOT NULL CHECK (btrim(template_id) <> ''),
    template_version text NOT NULL CHECK (btrim(template_version) <> ''),
    content_sha256_digest text NOT NULL CHECK (content_sha256_digest ~ '^[0-9a-f]{64}$'),
    content text NOT NULL CHECK (btrim(content) <> ''),
    allowed_tenant_ids text[] NOT NULL CHECK (cardinality(allowed_tenant_ids) > 0),
    allowed_purposes text[] NOT NULL CHECK (cardinality(allowed_purposes) > 0),
    allowed_environments text[] NOT NULL CHECK (cardinality(allowed_environments) > 0),
    maximum_classification text NOT NULL CHECK (btrim(maximum_classification) <> ''),
    is_code_generation_approved boolean NOT NULL,
    is_active boolean NOT NULL,
    approved_at timestamp with time zone NOT NULL,
    signature_algorithm text NOT NULL CHECK (signature_algorithm IN ('RS256', 'ES256')),
    signature_key_id text NOT NULL CHECK (btrim(signature_key_id) <> ''),
    signature_base64 text NOT NULL CHECK (btrim(signature_base64) <> ''),
    certificate_chain_reference text NOT NULL CHECK (btrim(certificate_chain_reference) <> ''),
    signed_at timestamp with time zone NOT NULL,
    signature_evidence_reference text NOT NULL CHECK (btrim(signature_evidence_reference) <> ''),
    CONSTRAINT pk_governed_code_generation_prompts PRIMARY KEY (template_id, template_version)
);

COMMIT;
