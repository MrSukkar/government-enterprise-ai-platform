using System.Collections.Immutable;
using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.EnterpriseModel.Model;
using Platform.EnterpriseModel.Registration;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlAuthorizedOpenTelemetryActivationReceiptReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IAuthorizedOpenTelemetryActivationReceiptReader
{
    public async Task<GovernedOpenTelemetryActivationReceipt?> LoadAsync(Guid activationId, string tenantId, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new AutomaticRegistrationDependencyUnavailableException("OpenTelemetry evidence is not configured.");
        const string sql = "SELECT delivery_run_id,record_json,record_sha256_digest,evidence_reference,evidence_references,recorded_at FROM software_factory.opentelemetry_evidence WHERE tenant_id=@tenant AND activation_id=@id";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, activationId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var record = JsonSerializer.Deserialize<OpenTelemetryEvidenceRecord>(reader.GetString(1))
                ?? throw new InvalidOperationException("Stored OpenTelemetry evidence is empty.");
            var digest = TestsDb.Digest(record);
            var evidenceReference = reader.GetString(3);
            if (record.ActivationId != activationId || record.TenantId != tenantId ||
                record.DeliveryRunId != reader.GetGuid(0) ||
                !StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(2), digest) ||
                evidenceReference != $"evidence://opentelemetry/{activationId:D}/sha256/{digest}")
                throw new InvalidOperationException("Stored OpenTelemetry evidence binding is invalid.");
            var result = record.Result;
            var evidence = record.EvidenceReferences.Concat(reader.GetFieldValue<string[]>(4)).Append(evidenceReference)
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
            return new GovernedOpenTelemetryActivationReceipt(activationId, record.DeploymentId,
                record.DeliveryRunId, tenantId, GovernedIntentPolicyOutcome.Permit, true,
                result.ConfigurationApplied, result.ExternalEffectOccurred, result.ProductionEffectOccurred,
                result.AutomaticRegistrationOccurred, result.EnterpriseModelMutated, false,
                result.RuntimeIdentity, result.ArtifactContentSha256Digest,
                result.TelemetryProfileSha256Digest, result.ServiceName, result.ServiceVersion,
                result.Signals, evidenceReference, evidence, "Separately approved Automatic Registration",
                new(reader.GetDateTime(5).ToUniversalTime()));
        }
        catch (NpgsqlException)
        {
            throw new AutomaticRegistrationDependencyUnavailableException("OpenTelemetry evidence is unavailable.");
        }
    }
}

public sealed class PostgreSqlAutomaticRegistrationRunReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IAutomaticRegistrationDeliveryRunReader
{
    public async Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new AutomaticRegistrationDependencyUnavailableException("Automatic Registration run evidence is not configured.");
        const string sql = "SELECT record_json,record_sha256_digest,evidence_reference FROM software_factory.automatic_registration_delivery_run_snapshots WHERE tenant_id=@tenant AND run_id=@id";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, runId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var run = JsonSerializer.Deserialize<SoftwareDeliveryRun>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored Automatic Registration run is empty.");
            run.Validate();
            var digest = TestsDb.Digest(run);
            if (run.CurrentStage != DeliveryStage.OpenTelemetry ||
                !StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), digest) ||
                reader.GetString(2) != $"evidence://delivery-runs/{runId:D}/sha256/{digest}")
                throw new InvalidOperationException("Stored Automatic Registration run binding is invalid.");
            return run;
        }
        catch (NpgsqlException)
        {
            throw new AutomaticRegistrationDependencyUnavailableException("Automatic Registration run evidence is unavailable.");
        }
    }
}

public sealed class PostgreSqlAutomaticRegistrationManifestReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> persistence,
    IOptions<AutomaticRegistrationRuntimeOptions> registration) : IGovernedAutomaticRegistrationManifestReader
{
    public async Task<GovernedAutomaticRegistrationManifest?> LoadAsync(Guid manifestId, string version, string tenantId, CancellationToken cancellationToken)
    {
        var database = persistence.Value;
        var runtime = registration.Value;
        if (!database.IsOperationallyConfigured || !runtime.IsOperationallyConfigured)
            throw new AutomaticRegistrationDependencyUnavailableException("Automatic Registration manifests are not configured.");
        if (manifestId != runtime.ManifestId || version != runtime.ManifestVersion)
            throw new UnauthorizedAccessException("Automatic Registration manifest is not deployment-pinned.");
        const string sql = "SELECT record_json,record_sha256_digest FROM software_factory.automatic_registration_manifests WHERE tenant_id=@tenant AND manifest_id=@id AND version=@version";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = database.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, manifestId);
            TestsDb.Add(command, "version", NpgsqlDbType.Text, version);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var manifest = JsonSerializer.Deserialize<GovernedAutomaticRegistrationManifest>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored Automatic Registration manifest is empty.");
            manifest.Validate();
            if (!StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), TestsDb.Digest(manifest)) ||
                !StringComparer.OrdinalIgnoreCase.Equals(manifest.Sha256Digest, runtime.ManifestSha256Digest) ||
                manifest.RuntimeIdentity != runtime.RuntimeIdentity || manifest.ServiceIdentity != runtime.ServiceIdentity ||
                manifest.ServiceVersion != runtime.ServiceVersion ||
                !StringComparer.OrdinalIgnoreCase.Equals(manifest.ArtifactDigest, runtime.ArtifactDigest))
                throw new InvalidOperationException("Stored Automatic Registration manifest binding is invalid.");
            return manifest;
        }
        catch (NpgsqlException)
        {
            throw new AutomaticRegistrationDependencyUnavailableException("Automatic Registration manifests are unavailable.");
        }
    }
}

