using System.Collections.Immutable;
using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlAuthorizedArtifactReceiptReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IAuthorizedArtifactPublicationReceiptReader
{
    public async Task<GovernedArtifactPublicationReceipt?> LoadAsync(
        Guid publicationId, string tenantId, CancellationToken cancellationToken)
    {
        var stored = await DeploymentArtifactStore.LoadAsync(dataSource, configured.Value,
            publicationId, tenantId, cancellationToken);
        if (stored is null) return null;
        var record = stored.Record;
        var evidence = record.EvidenceReferences.Concat(stored.EvidenceReferences)
            .Append(stored.EvidenceReference).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        return new GovernedArtifactPublicationReceipt(
            publicationId, record.CiCdExecutionId, record.DeliveryRunId, tenantId,
            GovernedIntentPolicyOutcome.Permit, true, true, true, false, false, false, false,
            record.Publication.Coordinate, record.Publication.ContentSha256Digest,
            record.Publication.ImmutableRegistryReference, true, record.Verification.Verifications,
            stored.EvidenceReference, evidence, "Separately approved Deployment", stored.RecordedAt);
    }
}

public sealed class PostgreSqlAuthorizedDeploymentArtifactReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IAuthorizedDeploymentArtifactReader
{
    public async Task<GovernedVerifiedDeploymentArtifact?> LoadAsync(
        Guid publicationId, string tenantId, CancellationToken cancellationToken)
    {
        var stored = await DeploymentArtifactStore.LoadAsync(dataSource, configured.Value,
            publicationId, tenantId, cancellationToken);
        if (stored is null) return null;
        var record = stored.Record;
        var evidence = record.EvidenceReferences.Concat(stored.EvidenceReferences)
            .Append(stored.EvidenceReference).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToImmutableArray();
        return new GovernedVerifiedDeploymentArtifact(
            publicationId, record.Publication.Coordinate, record.Publication.ContentSha256Digest,
            record.Publication.ImmutableRegistryReference, record.Manifest.SbomReference,
            record.Manifest.BuildAttestationReference, record.Publication.SignatureReference,
            stored.EvidenceReference, evidence);
    }
}

internal static class DeploymentArtifactStore
{
    internal sealed record StoredArtifact(ArtifactEvidenceRecord Record, string EvidenceReference,
        ImmutableArray<string> EvidenceReferences, DateTimeOffset RecordedAt);

    internal static async Task<StoredArtifact?> LoadAsync(
        NpgsqlDataSource dataSource, PostgreSqlIntentRegistrationOptions options,
        Guid publicationId, string tenantId, CancellationToken cancellationToken)
    {
        if (!options.IsOperationallyConfigured)
            throw new DeploymentDependencyUnavailableException("Artifact evidence is not configured.");
        const string sql = "SELECT record_json,record_sha256_digest,evidence_reference,evidence_references,recorded_at FROM software_factory.artifact_evidence WHERE tenant_id=@tenant AND publication_id=@id";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, publicationId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var record = JsonSerializer.Deserialize<ArtifactEvidenceRecord>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored Artifact evidence is empty.");
            if (!StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), TestsDb.Digest(record)) ||
                !record.Publication.Published || !record.Publication.RegistryMutated ||
                !record.Publication.ImmutableCoordinate || record.Publication.ExistingCoordinateOverwritten ||
                !record.Publication.ArtifactSigned || record.Publication.DeploymentOccurred ||
                record.Publication.ProductionEffectOccurred || !record.Verification.IsVerified)
                throw new InvalidOperationException("Stored Artifact evidence binding is invalid.");
            return new StoredArtifact(record, reader.GetString(2),
                reader.GetFieldValue<string[]>(3).ToImmutableArray(),
                new(reader.GetDateTime(4).ToUniversalTime()));
        }
        catch (NpgsqlException)
        {
            throw new DeploymentDependencyUnavailableException("Artifact evidence is unavailable.");
        }
    }
}

public sealed class PostgreSqlDeploymentRunReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IDeploymentDeliveryRunReader
{
    public async Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new DeploymentDependencyUnavailableException("Deployment run evidence is not configured.");
        const string sql = "SELECT record_json,record_sha256_digest,evidence_reference FROM software_factory.deployment_delivery_run_snapshots WHERE tenant_id=@tenant AND run_id=@id";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, runId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var run = JsonSerializer.Deserialize<SoftwareDeliveryRun>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored Deployment run is empty.");
            run.Validate();
            var digest = TestsDb.Digest(run);
            if (run.CurrentStage != DeliveryStage.Artifact ||
                !StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), digest) ||
                reader.GetString(2) != $"evidence://delivery-runs/{runId:D}/sha256/{digest}")
                throw new InvalidOperationException("Stored Deployment run binding is invalid.");
            return run;
        }
        catch (NpgsqlException)
        {
            throw new DeploymentDependencyUnavailableException("Deployment run evidence is unavailable.");
        }
    }
}

