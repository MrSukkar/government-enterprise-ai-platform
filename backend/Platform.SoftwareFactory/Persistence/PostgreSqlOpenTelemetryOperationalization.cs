using System.Collections.Immutable;
using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlAuthorizedDeploymentReceiptReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IAuthorizedSovereignDeploymentReceiptReader
{
    public async Task<GovernedSovereignDeploymentReceipt?> LoadAsync(
        Guid deploymentId, string tenantId, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new OpenTelemetryDependencyUnavailableException("Deployment evidence is not configured.");
        const string sql = "SELECT record_json,record_sha256_digest,evidence_reference,evidence_references,recorded_at FROM software_factory.deployment_evidence WHERE tenant_id=@tenant AND deployment_id=@id";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, deploymentId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var record = JsonSerializer.Deserialize<DeploymentEvidenceRecord>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored Deployment evidence is empty.");
            if (!StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), TestsDb.Digest(record)) ||
                !record.Result.DeploymentOccurred || !record.Result.ExternalEffectOccurred ||
                !record.Result.ActivationVerified || !record.Result.IdempotencyVerified ||
                record.Result.TelemetryConfigured || record.Result.AutomaticRegistrationOccurred ||
                record.Result.EnterpriseModelMutated)
                throw new InvalidOperationException("Stored Deployment evidence binding is invalid.");
            var evidenceReference = reader.GetString(2);
            var evidence = record.EvidenceReferences.Concat(reader.GetFieldValue<string[]>(3))
                .Append(evidenceReference).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
            return new GovernedSovereignDeploymentReceipt(
                deploymentId, record.ArtifactPublicationId, record.DeliveryRunId, tenantId,
                GovernedIntentPolicyOutcome.Permit, true, true, true, record.Result.ProductionEffectOccurred,
                false, false, false, false, record.Result.ArtifactContentSha256Digest,
                record.Profile.ProfileId, record.Result.TargetEnvironment, record.Result.RuntimeIdentity,
                record.Result.RollbackReference, evidenceReference, evidence,
                "Separately approved OpenTelemetry", new(reader.GetDateTime(4).ToUniversalTime()));
        }
        catch (NpgsqlException)
        {
            throw new OpenTelemetryDependencyUnavailableException("Deployment evidence is unavailable.");
        }
    }
}

public sealed class PostgreSqlOpenTelemetryRunReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IOpenTelemetryDeliveryRunReader
{
    public async Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry run evidence is not configured.");
        const string sql = "SELECT record_json,record_sha256_digest,evidence_reference FROM software_factory.opentelemetry_delivery_run_snapshots WHERE tenant_id=@tenant AND run_id=@id";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, runId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var run = JsonSerializer.Deserialize<SoftwareDeliveryRun>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored OpenTelemetry run is empty.");
            run.Validate();
            var digest = TestsDb.Digest(run);
            if (run.CurrentStage != DeliveryStage.Deployment ||
                !StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), digest) ||
                reader.GetString(2) != $"evidence://delivery-runs/{runId:D}/sha256/{digest}")
                throw new InvalidOperationException("Stored OpenTelemetry run binding is invalid.");
            return run;
        }
        catch (NpgsqlException)
        {
            throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry run evidence is unavailable.");
        }
    }
}

public sealed class PostgreSqlOpenTelemetryProfileReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> persistence,
    IOptions<OpenTelemetryRuntimeOptions> telemetry) : IGovernedOpenTelemetryProfileReader
{
    public async Task<GovernedOpenTelemetryProfile?> LoadAsync(
        Guid profileId, string version, string tenantId, CancellationToken cancellationToken)
    {
        var database = persistence.Value;
        var runtime = telemetry.Value;
        if (!database.IsOperationallyConfigured || !runtime.IsOperationallyConfigured)
            throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry profiles are not configured.");
        if (profileId != runtime.TelemetryProfileId || version != runtime.TelemetryProfileVersion)
            throw new UnauthorizedAccessException("OpenTelemetry profile is not deployment-pinned.");
        const string sql = "SELECT record_json,record_sha256_digest FROM software_factory.opentelemetry_profiles WHERE tenant_id=@tenant AND profile_id=@id AND version=@version";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = database.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, profileId);
            TestsDb.Add(command, "version", NpgsqlDbType.Text, version);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var profile = JsonSerializer.Deserialize<GovernedOpenTelemetryProfile>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored OpenTelemetry profile is empty.");
            profile.Validate();
            if (!StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), TestsDb.Digest(profile)) ||
                !StringComparer.OrdinalIgnoreCase.Equals(profile.Sha256Digest, runtime.TelemetryProfileSha256Digest) ||
                profile.ServiceName != runtime.ServiceName || profile.ServiceVersion != runtime.ServiceVersion ||
                profile.CollectorAgentEndpoint.AbsoluteUri != runtime.CollectorAgentEndpoint ||
                profile.CollectorGatewayEndpoint.AbsoluteUri != runtime.CollectorGatewayEndpoint ||
                profile.TrustAnchorReference != runtime.TrustAnchorReference ||
                profile.RedactionPolicyReference != runtime.RedactionPolicyReference ||
                !StringComparer.OrdinalIgnoreCase.Equals(profile.RedactionPolicySha256Digest, runtime.RedactionPolicySha256Digest))
                throw new InvalidOperationException("Stored OpenTelemetry profile binding is invalid.");
            return profile;
        }
        catch (NpgsqlException)
        {
            throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry profiles are unavailable.");
        }
    }
}

