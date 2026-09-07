BEGIN;

CREATE SCHEMA IF NOT EXISTS software_factory;

CREATE TABLE IF NOT EXISTS software_factory.governed_intent_registration
(
    tenant_id text NOT NULL CHECK (btrim(tenant_id) <> ''),
    registration_id uuid NOT NULL,
    submission_id uuid NOT NULL,
    subject_id text NOT NULL CHECK (btrim(subject_id) <> ''),
    classification text NOT NULL CHECK (classification IN ('Public', 'Internal', 'Confidential', 'Secret', 'TopSecret')),
    purpose text NOT NULL CHECK (btrim(purpose) <> ''),
    service_name text NOT NULL CHECK (btrim(service_name) <> ''),
    mission text NOT NULL CHECK (btrim(mission) <> ''),
    primary_users text NOT NULL CHECK (btrim(primary_users) <> ''),
    intent_sha256_digest text NOT NULL CHECK (intent_sha256_digest ~ '^[0-9A-Fa-f]{64}$'),
    policy_decision_request_id uuid NOT NULL,
    policy_bundle_id text NOT NULL,
    policy_bundle_version text NOT NULL,
    policy_bundle_sha256_digest text NOT NULL CHECK (policy_bundle_sha256_digest ~ '^[0-9A-Fa-f]{64}$'),
    idempotency_key text NOT NULL,
    version bigint NOT NULL CHECK (version >= 0),
    evidence_references text[] NOT NULL CHECK (cardinality(evidence_references) > 0 AND array_position(evidence_references, NULL) IS NULL AND array_position(evidence_references, '') IS NULL),
    registration_evidence_reference text NOT NULL CHECK (registration_evidence_reference ~ '^evidence://intent-registration/[0-9a-f-]{36}/sha256/[0-9a-f]{64}$'),
    registered_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_governed_intent_registration PRIMARY KEY (tenant_id, registration_id),
    CONSTRAINT uq_governed_intent_submission UNIQUE (tenant_id, submission_id),
    CONSTRAINT uq_governed_intent_idempotency UNIQUE (tenant_id, idempotency_key)
);

COMMIT;
