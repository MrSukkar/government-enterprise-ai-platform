BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.human_review_evidence (
 tenant_id text NOT NULL, review_id uuid NOT NULL, tests_execution_id uuid NOT NULL,
 sandbox_execution_id uuid NOT NULL, security_validation_id uuid NOT NULL, generation_id uuid NOT NULL,
 delivery_run_id uuid NOT NULL, reviewer_subject_id text NOT NULL, decision text NOT NULL CHECK (decision IN ('Approve','Reject')),
 rationale text NOT NULL, human_attestation_reference text NOT NULL,
 review_package_sha256_digest text NOT NULL CHECK (review_package_sha256_digest ~ '^[0-9a-f]{64}$'),
 policy_decision_request_id uuid NOT NULL, policy_bundle_sha256_digest text NOT NULL CHECK (policy_bundle_sha256_digest ~ '^[0-9a-f]{64}$'),
 version bigint NOT NULL CHECK (version >= 0), record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
 record_json jsonb NOT NULL, evidence_reference text NOT NULL UNIQUE, evidence_references text[] NOT NULL, recorded_at timestamptz NOT NULL,
 PRIMARY KEY (tenant_id, review_id),
 FOREIGN KEY (tenant_id, tests_execution_id) REFERENCES software_factory.tests_evidence (tenant_id, execution_id),
 FOREIGN KEY (tenant_id, delivery_run_id) REFERENCES software_factory.human_review_delivery_run_snapshots (tenant_id, run_id));
COMMIT;
