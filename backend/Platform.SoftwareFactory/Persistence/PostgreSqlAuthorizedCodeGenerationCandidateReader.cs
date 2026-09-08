using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlAuthorizedCodeGenerationCandidateReader(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IStaticValidationCodeGenerationCandidateReader
{
    private const string SelectSql = """
        SELECT record_json, record_sha256_digest, evidence_reference, evidence_references, recorded_at
          FROM software_factory.code_generation_evidence
         WHERE tenant_id = @tenant_id AND generation_id = @generation_id
           AND candidate_sha256_digest = @candidate_sha256_digest AND evidence_reference = @evidence_reference
        """;

    public async Task<AuthorizedCodeGenerationCandidateSnapshot?> LoadAsync(Guid generationId,
        string tenantId, string purpose, string candidateSha256Digest, string generationEvidenceReference,
        CancellationToken cancellationToken)
    {
        if (generationId == Guid.Empty) throw new ArgumentException("Generation identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        GovernedAiPlanningRequest.ValidateDigest(candidateSha256Digest, "code candidate");
        ArgumentException.ThrowIfNullOrWhiteSpace(generationEvidenceReference);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured) throw new StaticValidationDependencyUnavailableException("PostgreSQL Code Generation evidence is not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "generation_id", NpgsqlDbType.Uuid, generationId);
            Add(command, "candidate_sha256_digest", NpgsqlDbType.Text, candidateSha256Digest.ToLowerInvariant());
            Add(command, "evidence_reference", NpgsqlDbType.Text, generationEvidenceReference);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0); var storedDigest = reader.GetString(1); var evidenceReference = reader.GetString(2);
            var storedEvidence = reader.GetFieldValue<string[]>(3).ToImmutableArray();
            var recordedAt = new DateTimeOffset(reader.GetDateTime(4).ToUniversalTime());
            var record = JsonSerializer.Deserialize<CodeGenerationEvidenceRecord>(json)
                ?? throw new InvalidOperationException("Stored Code Generation evidence is empty.");
            var digest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
            record.Candidate.Validate();
            if (record.GenerationId != generationId || !StringComparer.Ordinal.Equals(record.TenantId, tenantId) ||
                !StringComparer.Ordinal.Equals(record.Purpose, purpose) ||
                !StringComparer.OrdinalIgnoreCase.Equals(record.CandidateSha256Digest, candidateSha256Digest) ||
                !StringComparer.OrdinalIgnoreCase.Equals(storedDigest, digest) ||
                !StringComparer.Ordinal.Equals(evidenceReference, generationEvidenceReference) ||
                !StringComparer.Ordinal.Equals(evidenceReference, $"evidence://code-generation/{generationId:D}/sha256/{digest}") ||
                record.Candidate.GeneratedFilePaths.IsDefaultOrEmpty || record.Candidate.EvidenceReferences.IsDefaultOrEmpty ||
                !record.Evaluation.IsAccepted || storedEvidence.IsDefaultOrEmpty || record.GeneratedAt > recordedAt)
                throw new InvalidOperationException("Stored Code Generation evidence failed exact binding validation.");
            foreach (var path in record.Candidate.GeneratedFilePaths) GovernedGeneratedPath.Validate(path);
            var evidence = record.EvidenceReferences.Concat(storedEvidence).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
            var receipt = new GovernedCodeGenerationReceipt(record.GenerationId, record.PlanningId,
                record.PackageSelectionId, record.DeliveryRunId, record.TenantId, GovernedIntentPolicyOutcome.Permit,
                true, false, false, false, record.CandidateSha256Digest, record.Candidate.Content,
                record.Candidate.GeneratedFilePaths, record.Evaluation.Findings, evidenceReference, evidence,
                "Separately approved Static Validation", recordedAt);
            return new(receipt, record.Candidate);
        }
        catch (NpgsqlException) { throw new StaticValidationDependencyUnavailableException("Code Generation evidence read is unavailable."); }
        catch (TimeoutException) { throw new StaticValidationDependencyUnavailableException("Code Generation evidence read timed out."); }
        catch (JsonException exception) { throw new InvalidOperationException("Code Generation evidence is malformed.", exception); }
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
