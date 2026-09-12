BEGIN;
CREATE TABLE IF NOT EXISTS software_factory.enterprise_model_context_evidence(
 tenant_id text NOT NULL,contextualization_id uuid NOT NULL,registration_id uuid NOT NULL,delivery_run_id uuid NOT NULL,
 enterprise_object_id uuid NOT NULL,record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),
 record_json jsonb NOT NULL,evidence_reference text NOT NULL UNIQUE,evidence_references text[] NOT NULL,recorded_at timestamptz NOT NULL,
 PRIMARY KEY(tenant_id,contextualization_id),FOREIGN KEY(tenant_id,registration_id) REFERENCES software_factory.automatic_registration_evidence(tenant_id,registration_id),
 FOREIGN KEY(tenant_id,delivery_run_id) REFERENCES software_factory.enterprise_model_delivery_run_snapshots(tenant_id,run_id));
COMMIT;
