using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlSandboxEvidenceRecorder(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : ISandboxEvidenceRecorder
{
    private const string SelectSql = """
        SELECT delivery_run_id, result_sha256_digest, record_sha256_digest, evidence_reference, recorded_at
          FROM software_factory.sandbox_evidence
         WHERE tenant_id = @tenant_id AND execution_id = @execution_id FOR UPDATE
        """;
    private const string InsertSql = """
        INSERT INTO software_factory.sandbox_evidence
            (tenant_id, execution_id, security_validation_id, generation_id, delivery_run_id,
             candidate_sha256_digest, security_report_sha256_digest, result_sha256_digest,
             record_sha256_digest, record_json, evidence_reference, evidence_references, recorded_at)
        VALUES (@tenant_id, @execution_id, @security_validation_id, @generation_id, @delivery_run_id,
             @candidate_sha256_digest, @security_report_sha256_digest, @result_sha256_digest,
             @record_sha256_digest, @record_json, @evidence_reference, @evidence_references, @recorded_at)
        ON CONFLICT DO NOTHING
        """;

    public async Task<SandboxEvidenceReceipt> RecordAsync(
        SandboxEvidenceRecord record, CancellationToken cancellationToken)
    {
        Validate(record); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new SandboxDependencyUnavailableException("PostgreSQL Sandbox evidence is not configured.");
        var recordedAt = Normalize(record.ExecutedAt); var persisted = record with { ExecutedAt = recordedAt };
        var json = JsonSerializer.Serialize(persisted);
        var recordDigest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(persisted)));
        var evidenceReference = $"evidence://sandbox/{record.ExecutionId:D}/sha256/{recordDigest}";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId, record.ExecutionId,
                options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(record, recordDigest, existing); await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            await using var command = new NpgsqlCommand(InsertSql, connection, transaction)
                { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, record.TenantId); Add(command, "execution_id", NpgsqlDbType.Uuid, record.ExecutionId);
            Add(command, "security_validation_id", NpgsqlDbType.Uuid, record.SecurityValidationId);
            Add(command, "generation_id", NpgsqlDbType.Uuid, record.GenerationId); Add(command, "delivery_run_id", NpgsqlDbType.Uuid, record.DeliveryRunId);
            Add(command, "candidate_sha256_digest", NpgsqlDbType.Text, record.CandidateSha256Digest);
            Add(command, "security_report_sha256_digest", NpgsqlDbType.Text, record.SecurityReportSha256Digest);
            Add(command, "result_sha256_digest", NpgsqlDbType.Text, record.ResultSha256Digest);
            Add(command, "record_sha256_digest", NpgsqlDbType.Text, recordDigest); Add(command, "record_json", NpgsqlDbType.Jsonb, json);
            Add(command, "evidence_reference", NpgsqlDbType.Text, evidenceReference);
            Add(command, "evidence_references", NpgsqlDbType.Array | NpgsqlDbType.Text, record.EvidenceReferences.ToArray());
            Add(command, "recorded_at", NpgsqlDbType.TimestampTz, recordedAt);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId, record.ExecutionId,
                    options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException("Sandbox evidence conflicted outside its tenant identity.");
                ValidateExisting(record, recordDigest, existing); await transaction.CommitAsync(cancellationToken);
                return existing.Receipt;
            }
            await transaction.CommitAsync(cancellationToken);
            return new(record.ExecutionId, record.DeliveryRunId, record.TenantId,
                record.ResultSha256Digest, evidenceReference, recordedAt);
        }
        catch (NpgsqlException) { throw new SandboxDependencyUnavailableException("PostgreSQL Sandbox evidence is unavailable."); }
        catch (TimeoutException) { throw new SandboxDependencyUnavailableException("PostgreSQL Sandbox evidence timed out."); }
    }

    private static async Task<Stored?> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string tenantId, Guid executionId, int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "execution_id", NpgsqlDbType.Uuid, executionId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new(new(executionId, reader.GetGuid(0), tenantId, reader.GetString(1), reader.GetString(3),
            new DateTimeOffset(reader.GetDateTime(4).ToUniversalTime())), reader.GetString(2));
    }

    private static void ValidateExisting(SandboxEvidenceRecord record, string digest, Stored stored)
    {
        var receipt = stored.Receipt;
        if (receipt.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.ResultSha256Digest, record.ResultSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(stored.RecordDigest, digest) ||
            !StringComparer.Ordinal.Equals(receipt.EvidenceReference,
                $"evidence://sandbox/{record.ExecutionId:D}/sha256/{digest}"))
            throw new InvalidOperationException("Stored Sandbox evidence does not match the authorized record.");
    }

    private static void Validate(SandboxEvidenceRecord record)
    {
        if (record.ExecutionId == Guid.Empty || record.SecurityValidationId == Guid.Empty ||
            record.GenerationId == Guid.Empty || record.DeliveryRunId == Guid.Empty ||
            record.PolicyDecisionRequestId == Guid.Empty || string.IsNullOrWhiteSpace(record.TenantId) ||
            string.IsNullOrWhiteSpace(record.SubjectId) || string.IsNullOrWhiteSpace(record.Purpose) ||
            string.IsNullOrWhiteSpace(record.Environment) || string.IsNullOrWhiteSpace(record.SecurityEvidenceReference) ||
            record.EvidenceReferences.IsDefaultOrEmpty || record.ExecutedAt == default || !record.Result.IsAccepted)
            throw new InvalidOperationException("Sandbox evidence payload is incomplete.");
        GovernedAiPlanningRequest.ValidateDigest(record.CandidateSha256Digest, "code candidate");
        GovernedAiPlanningRequest.ValidateDigest(record.SecurityReportSha256Digest, "Security report");
        GovernedAiPlanningRequest.ValidateDigest(record.ResultSha256Digest, "Sandbox result");
        record.SandboxImage.Validate(); record.IsolationPolicy.Validate();
    }

    private static DateTimeOffset Normalize(DateTimeOffset value)
    { var ticks = value.UtcTicks; var remainder = ticks % 10; return new(remainder == 0 ? ticks : ticks + 10 - remainder, TimeSpan.Zero); }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
    private sealed record Stored(SandboxEvidenceReceipt Receipt, string RecordDigest);
}