public sealed class PostgreSqlAutomaticRegistrationRepository(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IAutomaticRegistrationRepository
{
    private const string SelectSql = "SELECT request_id,request_fingerprint,enterprise_object_json,evidence_reference,committed_at FROM software_factory.automatic_registrations WHERE tenant_id=@tenant AND environment_name=@environment AND service_identity=@service FOR UPDATE";

    public async Task<AutomaticRegistrationCommit> RegisterAtomicallyAsync(AutomaticRegistrationProposal proposal, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new AutomaticRegistrationDependencyUnavailableException("Automatic Registration repository is not configured.");
        proposal.Key.Validate(); proposal.Candidate.Validate();
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var stored = await LoadAsync(connection, transaction, proposal.Key, options.CommandTimeoutSeconds, cancellationToken);
            if (stored is not null && StringComparer.Ordinal.Equals(stored.RequestFingerprint, proposal.RequestFingerprint))
            {
                await transaction.CommitAsync(cancellationToken);
                return stored with { RequestId = proposal.RequestId, Disposition = RegistrationDisposition.Unchanged };
            }
            var disposition = stored is null ? RegistrationDisposition.Created : RegistrationDisposition.Updated;
            var candidate = stored is null ? proposal.Candidate : proposal.Candidate with
            {
                Id = stored.EnterpriseObject.Id,
                CreatedAt = stored.EnterpriseObject.CreatedAt,
                UpdatedAt = proposal.ProposedAt
            };
            var evidenceReference = $"evidence://automatic-registration/commit/{proposal.RequestId:D}/sha256/{proposal.RequestFingerprint[7..]}";
            const string sql = "INSERT INTO software_factory.automatic_registrations(tenant_id,environment_name,service_identity,request_id,request_fingerprint,enterprise_object_id,enterprise_object_json,evidence_reference,committed_at) VALUES(@tenant,@environment,@service,@request,@fingerprint,@object,@json,@evidence,@at) ON CONFLICT(tenant_id,environment_name,service_identity) DO UPDATE SET request_id=EXCLUDED.request_id,request_fingerprint=EXCLUDED.request_fingerprint,enterprise_object_id=EXCLUDED.enterprise_object_id,enterprise_object_json=EXCLUDED.enterprise_object_json,evidence_reference=EXCLUDED.evidence_reference,committed_at=EXCLUDED.committed_at";
            await using var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = options.CommandTimeoutSeconds };
            foreach (var value in new[]
            {
                ("tenant", NpgsqlDbType.Text, (object)proposal.Key.TenantId),
                ("environment", NpgsqlDbType.Text, proposal.Key.EnvironmentName),
                ("service", NpgsqlDbType.Text, proposal.Key.ServiceIdentity),
                ("request", NpgsqlDbType.Uuid, proposal.RequestId),
                ("fingerprint", NpgsqlDbType.Text, proposal.RequestFingerprint),
                ("object", NpgsqlDbType.Uuid, candidate.Id.Value),
                ("json", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(candidate)),
                ("evidence", NpgsqlDbType.Text, evidenceReference),
                ("at", NpgsqlDbType.TimestampTz, proposal.ProposedAt)
            }) TestsDb.Add(command, value.Item1, value.Item2, value.Item3);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new AutomaticRegistrationCommit(proposal.RequestId, proposal.Key,
                proposal.RequestFingerprint, disposition, candidate, evidenceReference, proposal.ProposedAt);
        }
        catch (NpgsqlException)
        {
            throw new AutomaticRegistrationDependencyUnavailableException("Automatic Registration repository is unavailable.");
        }
    }

    private static async Task<AutomaticRegistrationCommit?> LoadAsync(NpgsqlConnection connection,
        NpgsqlTransaction transaction, AutomaticRegistrationKey key, int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        TestsDb.Add(command, "tenant", NpgsqlDbType.Text, key.TenantId);
        TestsDb.Add(command, "environment", NpgsqlDbType.Text, key.EnvironmentName);
        TestsDb.Add(command, "service", NpgsqlDbType.Text, key.ServiceIdentity);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var value = JsonSerializer.Deserialize<EnterpriseObject>(reader.GetString(2))
            ?? throw new InvalidOperationException("Stored registered Enterprise Object is empty.");
        value.Validate();
        return new AutomaticRegistrationCommit(reader.GetGuid(0), key, reader.GetString(1),
            RegistrationDisposition.Unchanged, value, reader.GetString(3),
            new(reader.GetDateTime(4).ToUniversalTime()));
    }
}

