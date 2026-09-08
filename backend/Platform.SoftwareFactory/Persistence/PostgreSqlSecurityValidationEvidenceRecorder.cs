using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlSecurityValidationEvidenceRecorder(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : ISecurityValidationEvidenceRecorder
{
    private const string SelectSql = """
        SELECT generation_id, delivery_run_id, security_report_sha256_digest,
               record_sha256_digest, evidence_reference, recorded_at
          FROM software_factory.security_validation_evidence
         WHERE tenant_id = @tenant_id AND validation_id = @validation_id FOR UPDATE
        """;
    private const string InsertSql = """
        INSERT INTO software_factory.security_validation_evidence
            (tenant_id, validation_id, static_validation_id, generation_id, delivery_run_id,
             candidate_sha256_digest, static_report_sha256_digest, security_report_sha256_digest,
             record_sha256_digest, record_json, evidence_reference, evidence_references, recorded_at)
        VALUES (@tenant_id, @validation_id, @static_validation_id, @generation_id, @delivery_run_id,
             @candidate_sha256_digest, @static_report_sha256_digest, @security_report_sha256_digest,
             @record_sha256_digest, @record_json, @evidence_reference, @evidence_references, @recorded_at)
        ON CONFLICT DO NOTHING
        """;

    public async Task<SecurityValidationEvidenceReceipt> RecordAsync(SecurityValidationEvidenceRecord record,
        CancellationToken cancellationToken)
    {
        Validate(record); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new SecurityValidationDependencyUnavailableException("PostgreSQL Security evidence is not configured.");
        var recordedAt = Normalize(record.ValidatedAt); var persisted = record with { ValidatedAt = recordedAt };
        var json = JsonSerializer.Serialize(persisted);
        var recordDigest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(persisted)));
        var evidenceReference = $"evidence://security-validation/{record.ValidationId:D}/sha256/{recordDigest}";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId, record.ValidationId,
                options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(record, recordDigest, existing); await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            await using var command = new NpgsqlCommand(InsertSql, connection, transaction)
                { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, record.TenantId); Add(command, "validation_id", NpgsqlDbType.Uuid, record.ValidationId);
            Add(command, "static_validation_id", NpgsqlDbType.Uuid, record.StaticValidationId); Add(command, "generation_id", NpgsqlDbType.Uuid, record.GenerationId);
            Add(command, "delivery_run_id", NpgsqlDbType.Uuid, record.DeliveryRunId); Add(command, "candidate_sha256_digest", NpgsqlDbType.Text, record.CandidateSha256Digest);
            Add(command, "static_report_sha256_digest", NpgsqlDbType.Text, record.StaticReportSha256Digest);
            Add(command, "security_report_sha256_digest", NpgsqlDbType.Text, record.SecurityReportSha256Digest);
            Add(command, "record_sha256_digest", NpgsqlDbType.Text, recordDigest); Add(command, "record_json", NpgsqlDbType.Jsonb, json);
            Add(command, "evidence_reference", NpgsqlDbType.Text, evidenceReference);
            Add(command, "evidence_references", NpgsqlDbType.Array | NpgsqlDbType.Text, record.EvidenceReferences.ToArray());
            Add(command, "recorded_at", NpgsqlDbType.TimestampTz, recordedAt);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId, record.ValidationId,
                    options.CommandTimeoutSeconds, cancellationToken) ??
                    throw new InvalidOperationException("Security evidence conflicted outside its tenant identity.");
                ValidateExisting(record, recordDigest, existing); await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            await transaction.CommitAsync(cancellationToken);
            return new(record.ValidationId, record.GenerationId, record.DeliveryRunId, record.TenantId,
                record.SecurityReportSha256Digest, evidenceReference, recordedAt);
        }
        catch (NpgsqlException) { throw new SecurityValidationDependencyUnavailableException("PostgreSQL Security evidence is unavailable."); }
        catch (TimeoutException) { throw new SecurityValidationDependencyUnavailableException("PostgreSQL Security evidence timed out."); }
    }

    private static async Task<Stored?> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string tenantId, Guid validationId, int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "validation_id", NpgsqlDbType.Uuid, validationId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new(new(validationId, reader.GetGuid(0), reader.GetGuid(1), tenantId, reader.GetString(2),
            reader.GetString(4), new DateTimeOffset(reader.GetDateTime(5).ToUniversalTime())), reader.GetString(3));
    }

    private static void ValidateExisting(SecurityValidationEvidenceRecord record, string digest, Stored stored)
    {
        var receipt = stored.Receipt;
        if (receipt.GenerationId != record.GenerationId || receipt.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.SecurityReportSha256Digest, record.SecurityReportSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(stored.RecordDigest, digest) ||
            !StringComparer.Ordinal.Equals(receipt.EvidenceReference, $"evidence://security-validation/{record.ValidationId:D}/sha256/{digest}"))
            throw new InvalidOperationException("Stored Security evidence does not match the authorized record.");
    }

    private static void Validate(SecurityValidationEvidenceRecord record)
    {
        if (record.ValidationId == Guid.Empty || record.StaticValidationId == Guid.Empty ||
            record.GenerationId == Guid.Empty || record.DeliveryRunId == Guid.Empty ||
            record.PolicyDecisionRequestId == Guid.Empty || string.IsNullOrWhiteSpace(record.TenantId) ||
            string.IsNullOrWhiteSpace(record.SubjectId) || string.IsNullOrWhiteSpace(record.Purpose) ||
            record.EvidenceReferences.IsDefaultOrEmpty || record.ValidatedAt == default ||
            record.CandidateSha256Digest.Length != 64 || record.StaticReportSha256Digest.Length != 64 ||
            record.SecurityReportSha256Digest.Length != 64 || !record.Report.IsAccepted ||
            record.Report.Gate != Validation.ValidationGate.Security)
            throw new InvalidOperationException("Security evidence payload is incomplete.");
    }

    private static DateTimeOffset Normalize(DateTimeOffset value)
    { var ticks = value.UtcTicks; var remainder = ticks % 10; return new(remainder == 0 ? ticks : ticks + 10 - remainder, TimeSpan.Zero); }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
    private sealed record Stored(SecurityValidationEvidenceReceipt Receipt, string RecordDigest);
}
