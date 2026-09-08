using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlCodeGenerationEvidenceRecorder(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : ICodeGenerationEvidenceRecorder
{
    private const string SelectSql = """
        SELECT planning_id, delivery_run_id, candidate_sha256_digest, record_sha256_digest, evidence_reference, recorded_at
          FROM software_factory.code_generation_evidence
         WHERE tenant_id = @tenant_id AND generation_id = @generation_id FOR UPDATE
        """;
    private const string InsertSql = """
        INSERT INTO software_factory.code_generation_evidence
            (tenant_id, generation_id, planning_id, package_selection_id, delivery_run_id,
             candidate_sha256_digest, record_sha256_digest, record_json, evidence_reference,
             evidence_references, recorded_at)
        VALUES (@tenant_id, @generation_id, @planning_id, @package_selection_id, @delivery_run_id,
             @candidate_sha256_digest, @record_sha256_digest, @record_json, @evidence_reference,
             @evidence_references, @recorded_at)
        ON CONFLICT DO NOTHING
        """;

    public async Task<CodeGenerationEvidenceReceipt> RecordAsync(CodeGenerationEvidenceRecord record,
        CancellationToken cancellationToken)
    {
        Validate(record); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured) throw new CodeGenerationDependencyUnavailableException("PostgreSQL Code Generation evidence is not configured.");
        var recordedAt = Normalize(record.GeneratedAt); var persisted = record with { GeneratedAt = recordedAt };
        var json = JsonSerializer.Serialize(persisted);
        var recordDigest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(persisted)));
        var evidenceReference = $"evidence://code-generation/{record.GenerationId:D}/sha256/{recordDigest}";
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var existing = await LoadAsync(connection, transaction, record.TenantId, record.GenerationId,
                options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            { ValidateExisting(record, recordDigest, existing); await transaction.CommitAsync(cancellationToken); return existing.Receipt; }
            await using var command = new NpgsqlCommand(InsertSql, connection, transaction) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, record.TenantId); Add(command, "generation_id", NpgsqlDbType.Uuid, record.GenerationId);
            Add(command, "planning_id", NpgsqlDbType.Uuid, record.PlanningId); Add(command, "package_selection_id", NpgsqlDbType.Uuid, record.PackageSelectionId);
            Add(command, "delivery_run_id", NpgsqlDbType.Uuid, record.DeliveryRunId); Add(command, "candidate_sha256_digest", NpgsqlDbType.Text, record.CandidateSha256Digest);
            Add(command, "record_sha256_digest", NpgsqlDbType.Text, recordDigest); Add(command, "record_json", NpgsqlDbType.Jsonb, json);
            Add(command, "evidence_reference", NpgsqlDbType.Text, evidenceReference);
            Add(command, "evidence_references", NpgsqlDbType.Array | NpgsqlDbType.Text, record.EvidenceReferences.ToArray());
            Add(command, "recorded_at", NpgsqlDbType.TimestampTz, recordedAt);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                existing = await LoadAsync(connection, transaction, record.TenantId, record.GenerationId,
                    options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new InvalidOperationException("Code Generation evidence conflicted outside its tenant identity.");
                ValidateExisting(record, recordDigest, existing); await transaction.CommitAsync(cancellationToken); return existing.Receipt;
            }
            await transaction.CommitAsync(cancellationToken);
            return new(record.GenerationId, record.PlanningId, record.DeliveryRunId, record.TenantId,
                record.CandidateSha256Digest, evidenceReference, recordedAt);
        }
        catch (NpgsqlException) { throw new CodeGenerationDependencyUnavailableException("PostgreSQL Code Generation evidence persistence is unavailable."); }
        catch (TimeoutException) { throw new CodeGenerationDependencyUnavailableException("PostgreSQL Code Generation evidence persistence timed out."); }
    }

    private static async Task<Stored?> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string tenantId, Guid generationId, int timeout, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction) { CommandTimeout = timeout };
        Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "generation_id", NpgsqlDbType.Uuid, generationId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new(new(generationId, reader.GetGuid(0), reader.GetGuid(1), tenantId, reader.GetString(2), reader.GetString(4),
            new DateTimeOffset(reader.GetDateTime(5).ToUniversalTime())), reader.GetString(3));
    }

    private static void ValidateExisting(CodeGenerationEvidenceRecord record, string digest, Stored stored)
    {
        var receipt = stored.Receipt;
        if (receipt.PlanningId != record.PlanningId || receipt.DeliveryRunId != record.DeliveryRunId ||
            !StringComparer.OrdinalIgnoreCase.Equals(receipt.CandidateSha256Digest, record.CandidateSha256Digest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(stored.RecordDigest, digest) ||
            !StringComparer.Ordinal.Equals(receipt.EvidenceReference,
                $"evidence://code-generation/{record.GenerationId:D}/sha256/{digest}"))
            throw new InvalidOperationException("Stored Code Generation evidence does not match the authorized record.");
    }

    private static void Validate(CodeGenerationEvidenceRecord record)
    {
        if (record.GenerationId == Guid.Empty || record.PlanningId == Guid.Empty || record.PackageSelectionId == Guid.Empty ||
            record.DeliveryRunId == Guid.Empty || record.PolicyDecisionRequestId == Guid.Empty ||
            record.EvidenceReferences.IsDefaultOrEmpty || record.GeneratedAt == default ||
            record.CandidateSha256Digest.Length != 64 || record.Candidate.GeneratedFilePaths.IsDefaultOrEmpty ||
            record.Candidate.EvidenceReferences.IsDefaultOrEmpty || !record.Evaluation.IsAccepted)
            throw new InvalidOperationException("Code Generation evidence payload is incomplete.");
    }

    private static DateTimeOffset Normalize(DateTimeOffset value)
    { var ticks = value.UtcTicks; var remainder = ticks % 10; return new(remainder == 0 ? ticks : ticks + 10 - remainder, TimeSpan.Zero); }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
    private sealed record Stored(CodeGenerationEvidenceReceipt Receipt, string RecordDigest);
}
