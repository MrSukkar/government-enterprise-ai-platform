BEGIN;

CREATE TABLE IF NOT EXISTS software_factory.institutional_package_catalog
(
    package_kind text NOT NULL CHECK (btrim(package_kind) <> ''),
    package_name text NOT NULL CHECK (btrim(package_name) <> ''),
    package_version text NOT NULL CHECK (btrim(package_version) <> ''),
    content_digest text NOT NULL CHECK (content_digest ~ '^sha256:[0-9A-Fa-f]{64}$'),
    allowed_tenant_ids text[] NOT NULL CHECK (cardinality(allowed_tenant_ids) > 0),
    allowed_environments text[] NOT NULL CHECK (cardinality(allowed_environments) > 0),
    record_sha256_digest text NOT NULL CHECK (record_sha256_digest ~ '^[0-9a-f]{64}$'),
    record_json jsonb NOT NULL,
    recorded_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_institutional_package_catalog PRIMARY KEY
        (package_kind, package_name, package_version, content_digest)
);

COMMIT;
