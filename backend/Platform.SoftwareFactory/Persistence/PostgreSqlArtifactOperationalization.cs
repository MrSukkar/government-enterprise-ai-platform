using System.Collections.Immutable;
using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlAuthorizedCiCdReceiptReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IAuthorizedCiCdExecutionReceiptReader
{
    public async Task<GovernedCiCdExecutionReceipt?> LoadAsync(
        Guid executionId, string tenantId, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new ArtifactDependencyUnavailableException("CI/CD evidence is not configured.");
        const string sql = "SELECT record_json,record_sha256_digest,evidence_reference,evidence_references,recorded_at FROM software_factory.cicd_evidence WHERE tenant_id=@tenant AND execution_id=@id";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, executionId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var record = JsonSerializer.Deserialize<CiCdEvidenceRecord>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored CI/CD evidence is empty.");
            if (!StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), TestsDb.Digest(record)) ||
                !record.Result.CiCdTriggered || record.Result.SourceMutationOccurred || record.Result.ArtifactPublished ||
                record.Result.RegistryMutated || record.Result.DeploymentOccurred || record.Result.ProductionEffectOccurred ||
                record.Result.OutputManifest.IsReleasedArtifact)
                throw new InvalidOperationException("Stored CI/CD evidence binding is invalid.");
            var evidenceReference = reader.GetString(2);
            var evidence = record.EvidenceReferences.Concat(reader.GetFieldValue<string[]>(3))
                .Append(evidenceReference).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
            return new GovernedCiCdExecutionReceipt(
                executionId, record.GitOperationId, record.DeliveryRunId, tenantId,
                GovernedIntentPolicyOutcome.Permit, true, true, false, false, false, false, false,
                record.Result.RepositoryId, record.Result.CommitId, record.Result.WorkflowSha256Digest,
                record.Result.OutputManifest.ManifestSha256Digest, record.Result.Stages, evidenceReference,
                evidence, "Separately approved Artifact", new(reader.GetDateTime(4).ToUniversalTime()));
        }
        catch (NpgsqlException)
        {
            throw new ArtifactDependencyUnavailableException("CI/CD evidence is unavailable.");
        }
    }
}

public sealed class PostgreSqlAuthorizedPipelineOutputManifestReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IAuthorizedPipelineOutputManifestReader
{
    public async Task<GovernedPipelineOutputManifest?> LoadAsync(
        Guid executionId, string manifestSha256Digest, string tenantId, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new ArtifactDependencyUnavailableException("Pipeline-output evidence is not configured.");
        const string sql = "SELECT record_json,record_sha256_digest FROM software_factory.cicd_evidence WHERE tenant_id=@tenant AND execution_id=@id AND manifest_sha256_digest=@manifest";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, executionId);
            TestsDb.Add(command, "manifest", NpgsqlDbType.Text, manifestSha256Digest.ToLowerInvariant());
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var record = JsonSerializer.Deserialize<CiCdEvidenceRecord>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored pipeline-output evidence is empty.");
            var manifest = record.Result.OutputManifest;
            if (!StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), TestsDb.Digest(record)) ||
                !StringComparer.OrdinalIgnoreCase.Equals(manifest.ManifestSha256Digest, manifestSha256Digest) ||
                manifest.IsReleasedArtifact || manifest.OutputSha256Digests.IsDefaultOrEmpty ||
                manifest.EvidenceReferences.IsDefaultOrEmpty)
                throw new InvalidOperationException("Stored pipeline-output binding is invalid.");
            return manifest;
        }
        catch (NpgsqlException)
        {
            throw new ArtifactDependencyUnavailableException("Pipeline-output evidence is unavailable.");
        }
    }
}

public sealed class PostgreSqlArtifactRunReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IArtifactDeliveryRunReader
{
    public async Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new ArtifactDependencyUnavailableException("Artifact delivery-run evidence is not configured.");
        const string sql = "SELECT record_json,record_sha256_digest,evidence_reference FROM software_factory.artifact_delivery_run_snapshots WHERE tenant_id=@tenant AND run_id=@id";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
            TestsDb.Add(command, "id", NpgsqlDbType.Uuid, runId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var run = JsonSerializer.Deserialize<SoftwareDeliveryRun>(reader.GetString(0))
                ?? throw new InvalidOperationException("Stored Artifact delivery run is empty.");
            run.Validate();
            var digest = TestsDb.Digest(run);
            if (run.CurrentStage != DeliveryStage.CiCd ||
                !StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), digest) ||
                reader.GetString(2) != $"evidence://delivery-runs/{runId:D}/sha256/{digest}")
                throw new InvalidOperationException("Stored Artifact delivery-run binding is invalid.");
            return run;
        }
        catch (NpgsqlException)
        {
            throw new ArtifactDependencyUnavailableException("Artifact delivery-run evidence is unavailable.");
        }
    }
}