public sealed class PostgreSqlOpenTelemetryEvidenceRecorder(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IOpenTelemetryEvidenceRecorder
{
    private const string SelectSql = "SELECT delivery_run_id,telemetry_profile_sha256_digest,record_sha256_digest,evidence_reference,recorded_at FROM software_factory.opentelemetry_evidence WHERE tenant_id=@tenant AND activation_id=@id FOR UPDATE";

    public async Task<OpenTelemetryEvidenceReceipt> RecordAsync(
        OpenTelemetryEvidenceRecord record, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry evidence is not configured.");
        var digest = TestsDb.Digest(record);
        var evidenceReference = $"evidence://opentelemetry/{record.ActivationId:D}/sha256/{digest}";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId, record.ActivationId,
                options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(record, digest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            const string sql = "INSERT INTO software_factory.opentelemetry_evidence(tenant_id,activation_id,deployment_id,delivery_run_id,telemetry_profile_sha256_digest,record_sha256_digest,record_json,evidence_reference,evidence_references,recorded_at) VALUES(@tenant,@id,@deployment,@run,@profile,@digest,@json,@evidence,@refs,@at) ON CONFLICT DO NOTHING";
            await using var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = options.CommandTimeoutSeconds };
            foreach (var parameter in new[]
            {
                ("tenant", NpgsqlDbType.Text, (object)record.TenantId),
                ("id", NpgsqlDbType.Uuid, record.ActivationId),
                ("deployment", NpgsqlDbType.Uuid, record.DeploymentId),
                ("run", NpgsqlDbType.Uuid, record.DeliveryRunId),
                ("profile", NpgsqlDbType.Text, record.Profile.Sha256Digest.ToLowerInvariant()),
                ("digest", NpgsqlDbType.Text, digest),
                ("json", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(record)),
                ("evidence", NpgsqlDbType.Text, evidenceReference),
                ("refs", NpgsqlDbType.Array | NpgsqlDbType.Text, record.EvidenceReferences.ToArray()),
                ("at", NpgsqlDbType.TimestampTz, record.AuthorizedAt)
            }) TestsDb.Add(command, parameter.Item1, parameter.Item2, parameter.Item3);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId, record.ActivationId,
                    options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException("OpenTelemetry evidence identity conflict.");
                ValidateExisting(record, digest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            await transaction.CommitAsync(cancellationToken);
            return new OpenTelemetryEvidenceReceipt(record.ActivationId, record.DeliveryRunId,
                record.TenantId, record.Profile.Sha256Digest, evidenceReference, record.AuthorizedAt);
        }
        catch (NpgsqlException)
        {
            throw new OpenTelemetryDependencyUnavailableException("OpenTelemetry evidence is unavailable.");
        }
    }

    private static async Task<StoredTelemetryEvidence?> LoadAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string tenantId, Guid activationId,
        int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
        TestsDb.Add(command, "id", NpgsqlDbType.Uuid, activationId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new StoredTelemetryEvidence(new OpenTelemetryEvidenceReceipt(
            activationId, reader.GetGuid(0), tenantId, reader.GetString(1), reader.GetString(3),
            new(reader.GetDateTime(4).ToUniversalTime())), reader.GetString(2));
    }

    private static void ValidateExisting(
        OpenTelemetryEvidenceRecord record, string digest, StoredTelemetryEvidence existing)
    {
        if (existing.Receipt.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.Receipt.TelemetryProfileSha256Digest, record.Profile.Sha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.RecordDigest, digest))
            throw new InvalidOperationException("Stored OpenTelemetry evidence does not match the result.");
    }

    private sealed record StoredTelemetryEvidence(OpenTelemetryEvidenceReceipt Receipt, string RecordDigest);
}
