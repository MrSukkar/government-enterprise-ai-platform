using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlSecurityValidationCodeGenerationCandidateReader(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : ISecurityValidationCodeGenerationCandidateReader
{
    private const string SelectSql = """
        SELECT generation.record_json, generation.record_sha256_digest,
               generation.evidence_reference, generation.evidence_references, generation.recorded_at
          FROM software_factory.code_generation_evidence AS generation
          JOIN software_factory.static_validation_evidence AS static
            ON static.tenant_id = generation.tenant_id AND static.generation_id = generation.generation_id
         WHERE generation.tenant_id = @tenant_id AND generation.generation_id = @generation_id
           AND generation.candidate_sha256_digest = @candidate_sha256_digest
           AND static.validation_id = @static_validation_id
           AND static.candidate_sha256_digest = @candidate_sha256_digest
           AND static.evidence_reference = @static_evidence_reference
        """;

    public async Task<AuthorizedCodeGenerationCandidateSnapshot?> LoadAsync(Guid generationId,
        string tenantId, string purpose, string candidateSha256Digest, Guid staticValidationId,
        string staticEvidenceReference, CancellationToken cancellationToken)
    {
        if (generationId == Guid.Empty || staticValidationId == Guid.Empty)
            throw new ArgumentException("Security candidate identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        GovernedAiPlanningRequest.ValidateDigest(candidateSha256Digest, "code candidate");
        ArgumentException.ThrowIfNullOrWhiteSpace(staticEvidenceReference); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new SecurityValidationDependencyUnavailableException("PostgreSQL Code Generation evidence is not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "generation_id", NpgsqlDbType.Uuid, generationId);
            Add(command, "candidate_sha256_digest", NpgsqlDbType.Text, candidateSha256Digest.ToLowerInvariant());
            Add(command, "static_validation_id", NpgsqlDbType.Uuid, staticValidationId);
            Add(command, "static_evidence_reference", NpgsqlDbType.Text, staticEvidenceReference);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0); var storedDigest = reader.GetString(1); var evidenceReference = reader.GetString(2);
            var storedEvidence = reader.GetFieldValue<string[]>(3).ToImmutableArray();
            var recordedAt = new DateTimeOffset(reader.GetDateTime(4).ToUniversalTime());
            var record = JsonSerializer.Deserialize<CodeGenerationEvidenceRecord>(json) ??
                throw new InvalidOperationException("Stored Code Generation evidence is empty.");
            var digest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
            record.Candidate.Validate();
            if (record.GenerationId != generationId || !StringComparer.Ordinal.Equals(record.TenantId, tenantId) ||
                !StringComparer.Ordinal.Equals(record.Purpose, purpose) ||
                !StringComparer.OrdinalIgnoreCase.Equals(record.CandidateSha256Digest, candidateSha256Digest) ||
                !StringComparer.OrdinalIgnoreCase.Equals(storedDigest, digest) ||
                !StringComparer.Ordinal.Equals(evidenceReference, $"evidence://code-generation/{generationId:D}/sha256/{digest}") ||
                record.Candidate.GeneratedFilePaths.IsDefaultOrEmpty || record.Candidate.EvidenceReferences.IsDefaultOrEmpty ||
                !record.Evaluation.IsAccepted || storedEvidence.IsDefaultOrEmpty || record.GeneratedAt > recordedAt)
                throw new InvalidOperationException("Stored Code Generation evidence failed Security-chain validation.");
            foreach (var path in record.Candidate.GeneratedFilePaths) GovernedGeneratedPath.Validate(path);
            var evidence = record.EvidenceReferences.Concat(storedEvidence).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToImmutableArray();
            var receipt = new GovernedCodeGenerationReceipt(record.GenerationId, record.PlanningId,
                record.PackageSelectionId, record.DeliveryRunId, record.TenantId, GovernedIntentPolicyOutcome.Permit,
                true, false, false, false, record.CandidateSha256Digest, record.Candidate.Content,
                record.Candidate.GeneratedFilePaths, record.Evaluation.Findings, evidenceReference, evidence,
                "Separately approved Static Validation", recordedAt);
            return new(receipt, record.Candidate);
        }
        catch (NpgsqlException) { throw new SecurityValidationDependencyUnavailableException("Code Generation evidence read is unavailable."); }
        catch (TimeoutException) { throw new SecurityValidationDependencyUnavailableException("Code Generation evidence read timed out."); }
        catch (JsonException exception) { throw new InvalidOperationException("Stored Code Generation evidence is malformed.", exception); }
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