public sealed class PostgreSqlArtifactEvidenceRecorder(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configured) : IArtifactEvidenceRecorder
{
    private const string SelectSql = "SELECT delivery_run_id,content_sha256_digest,immutable_registry_reference,record_sha256_digest,evidence_reference,recorded_at FROM software_factory.artifact_evidence WHERE tenant_id=@tenant AND publication_id=@id FOR UPDATE";

    public async Task<ArtifactEvidenceReceipt> RecordAsync(
        ArtifactEvidenceRecord record, CancellationToken cancellationToken)
    {
        var options = configured.Value;
        if (!options.IsOperationallyConfigured)
            throw new ArtifactDependencyUnavailableException("Artifact evidence is not configured.");
        var digest = TestsDb.Digest(record);
        var evidenceReference = $"evidence://artifact/{record.PublicationId:D}/sha256/{digest}";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId, record.PublicationId,
                options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(record, digest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            const string sql = "INSERT INTO software_factory.artifact_evidence(tenant_id,publication_id,cicd_execution_id,delivery_run_id,content_sha256_digest,immutable_registry_reference,record_sha256_digest,record_json,evidence_reference,evidence_references,recorded_at) VALUES(@tenant,@id,@cicd,@run,@content,@registry,@digest,@json,@evidence,@refs,@at) ON CONFLICT DO NOTHING";
            await using var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = options.CommandTimeoutSeconds };
            foreach (var parameter in new[]
            {
                ("tenant", NpgsqlDbType.Text, (object)record.TenantId),
                ("id", NpgsqlDbType.Uuid, record.PublicationId),
                ("cicd", NpgsqlDbType.Uuid, record.CiCdExecutionId),
                ("run", NpgsqlDbType.Uuid, record.DeliveryRunId),
                ("content", NpgsqlDbType.Text, record.Publication.ContentSha256Digest.ToLowerInvariant()),
                ("registry", NpgsqlDbType.Text, record.Publication.ImmutableRegistryReference),
                ("digest", NpgsqlDbType.Text, digest),
                ("json", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(record)),
                ("evidence", NpgsqlDbType.Text, evidenceReference),
                ("refs", NpgsqlDbType.Array | NpgsqlDbType.Text, record.EvidenceReferences.ToArray()),
                ("at", NpgsqlDbType.TimestampTz, record.AuthorizedAt)
            }) TestsDb.Add(command, parameter.Item1, parameter.Item2, parameter.Item3);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId, record.PublicationId,
                    options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException("Artifact evidence identity conflict.");
                ValidateExisting(record, digest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            await transaction.CommitAsync(cancellationToken);
            return new ArtifactEvidenceReceipt(record.PublicationId, record.DeliveryRunId, record.TenantId,
                record.Publication.ContentSha256Digest, record.Publication.ImmutableRegistryReference,
                evidenceReference, record.AuthorizedAt);
        }
        catch (NpgsqlException)
        {
            throw new ArtifactDependencyUnavailableException("Artifact evidence is unavailable.");
        }
    }

    private static async Task<StoredArtifactEvidence?> LoadAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string tenantId, Guid publicationId,
        int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        TestsDb.Add(command, "tenant", NpgsqlDbType.Text, tenantId);
        TestsDb.Add(command, "id", NpgsqlDbType.Uuid, publicationId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new StoredArtifactEvidence(new ArtifactEvidenceReceipt(
            publicationId, reader.GetGuid(0), tenantId, reader.GetString(1), reader.GetString(2),
            reader.GetString(4), new(reader.GetDateTime(5).ToUniversalTime())), reader.GetString(3));
    }

    private static void ValidateExisting(ArtifactEvidenceRecord record, string digest, StoredArtifactEvidence existing)
    {
        if (existing.Receipt.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.Receipt.ContentSha256Digest, record.Publication.ContentSha256Digest) ||
            existing.Receipt.ImmutableRegistryReference != record.Publication.ImmutableRegistryReference ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.RecordDigest, digest))
            throw new InvalidOperationException("Stored Artifact evidence does not match the publication.");
    }

    private sealed record StoredArtifactEvidence(ArtifactEvidenceReceipt Receipt, string RecordDigest);
}
