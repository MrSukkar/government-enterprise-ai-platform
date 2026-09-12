BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.evidence_completion_delivery_run_snapshots(
 tenant_id text NOT NULL,run_id uuid NOT NULL,contextualization_id uuid NOT NULL,purpose text NOT NULL,
 record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),record_json jsonb NOT NULL,
 evidence_reference text NOT NULL UNIQUE,recorded_at timestamptz NOT NULL,PRIMARY KEY(tenant_id,run_id),
 FOREIGN KEY(tenant_id,contextualization_id) REFERENCES software_factory.enterprise_model_context_evidence(tenant_id,contextualization_id));
COMMIT;
