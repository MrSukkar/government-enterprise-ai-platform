BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.approved_packages_evidence
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    selection_id uuid NOT NULL,
    architecture_discovery_id uuid NOT NULL,
    registration_id uuid NOT NULL,
    registration_version bigint NOT NULL CHECK (registration_version >= 0),
    selection_sha256_digest text NOT NULL CHECK (selection_sha256_digest ~ '^[0-9A-Fa-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL CHECK (evidence_reference ~ '^evidence://approved-packages/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    evidence_references text[] NOT NULL CHECK (cardinality(evidence_references) > 0),
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_approved_packages_evidence PRIMARY KEY (tenant_id, selection_id),
    CONSTRAINT uq_approved_packages_evidence_reference UNIQUE (tenant_id, evidence_reference),
    CONSTRAINT fk_approved_packages_architecture FOREIGN KEY (tenant_id, architecture_discovery_id)
        REFERENCES software_factory.existing_architecture_evidence (tenant_id, discovery_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_approved_packages_registration FOREIGN KEY (tenant_id, registration_id)
        REFERENCES software_factory.governed_intent_registration (tenant_id, registration_id) ON UPDATE RESTRICT ON DELETE RESTRICT
);

COMMIT;
