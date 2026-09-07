BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.enterprise_context_evidence
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    discovery_id uuid NOT NULL,
    registration_id uuid NOT NULL,
    registration_version bigint NOT NULL CHECK (registration_version >= 0),
    context_sha256_digest text NOT NULL CHECK (context_sha256_digest ~ '^[0-9A-Fa-f]{64}$'),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    evidence_reference text NOT NULL CHECK (evidence_reference ~ '^evidence://enterprise-context/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    evidence_references text[] NOT NULL CHECK (cardinality(evidence_references) > 0 AND array_position(evidence_references, NULL) IS NULL AND array_position(evidence_references, '') IS NULL),
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_enterprise_context_evidence PRIMARY KEY (tenant_id, discovery_id),
    CONSTRAINT uq_enterprise_context_evidence_reference UNIQUE (tenant_id, evidence_reference),
    CONSTRAINT fk_enterprise_context_registration FOREIGN KEY (tenant_id, registration_id)
        REFERENCES software_factory.governed_intent_registration (tenant_id, registration_id)
        ON UPDATE RESTRICT ON DELETE RESTRICT
);

COMMIT;
