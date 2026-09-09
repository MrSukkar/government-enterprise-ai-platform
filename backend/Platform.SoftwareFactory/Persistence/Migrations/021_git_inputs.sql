BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.git_delivery_run_snapshots (
 tenant_id text NOT NULL,run_id uuid NOT NULL,review_id uuid NOT NULL,tests_execution_id uuid NOT NULL,generation_id uuid NOT NULL,purpose text NOT NULL,
 candidate_sha256_digest text NOT NULL CHECK(candidate_sha256_digest ~ '^[0-9a-f]{64}$'),review_package_sha256_digest text NOT NULL CHECK(review_package_sha256_digest ~ '^[0-9a-f]{64}$'),
 record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),record_json jsonb NOT NULL,evidence_reference text NOT NULL UNIQUE,recorded_at timestamptz NOT NULL,
 PRIMARY KEY(tenant_id,run_id),FOREIGN KEY(tenant_id,review_id) REFERENCES software_factory.human_review_evidence(tenant_id,review_id));
COMMIT;
