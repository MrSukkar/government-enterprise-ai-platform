using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlExistingArchitectureEvidenceRecorder(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IExistingArchitectureEvidenceRecorder
{
    private const string SelectSql = """
        SELECT systems_discovery_id, registration_id, architecture_sha256_digest,
               record_sha256_digest, evidence_reference, recorded_at
          FROM software_factory.existing_architecture_evidence
         WHERE tenant_id = @tenant_id AND discovery_id = @discovery_id FOR UPDATE
        """;
    private const string InsertSql = """
        INSERT INTO software_factory.existing_architecture_evidence
            (tenant_id, discovery_id, systems_discovery_id, context_discovery_id, registration_id,
             registration_version, architecture_sha256_digest, record_sha256_digest, record_json,
             evidence_reference, evidence_references, recorded_at)
        VALUES (@tenant_id, @discovery_id, @systems_discovery_id, @context_discovery_id, @registration_id,
             @registration_version, @architecture_sha256_digest, @record_sha256_digest, @record_json,
             @evidence_reference, @evidence_references, @recorded_at) ON CONFLICT DO NOTHING
        """;

    public async Task<ExistingArchitectureEvidenceReceipt> RecordAsync(
        ExistingArchitectureEvidenceRecord record, CancellationToken cancellationToken)
    {
        Validate(record);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new ExistingArchitectureDependencyUnavailableException("PostgreSQL Existing Architecture evidence is not validly configured.");
        var recordedAt = Normalize(record.DiscoveredAt);
        var persisted = record with { DiscoveredAt = recordedAt };
        var json = JsonSerializer.Serialize(persisted);
        var recordDigest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(persisted)));
        var evidenceReference = $"evidence://existing-architecture/{record.DiscoveryId:D}/sha256/{recordDigest}";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId, record.DiscoveryId, options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(record, recordDigest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            await using var command = new NpgsqlCommand(InsertSql, connection, transaction) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, record.TenantId); Add(command, "discovery_id", NpgsqlDbType.Uuid, record.DiscoveryId);
            Add(command, "systems_discovery_id", NpgsqlDbType.Uuid, record.SystemsDiscoveryId); Add(command, "context_discovery_id", NpgsqlDbType.Uuid, record.ContextDiscoveryId);
            Add(command, "registration_id", NpgsqlDbType.Uuid, record.RegistrationId); Add(command, "registration_version", NpgsqlDbType.Bigint, record.RegistrationVersion);
            Add(command, "architecture_sha256_digest", NpgsqlDbType.Text, record.ArchitectureSha256Digest); Add(command, "record_sha256_digest", NpgsqlDbType.Text, recordDigest);
            Add(command, "record_json", NpgsqlDbType.Jsonb, json); Add(command, "evidence_reference", NpgsqlDbType.Text, evidenceReference);
            Add(command, "evidence_references", NpgsqlDbType.Array | NpgsqlDbType.Text, record.EvidenceReferences.ToArray()); Add(command, "recorded_at", NpgsqlDbType.TimestampTz, recordedAt);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId, record.DiscoveryId, options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException("Existing Architecture evidence conflicted outside its tenant identity.");
                ValidateExisting(record, recordDigest, existing);
                await transaction.CommitAsync(cancellationToken); return existing.Receipt;
            }
            await transaction.CommitAsync(cancellationToken);
            return new(record.DiscoveryId, record.SystemsDiscoveryId, record.RegistrationId, record.TenantId,
                record.ArchitectureSha256Digest, evidenceReference, recordedAt);
        }
        catch (NpgsqlException) { throw new ExistingArchitectureDependencyUnavailableException("PostgreSQL Existing Architecture evidence persistence is unavailable."); }
        catch (TimeoutException) { throw new ExistingArchitectureDependencyUnavailableException("PostgreSQL Existing Architecture evidence persistence timed out."); }
    }

    private static async Task<Stored?> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string tenantId, Guid discoveryId, int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "discovery_id", NpgsqlDbType.Uuid, discoveryId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new(new(discoveryId, reader.GetGuid(0), reader.GetGuid(1), tenantId, reader.GetString(2),
            reader.GetString(4), new DateTimeOffset(reader.GetDateTime(5).ToUniversalTime())), reader.GetString(3));
    }

    private static void ValidateExisting(ExistingArchitectureEvidenceRecord record, string recordDigest, Stored stored)
    {
        var receipt = stored.Receipt;
        if (receipt.SystemsDiscoveryId != record.SystemsDiscoveryId || receipt.RegistrationId != record.RegistrationId ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.ArchitectureSha256Digest, record.ArchitectureSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(stored.RecordDigest, recordDigest) ||
            !StringComparer.Ordinal.Equals(receipt.EvidenceReference, $"evidence://existing-architecture/{record.DiscoveryId:D}/sha256/{recordDigest}"))
            throw new InvalidOperationException("Stored Existing Architecture evidence does not exactly match the authorized record.");
    }
    private static void Validate(ExistingArchitectureEvidenceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.DiscoveryId == Guid.Empty || record.SystemsDiscoveryId == Guid.Empty || record.ContextDiscoveryId == Guid.Empty ||
            record.RegistrationId == Guid.Empty || record.PolicyDecisionRequestId == Guid.Empty || record.Items.IsDefault ||
            record.EvidenceReferences.IsDefaultOrEmpty || record.DiscoveredAt == default || record.ArchitectureSha256Digest.Length != 64)
            throw new InvalidOperationException("Existing Architecture evidence payload is incomplete.");
    }
    private static DateTimeOffset Normalize(DateTimeOffset value) { var ticks = value.UtcTicks; var remainder = ticks % 10; return new(remainder == 0 ? ticks : ticks + 10 - remainder, TimeSpan.Zero); }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) => command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
    private sealed record Stored(ExistingArchitectureEvidenceReceipt Receipt, string RecordDigest);
}
