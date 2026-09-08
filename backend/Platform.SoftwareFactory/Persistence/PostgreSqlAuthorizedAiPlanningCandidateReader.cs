using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlAuthorizedAiPlanningCandidateReader(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IAuthorizedAiPlanningCandidateReader
{
    private const string SelectSql = """
        SELECT record_json, record_sha256_digest, evidence_reference, evidence_references, recorded_at
          FROM software_factory.ai_planning_evidence
         WHERE tenant_id = @tenant_id AND planning_id = @planning_id
           AND package_selection_id = @package_selection_id
           AND candidate_sha256_digest = @planning_sha256_digest
        """;

    public async Task<GovernedAiPlanningReceipt?> LoadAsync(Guid planningId, string tenantId, string purpose,
        Guid packageSelectionId, string selectionSha256Digest, string planningSha256Digest,
        CancellationToken cancellationToken)
    {
        if (planningId == Guid.Empty || packageSelectionId == Guid.Empty) throw new ArgumentException("Planning identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        GovernedAiPlanningRequest.ValidateDigest(selectionSha256Digest, "package selection");
        GovernedAiPlanningRequest.ValidateDigest(planningSha256Digest, "AI Planning candidate");
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured) throw new CodeGenerationDependencyUnavailableException("PostgreSQL AI Planning evidence is not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "planning_id", NpgsqlDbType.Uuid, planningId);
            Add(command, "package_selection_id", NpgsqlDbType.Uuid, packageSelectionId);
            Add(command, "planning_sha256_digest", NpgsqlDbType.Text, planningSha256Digest.ToLowerInvariant());
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0); var storedDigest = reader.GetString(1); var evidenceReference = reader.GetString(2);
            var storedEvidence = reader.GetFieldValue<string[]>(3).ToImmutableArray();
            var recordedAt = new DateTimeOffset(reader.GetDateTime(4).ToUniversalTime());
            var record = JsonSerializer.Deserialize<AiPlanningEvidenceRecord>(json)
                ?? throw new InvalidOperationException("Stored AI Planning evidence is empty.");
            var digest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
            if (record.PlanningId != planningId || record.PackageSelectionId != packageSelectionId ||
                !StringComparer.Ordinal.Equals(record.TenantId, tenantId) || !StringComparer.Ordinal.Equals(record.Purpose, purpose) ||
                !StringComparer.OrdinalIgnoreCase.Equals(record.SelectionSha256Digest, selectionSha256Digest) ||
                !StringComparer.OrdinalIgnoreCase.Equals(record.CandidateSha256Digest, planningSha256Digest) ||
                !StringComparer.OrdinalIgnoreCase.Equals(storedDigest, digest) || record.Candidate.GeneratedFilePaths.Length != 0 ||
                record.Candidate.EvidenceReferences.IsDefaultOrEmpty || !record.Evaluation.IsAccepted || storedEvidence.IsDefaultOrEmpty ||
                !StringComparer.Ordinal.Equals(evidenceReference, $"evidence://ai-planning/{planningId:D}/sha256/{digest}") ||
                record.PlannedAt > recordedAt)
                throw new InvalidOperationException("Stored AI Planning evidence failed exact binding validation.");
            return new(record.PlanningId, record.PackageSelectionId, record.DeliveryRunId, record.TenantId,
                GovernedIntentPolicyOutcome.Permit, true, false, false, record.CandidateSha256Digest,
                record.Candidate.Content, record.Evaluation.Findings, evidenceReference,
                record.EvidenceReferences.Concat(storedEvidence).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(),
                "Separately approved Code Generation", recordedAt);
        }
        catch (NpgsqlException) { throw new CodeGenerationDependencyUnavailableException("PostgreSQL AI Planning evidence read is unavailable."); }
        catch (TimeoutException) { throw new CodeGenerationDependencyUnavailableException("PostgreSQL AI Planning evidence read timed out."); }
        catch (JsonException exception) { throw new InvalidOperationException("Stored AI Planning evidence is malformed.", exception); }
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
