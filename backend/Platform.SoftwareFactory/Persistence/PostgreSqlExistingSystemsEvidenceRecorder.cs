using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlExistingSystemsEvidenceRecorder(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IExistingSystemsEvidenceRecorder
{
    private const string SelectSql = """
        SELECT context_discovery_id, registration_id, inventory_sha256_digest,
               record_sha256_digest, evidence_reference, recorded_at
          FROM software_factory.existing_systems_evidence
         WHERE tenant_id = @tenant_id AND discovery_id = @discovery_id
         FOR UPDATE
        """;
    private const string InsertSql = """
        INSERT INTO software_factory.existing_systems_evidence
            (tenant_id, discovery_id, context_discovery_id, registration_id,
             registration_version, inventory_sha256_digest, record_sha256_digest,
             record_json, evidence_reference, evidence_references, recorded_at)
        VALUES
            (@tenant_id, @discovery_id, @context_discovery_id, @registration_id,
             @registration_version, @inventory_sha256_digest, @record_sha256_digest,
             @record_json, @evidence_reference, @evidence_references, @recorded_at)
        ON CONFLICT DO NOTHING
        """;

    public async Task<ExistingSystemsEvidenceReceipt> RecordAsync(
        ExistingSystemsEvidenceRecord record,
        CancellationToken cancellationToken)
    {
        Validate(record);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new ExistingSystemsDependencyUnavailableException(
                "PostgreSQL Existing Systems evidence is not validly configured.");
        var recordedAt = NormalizePostgreSqlTimestamp(record.DiscoveredAt);
        var persistedRecord = record with { DiscoveredAt = recordedAt };
        var recordJson = JsonSerializer.Serialize(persistedRecord);
        var recordDigest = Convert.ToHexStringLower(
            SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(persistedRecord)));
        var evidenceReference =
            $"evidence://existing-systems/{record.DiscoveryId:D}/sha256/{recordDigest}";

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId,
                record.DiscoveryId, options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(record, recordDigest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }

            await using var command = new NpgsqlCommand(InsertSql, connection, transaction)
            {
                CommandTimeout = options.CommandTimeoutSeconds
            };
            Add(command, "tenant_id", NpgsqlDbType.Text, record.TenantId);
            Add(command, "discovery_id", NpgsqlDbType.Uuid, record.DiscoveryId);
            Add(command, "context_discovery_id", NpgsqlDbType.Uuid, record.ContextDiscoveryId);
            Add(command, "registration_id", NpgsqlDbType.Uuid, record.RegistrationId);
            Add(command, "registration_version", NpgsqlDbType.Bigint, record.RegistrationVersion);
            Add(command, "inventory_sha256_digest", NpgsqlDbType.Text, record.InventorySha256Digest);
            Add(command, "record_sha256_digest", NpgsqlDbType.Text, recordDigest);
            Add(command, "record_json", NpgsqlDbType.Jsonb, recordJson);
            Add(command, "evidence_reference", NpgsqlDbType.Text, evidenceReference);
            Add(command, "evidence_references", NpgsqlDbType.Array | NpgsqlDbType.Text,
                record.EvidenceReferences.ToArray());
            Add(command, "recorded_at", NpgsqlDbType.TimestampTz, recordedAt);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId,
                    record.DiscoveryId, options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException(
                        "Existing Systems evidence conflicted outside its tenant-scoped identity.");
                ValidateExisting(record, recordDigest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }

            await transaction.CommitAsync(cancellationToken);
            return new ExistingSystemsEvidenceReceipt(record.DiscoveryId,
                record.ContextDiscoveryId, record.RegistrationId, record.TenantId,
                record.InventorySha256Digest, evidenceReference, recordedAt);
        }
        catch (NpgsqlException)
        {
            throw new ExistingSystemsDependencyUnavailableException(
                "PostgreSQL Existing Systems evidence persistence is unavailable.");
        }
        catch (TimeoutException)
        {
            throw new ExistingSystemsDependencyUnavailableException(
                "PostgreSQL Existing Systems evidence persistence timed out.");
        }
    }

    private static async Task<StoredExistingSystemsEvidence?> LoadAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string tenantId,
        Guid discoveryId, int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction)
        {
            CommandTimeout = timeout
        };
        Add(command, "tenant_id", NpgsqlDbType.Text, tenantId);
        Add(command, "discovery_id", NpgsqlDbType.Uuid, discoveryId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new StoredExistingSystemsEvidence(
            new ExistingSystemsEvidenceReceipt(discoveryId, reader.GetGuid(0), reader.GetGuid(1),
                tenantId, reader.GetString(2), reader.GetString(4),
                new DateTimeOffset(reader.GetDateTime(5).ToUniversalTime())),
            reader.GetString(3));
    }

    private static void ValidateExisting(
        ExistingSystemsEvidenceRecord record,
        string recordDigest,
        StoredExistingSystemsEvidence stored)
    {
        var receipt = stored.Receipt;
        if (receipt.DiscoveryId != record.DiscoveryId ||
            receipt.ContextDiscoveryId != record.ContextDiscoveryId ||
            receipt.RegistrationId != record.RegistrationId ||
            !StringComparer.Ordinal.Equals(receipt.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.InventorySha256Digest, record.InventorySha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(stored.RecordSha256Digest, recordDigest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.EvidenceReference.Split('/').LastOrDefault(), recordDigest) ||
            receipt.RecordedAt < record.DiscoveredAt)
            throw new InvalidOperationException(
                "Stored Existing Systems evidence does not exactly match the authorized record.");
    }

    private static void Validate(ExistingSystemsEvidenceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.DiscoveryId == Guid.Empty || record.ContextDiscoveryId == Guid.Empty ||
            record.RegistrationId == Guid.Empty || record.RegistrationVersion < 0 ||
            record.PolicyDecisionRequestId == Guid.Empty || record.Systems.IsDefault ||
            record.EvidenceReferences.IsDefaultOrEmpty || record.DiscoveredAt == default)
            throw new InvalidOperationException("Existing Systems evidence payload is incomplete.");
        foreach (var value in new[] { record.TenantId, record.SubjectId, record.Purpose,
                     record.PolicyBundleId, record.PolicyBundleVersion }
                 .Concat(record.EvidenceReferences))
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var digest in new[] { record.IntentSha256Digest, record.ContextSha256Digest,
                     record.PolicyBundleSha256Digest, record.InventorySha256Digest })
            RequireSha256(digest);
    }

    private static void RequireSha256(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Existing Systems evidence requires SHA-256 digests.");
    }

    private static DateTimeOffset NormalizePostgreSqlTimestamp(DateTimeOffset value)
    {
        var utcTicks = value.UtcTicks;
        var remainder = utcTicks % 10;
        return new DateTimeOffset(remainder == 0 ? utcTicks : utcTicks + (10 - remainder), TimeSpan.Zero);
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });

    private sealed record StoredExistingSystemsEvidence(
        ExistingSystemsEvidenceReceipt Receipt,
        string RecordSha256Digest);
}
