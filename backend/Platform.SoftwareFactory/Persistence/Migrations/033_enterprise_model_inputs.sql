BEGIN;
ALTER TABLE software_factory.automatic_registrations ADD COLUMN IF NOT EXISTS enterprise_object_id uuid;
CREATE UNIQUE INDEX IF NOT EXISTS ux_automatic_registrations_object ON software_factory.automatic_registrations(tenant_id,enterprise_object_id);
CREATE TABLE IF NOT EXISTS software_factory.enterprise_model_delivery_run_snapshots(
 tenant_id text NOT NULL,run_id uuid NOT NULL,registration_id uuid NOT NULL,purpose text NOT NULL,
 record_sha256_digest text NOT NULL CHECK(record_sha256_digest ~ '^[0-9a-f]{64}$'),record_json jsonb NOT NULL,
 evidence_reference text NOT NULL UNIQUE,recorded_at timestamptz NOT NULL,PRIMARY KEY(tenant_id,run_id),
 FOREIGN KEY(tenant_id,registration_id) REFERENCES software_factory.automatic_registration_evidence(tenant_id,registration_id));
COMMIT;
