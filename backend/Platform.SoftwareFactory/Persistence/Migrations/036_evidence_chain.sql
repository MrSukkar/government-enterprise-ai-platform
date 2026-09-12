BEGIN;
CREATE SCHEMA IF NOT EXISTS evidence;
CREATE TABLE IF NOT EXISTS evidence.chain_entries(
 tenant_id text NOT NULL,chain_id uuid NOT NULL,sequence bigint NOT NULL CHECK(sequence>=0),stage text NOT NULL,
 classification text NOT NULL,entry_sha256_digest text NOT NULL CHECK(entry_sha256_digest ~ '^[0-9a-f]{64}$'),
 record_json jsonb NOT NULL,occurred_at timestamptz NOT NULL,PRIMARY KEY(tenant_id,chain_id,sequence),
 UNIQUE(tenant_id,chain_id,entry_sha256_digest));
REVOKE UPDATE,DELETE,TRUNCATE ON evidence.chain_entries FROM PUBLIC;
COMMIT;
