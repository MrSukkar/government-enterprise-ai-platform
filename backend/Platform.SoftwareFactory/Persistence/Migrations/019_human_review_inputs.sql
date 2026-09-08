BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.human_review_delivery_run_snapshots (
 tenant_id text NOT NULL, run_id uuid NOT NULL, tests_execution_id uuid NOT NULL,
 sandbox_execution_id uuid NOT NULL, generation_id uuid NOT NULL, purpose text NOT NULL,
 candidate_sha256_digest text NOT NULL CHECK (candidate_sha256_digest ~ '^[0-9a-f]{64}$'),
 tests_result_sha256_digest text NOT NULL CHECK (tests_result_sha256_digest ~ '^[0-9a-f]{64}$'),
 record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
 record_json jsonb NOT NULL, evidence_reference text NOT NULL UNIQUE, recorded_at timestamptz NOT NULL,
 PRIMARY KEY (tenant_id, run_id),
 FOREIGN KEY (tenant_id, tests_execution_id) REFERENCES software_factory.tests_evidence (tenant_id, execution_id));
COMMIT;
