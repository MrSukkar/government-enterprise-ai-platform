BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.git_evidence (
 tenant_id text NOT NULL,operation_id uuid NOT NULL,review_id uuid NOT NULL,tests_execution_id uuid NOT NULL,generation_id uuid NOT NULL,delivery_run_id uuid NOT NULL,
 commit_id text NOT NULL,change_set_sha256_digest text NOT NULL CHECK(change_set_sha256_digest ~ '^[0-9a-f]{64}$'),record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
 record_json jsonb NOT NULL,evidence_reference text NOT NULL UNIQUE,evidence_references text[] NOT NULL,recorded_at timestamptz NOT NULL,
 PRIMARY KEY(tenant_id,operation_id),FOREIGN KEY(tenant_id,review_id) REFERENCES software_factory.human_review_evidence(tenant_id,review_id),FOREIGN KEY(tenant_id,delivery_run_id) REFERENCES software_factory.git_delivery_run_snapshots(tenant_id,run_id));
COMMIT;
