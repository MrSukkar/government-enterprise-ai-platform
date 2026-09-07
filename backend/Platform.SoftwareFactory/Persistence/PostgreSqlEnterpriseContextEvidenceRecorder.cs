using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlEnterpriseContextEvidenceRecorder(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IEnterpriseContextEvidenceRecorder
{
    private const string SelectSql = """
        SELECT registration_id, context_sha256_digest, record_sha256_digest,
               evidence_reference, recorded_at
          FROM software_factory.enterprise_context_evidence
         WHERE tenant_id = @tenant_id AND discovery_id = @discovery_id
         FOR UPDATE
        """;

    private const string InsertSql = """
        INSERT INTO software_factory.enterprise_context_evidence
            (tenant_id, discovery_id, registration_id, registration_version,
             context_sha256_digest, record_sha256_digest, record_json,
             evidence_reference, evidence_references, recorded_at)
        VALUES
            (@tenant_id, @discovery_id, @registration_id, @registration_version,
             @context_sha256_digest, @record_sha256_digest, @record_json,
             @evidence_reference, @evidence_references, @recorded_at)
        ON CONFLICT DO NOTHING
        """;

    public async Task<EnterpriseContextEvidenceReceipt> RecordAsync(
        EnterpriseContextEvidenceRecord record,
        CancellationToken cancellationToken)
    {
        Validate(record);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new EnterpriseContextDependencyUnavailableException(
                "PostgreSQL Enterprise Context evidence is not validly configured.");
        var recordedAt = NormalizePostgreSqlTimestamp(record.DiscoveredAt);
        var persistedRecord = record with { DiscoveredAt = recordedAt };
        var recordJson = JsonSerializer.Serialize(persistedRecord);
        var recordDigest = Convert.ToHexStringLower(
            SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(persistedRecord)));
        var evidenceReference =
            $"evidence://enterprise-context/{record.DiscoveryId:D}/sha256/{recordDigest}";

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
            Add(command, "registration_id", NpgsqlDbType.Uuid, record.RegistrationId);
            Add(command, "registration_version", NpgsqlDbType.Bigint, record.RegistrationVersion);
            Add(command, "context_sha256_digest", NpgsqlDbType.Text, record.ContextSha256Digest);
            Add(command, "record_sha256_digest", NpgsqlDbType.Text, recordDigest);
            Add(command, "record_json", NpgsqlDbType.Jsonb, recordJson);
            Add(command, "evidence_reference", NpgsqlDbType.Text, evidenceReference);
            Add(command, "evidence_references", NpgsqlDbType.Array | NpgsqlDbType.Text,
                record.EvidenceReferences.ToArray());
            Add(command, "recorded_at", NpgsqlDbType.TimestampTz, recordedAt);
            var inserted = await command.ExecuteNonQueryAsync(cancellationToken);
            if (inserted == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId,
                    record.DiscoveryId, options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException(
                        "Enterprise Context evidence conflicted outside its tenant-scoped identity.");
                ValidateExisting(record, recordDigest, existing);
                await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }

            await transaction.CommitAsync(cancellationToken);
            return new EnterpriseContextEvidenceReceipt(
                record.DiscoveryId, record.RegistrationId, record.TenantId,
                record.ContextSha256Digest, evidenceReference, recordedAt);
        }
        catch (NpgsqlException)
        {
            throw new EnterpriseContextDependencyUnavailableException(
                "PostgreSQL Enterprise Context evidence persistence is unavailable.");
        }
        catch (TimeoutException)
        {
            throw new EnterpriseContextDependencyUnavailableException(
                "PostgreSQL Enterprise Context evidence persistence timed out.");
        }
    }

    private static async Task<StoredEnterpriseContextEvidence?> LoadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string tenantId,
        Guid discoveryId,
        int timeout,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction)
        {
            CommandTimeout = timeout
        };
        Add(command, "tenant_id", NpgsqlDbType.Text, tenantId);
        Add(command, "discovery_id", NpgsqlDbType.Uuid, discoveryId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new StoredEnterpriseContextEvidence(
            new EnterpriseContextEvidenceReceipt(
                discoveryId, reader.GetGuid(0), tenantId, reader.GetString(1),
                reader.GetString(3), new DateTimeOffset(reader.GetDateTime(4).ToUniversalTime())),
            reader.GetString(2));
    }

    private static void ValidateExisting(
        EnterpriseContextEvidenceRecord record,
        string recordDigest,
        StoredEnterpriseContextEvidence stored)
    {
        var existing = stored.Receipt;
        var digestSegment = existing.EvidenceReference.Split('/').LastOrDefault();
        if (existing.DiscoveryId != record.DiscoveryId || existing.RegistrationId != record.RegistrationId ||
            !StringComparer.Ordinal.Equals(existing.TenantId, record.TenantId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.ContextSha256Digest, record.ContextSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(stored.RecordSha256Digest, recordDigest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(digestSegment, stored.RecordSha256Digest) ||
            existing.RecordedAt < record.DiscoveredAt)
            throw new InvalidOperationException(
                "Stored Enterprise Context evidence does not exactly match the authorized record.");
    }

    private static void Validate(EnterpriseContextEvidenceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.DiscoveryId == Guid.Empty || record.RegistrationId == Guid.Empty ||
            record.RegistrationVersion < 0 || record.PolicyDecisionRequestId == Guid.Empty)
            throw new InvalidOperationException("Enterprise Context evidence identity is invalid.");
        ArgumentException.ThrowIfNullOrWhiteSpace(record.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.Purpose);
        RequireSha256(record.IntentSha256Digest);
        RequireSha256(record.PolicyBundleSha256Digest);
        RequireSha256(record.ContextSha256Digest);
        if (record.Items.IsDefault || record.EvidenceReferences.IsDefaultOrEmpty || record.DiscoveredAt == default)
            throw new InvalidOperationException("Enterprise Context evidence payload is incomplete.");
        foreach (var reference in record.EvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(reference);
    }

    private static void RequireSha256(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Enterprise Context evidence requires SHA-256 digests.");
    }

    private static DateTimeOffset NormalizePostgreSqlTimestamp(DateTimeOffset value)
    {
        var utcTicks = value.UtcTicks;
        var remainder = utcTicks % 10;
        return new DateTimeOffset(remainder == 0 ? utcTicks : utcTicks + (10 - remainder), TimeSpan.Zero);
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });

    private sealed record StoredEnterpriseContextEvidence(
        EnterpriseContextEvidenceReceipt Receipt,
        string RecordSha256Digest);
}