public sealed class PostgreSqlAutomaticRegistrationEvidenceRecorder(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IAutomaticRegistrationEvidenceRecorder
{
    private const string SelectSql = "SELECT delivery_run_id,request_fingerprint,record_sha256_digest,evidence_reference,recorded_at FROM software_factory.automatic_registration_evidence WHERE tenant_id=@tenant AND registration_id=@id FOR UPDATE";

    public async Task<AutomaticRegistrationEvidenceReceipt> RecordAsync(AutomaticRegistrationEvidenceRecord record, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new AutomaticRegistrationDependencyUnavailableException("Automatic Registration evidence is not configured.");
        var digest = TestsDb.Digest(record);
        var evidenceReference = $"evidence://automatic-registration/{record.RegistrationId:D}/sha256/{digest}";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId, record.RegistrationId, options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(record, digest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            const string sql = "INSERT INTO software_factory.automatic_registration_evidence(tenant_id,registration_id,activation_id,delivery_run_id,request_fingerprint,enterprise_object_id,record_sha256_digest,record_json,evidence_reference,evidence_references,recorded_at) VALUES(@tenant,@id,@activation,@run,@fingerprint,@object,@digest,@json,@evidence,@refs,@at) ON CONFLICT DO NOTHING";
            await using var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = options.CommandTimeoutSeconds };
            foreach (var value in new[]
            {
                ("tenant", NpgsqlDbType.Text, (object)record.TenantId), ("id", NpgsqlDbType.Uuid, record.RegistrationId),
                ("activation", NpgsqlDbType.Uuid, record.ActivationId), ("run", NpgsqlDbType.Uuid, record.DeliveryRunId),
                ("fingerprint", NpgsqlDbType.Text, record.Commit.RequestFingerprint),
                ("object", NpgsqlDbType.Uuid, record.Commit.EnterpriseObject.Id.Value), ("digest", NpgsqlDbType.Text, digest),
                ("json", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(record)), ("evidence", NpgsqlDbType.Text, evidenceReference),
                ("refs", NpgsqlDbType.Array | NpgsqlDbType.Text, record.EvidenceReferences.ToArray()),
                ("at", NpgsqlDbType.TimestampTz, record.AuthorizedAt)
            }) TestsDb.Add(command, value.Item1, value.Item2, value.Item3);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId, record.RegistrationId, options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException("Automatic Registration evidence identity conflict.");
                ValidateExisting(record, digest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            await transaction.CommitAsync(cancellationToken);
            return new AutomaticRegistrationEvidenceReceipt(record.RegistrationId, record.DeliveryRunId,
                record.TenantId, record.Commit.RequestFingerprint, evidenceReference, record.AuthorizedAt);
        }
        catch (NpgsqlException)
        {
            throw new AutomaticRegistrationDependencyUnavailableException("Automatic Registration evidence is unavailable.");
        }
    }

    private static async Task<StoredEvidence?> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string tenantId, Guid registrationId, int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId); TestsDb.Add(command, "id", NpgsqlDbType.Uuid, registrationId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new StoredEvidence(new AutomaticRegistrationEvidenceReceipt(registrationId, reader.GetGuid(0), tenantId,
            reader.GetString(1), reader.GetString(3), new(reader.GetDateTime(4).ToUniversalTime())), reader.GetString(2));
    }

    private static void ValidateExisting(AutomaticRegistrationEvidenceRecord record, string digest, StoredEvidence existing)
    {
        if (existing.Receipt.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.Ordinal.Equals(existing.Receipt.RequestFingerprint, record.Commit.RequestFingerprint) ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.RecordDigest, digest))
            throw new InvalidOperationException("Stored Automatic Registration evidence does not match the result.");
    }

    private sealed record StoredEvidence(AutomaticRegistrationEvidenceReceipt Receipt, string RecordDigest);
}
