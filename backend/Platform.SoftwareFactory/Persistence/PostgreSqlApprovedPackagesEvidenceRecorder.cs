using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlApprovedPackagesEvidenceRecorder(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IApprovedPackagesEvidenceRecorder
{
    private const string SelectSql = """
        SELECT architecture_discovery_id, registration_id, selection_sha256_digest,
               record_sha256_digest, evidence_reference, recorded_at
          FROM software_factory.approved_packages_evidence
         WHERE tenant_id = @tenant_id AND selection_id = @selection_id FOR UPDATE
        """;
    private const string InsertSql = """
        INSERT INTO software_factory.approved_packages_evidence
            (tenant_id, selection_id, architecture_discovery_id, registration_id,
             registration_version, selection_sha256_digest, record_sha256_digest,
             record_json, evidence_reference, evidence_references, recorded_at)
        VALUES (@tenant_id, @selection_id, @architecture_discovery_id, @registration_id,
             @registration_version, @selection_sha256_digest, @record_sha256_digest,
             @record_json, @evidence_reference, @evidence_references, @recorded_at)
        ON CONFLICT DO NOTHING
        """;

    public async Task<ApprovedPackagesEvidenceReceipt> RecordAsync(
        ApprovedPackagesEvidenceRecord record, CancellationToken cancellationToken)
    {
        Validate(record); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new ApprovedPackagesDependencyUnavailableException("PostgreSQL Approved Packages evidence is not configured.");
        var recordedAt = Normalize(record.SelectedAt); var persisted = record with { SelectedAt = recordedAt };
        var json = JsonSerializer.Serialize(persisted);
        var recordDigest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(persisted)));
        var evidenceReference = $"evidence://approved-packages/{record.SelectionId:D}/sha256/{recordDigest}";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId, record.SelectionId, options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(record, recordDigest, existing); await transaction.CommitAsync(cancellationToken); return existing.Receipt;
            }
            await using var command = new NpgsqlCommand(InsertSql, connection, transaction) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, record.TenantId); Add(command, "selection_id", NpgsqlDbType.Uuid, record.SelectionId);
            Add(command, "architecture_discovery_id", NpgsqlDbType.Uuid, record.ArchitectureDiscoveryId); Add(command, "registration_id", NpgsqlDbType.Uuid, record.RegistrationId);
            Add(command, "registration_version", NpgsqlDbType.Bigint, record.RegistrationVersion); Add(command, "selection_sha256_digest", NpgsqlDbType.Text, record.SelectionSha256Digest);
            Add(command, "record_sha256_digest", NpgsqlDbType.Text, recordDigest); Add(command, "record_json", NpgsqlDbType.Jsonb, json);
            Add(command, "evidence_reference", NpgsqlDbType.Text, evidenceReference); Add(command, "evidence_references", NpgsqlDbType.Array | NpgsqlDbType.Text, record.EvidenceReferences.ToArray());
            Add(command, "recorded_at", NpgsqlDbType.TimestampTz, recordedAt);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId, record.SelectionId, options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException("Approved Packages evidence conflicted outside its tenant identity.");
                ValidateExisting(record, recordDigest, existing); await transaction.CommitAsync(cancellationToken); return existing.Receipt;
            }
            await transaction.CommitAsync(cancellationToken);
            return new(record.SelectionId, record.ArchitectureDiscoveryId, record.RegistrationId, record.TenantId,
                record.SelectionSha256Digest, evidenceReference, recordedAt);
        }
        catch (NpgsqlException) { throw new ApprovedPackagesDependencyUnavailableException("PostgreSQL Approved Packages evidence persistence is unavailable."); }
        catch (TimeoutException) { throw new ApprovedPackagesDependencyUnavailableException("PostgreSQL Approved Packages evidence persistence timed out."); }
    }
    private static async Task<Stored?> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string tenantId, Guid selectionId, int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "selection_id", NpgsqlDbType.Uuid, selectionId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new(new(selectionId, reader.GetGuid(0), reader.GetGuid(1), tenantId, reader.GetString(2), reader.GetString(4),
            new DateTimeOffset(reader.GetDateTime(5).ToUniversalTime())), reader.GetString(3));
    }
    private static void ValidateExisting(ApprovedPackagesEvidenceRecord record, string recordDigest, Stored stored)
    {
        var receipt = stored.Receipt;
        if (receipt.ArchitectureDiscoveryId != record.ArchitectureDiscoveryId || receipt.RegistrationId != record.RegistrationId ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.SelectionSha256Digest, record.SelectionSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(stored.RecordDigest, recordDigest) ||
            !StringComparer.Ordinal.Equals(receipt.EvidenceReference, $"evidence://approved-packages/{record.SelectionId:D}/sha256/{recordDigest}"))
            throw new InvalidOperationException("Stored Approved Packages evidence does not match the authorized record.");
    }
    private static void Validate(ApprovedPackagesEvidenceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.SelectionId == Guid.Empty || record.ArchitectureDiscoveryId == Guid.Empty || record.RegistrationId == Guid.Empty ||
            record.RegistrationVersion < 0 || record.PolicyDecisionRequestId == Guid.Empty || record.Packages.IsDefaultOrEmpty ||
            record.EvidenceReferences.IsDefaultOrEmpty || record.SelectedAt == default || record.SelectionSha256Digest.Length != 64)
            throw new InvalidOperationException("Approved Packages evidence payload is incomplete.");
    }
    private static DateTimeOffset Normalize(DateTimeOffset value) { var ticks = value.UtcTicks; var remainder = ticks % 10; return new(remainder == 0 ? ticks : ticks + 10 - remainder, TimeSpan.Zero); }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) => command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
    private sealed record Stored(ApprovedPackagesEvidenceReceipt Receipt, string RecordDigest);
}