public sealed class PostgreSqlSovereignDeploymentProfileReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> persistence,
    IOptions<DeploymentRuntimeOptions> deployment) : IGovernedSovereignDeploymentProfileReader
{
    public async Task<GovernedSovereignDeploymentProfile?> LoadAsync(
        Guid profileId, string version, string tenantId, CancellationToken cancellationToken)
    {
        var database = persistence.Value;
        var runtime = deployment.Value;
        if (!database.IsOperationallyConfigured || !runtime.IsOperationallyConfigured)
            throw new DeploymentDependencyUnavailableException("Sovereign Deployment profiles are not configured.");
        if (profileId != runtime.DeploymentProfileId || version != runtime.DeploymentProfileVersion)
            throw new UnauthorizedAccessException("Deployment profile is not deployment-pinned.");
        const string sql = "SELECT record_json,record_sha256_digest FROM software_factory.sovereign_deployment_profiles WHERE tenant_id=@tenant AND profile_id=@id AND version=@version";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = database.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, profileId);
            TestsDb.Add(command, "version", NpgsqlDbType.Text, version);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var profile = JsonSerializer.Deserialize<GovernedSovereignDeploymentProfile>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored Deployment profile is empty.");
            profile.Validate();
            if (!StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), TestsDb.Digest(profile)) ||
                !StringComparer.OrdinalIgnoreCase.Equals(profile.Sha256Digest, runtime.DeploymentProfileSha256Digest) ||
                profile.EnvironmentName != runtime.TargetEnvironment)
                throw new InvalidOperationException("Stored Deployment profile binding is invalid.");
            return profile;
        }
        catch (NpgsqlException)
        {
            throw new DeploymentDependencyUnavailableException("Sovereign Deployment profiles are unavailable.");
        }
    }
}

public sealed class PostgreSqlDeploymentEvidenceRecorder(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IDeploymentEvidenceRecorder
{
    private const string SelectSql = "SELECT delivery_run_id,artifact_content_sha256_digest,runtime_identity,record_sha256_digest,evidence_reference,recorded_at FROM software_factory.deployment_evidence WHERE tenant_id=@tenant AND deployment_id=@id FOR UPDATE";

    public async Task<DeploymentEvidenceReceipt> RecordAsync(
        DeploymentEvidenceRecord record, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new DeploymentDependencyUnavailableException("Deployment evidence is not configured.");
        var digest = TestsDb.Digest(record);
        var evidenceReference = $"evidence://deployment/{record.DeploymentId:D}/sha256/{digest}";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId, record.DeploymentId,
                options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(record, digest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            const string sql = "INSERT INTO software_factory.deployment_evidence(tenant_id,deployment_id,artifact_publication_id,delivery_run_id,artifact_content_sha256_digest,runtime_identity,record_sha256_digest,record_json,evidence_reference,evidence_references,recorded_at) VALUES(@tenant,@id,@artifact,@run,@content,@runtime,@digest,@json,@evidence,@refs,@at) ON CONFLICT DO NOTHING";
            await using var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = options.CommandTimeoutSeconds };
            foreach (var parameter in new[]
            {
                ("tenant", NpgsqlDbType.Text, (object)record.TenantId),
                ("id", NpgsqlDbType.Uuid, record.DeploymentId),
                ("artifact", NpgsqlDbType.Uuid, record.ArtifactPublicationId),
                ("run", NpgsqlDbType.Uuid, record.DeliveryRunId),
                ("content", NpgsqlDbType.Text, record.Result.ArtifactContentSha256Digest.ToLowerInvariant()),
                ("runtime", NpgsqlDbType.Text, record.Result.RuntimeIdentity),
                ("digest", NpgsqlDbType.Text, digest),
                ("json", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(record)),
                ("evidence", NpgsqlDbType.Text, evidenceReference),
                ("refs", NpgsqlDbType.Array | NpgsqlDbType.Text, record.EvidenceReferences.ToArray()),
                ("at", NpgsqlDbType.TimestampTz, record.AuthorizedAt)
            }) TestsDb.Add(command, parameter.Item1, parameter.Item2, parameter.Item3);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId, record.DeploymentId,
                    options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException("Deployment evidence identity conflict.");
                ValidateExisting(record, digest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            await transaction.CommitAsync(cancellationToken);
            return new DeploymentEvidenceReceipt(record.DeploymentId, record.DeliveryRunId, record.TenantId,
                record.Result.ArtifactContentSha256Digest, record.Result.RuntimeIdentity,
                evidenceReference, record.AuthorizedAt);
        }
        catch (NpgsqlException)
        {
            throw new DeploymentDependencyUnavailableException("Deployment evidence is unavailable.");
        }
    }

    private static async Task<StoredDeploymentEvidence?> LoadAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string tenantId, Guid deploymentId,
        int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
        TestsDb.Add(command, "id", NpgsqlDbType.Uuid, deploymentId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new StoredDeploymentEvidence(new DeploymentEvidenceReceipt(
            deploymentId, reader.GetGuid(0), tenantId, reader.GetString(1), reader.GetString(2),
            reader.GetString(4), new(reader.GetDateTime(5).ToUniversalTime())), reader.GetString(3));
    }

    private static void ValidateExisting(
        DeploymentEvidenceRecord record, string digest, StoredDeploymentEvidence existing)
    {
        if (existing.Receipt.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.Receipt.ArtifactContentSha256Digest,
                record.Result.ArtifactContentSha256Digest) ||
            existing.Receipt.RuntimeIdentity != record.Result.RuntimeIdentity ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.RecordDigest, digest))
            throw new InvalidOperationException("Stored Deployment evidence does not match the result.");
    }

    private sealed record StoredDeploymentEvidence(DeploymentEvidenceReceipt Receipt, string RecordDigest);
}
