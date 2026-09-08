using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlAuthorizedStaticValidationReceiptReader(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IAuthorizedStaticValidationReceiptReader
{
    private const string SelectSql = """
        SELECT record_json, record_sha256_digest, evidence_reference, evidence_references, recorded_at
          FROM software_factory.static_validation_evidence
         WHERE tenant_id = @tenant_id AND validation_id = @validation_id
           AND generation_id = @generation_id AND delivery_run_id = @delivery_run_id
           AND candidate_sha256_digest = @candidate_sha256_digest
           AND report_sha256_digest = @report_sha256_digest AND evidence_reference = @evidence_reference
        """;

    public async Task<GovernedStaticValidationReceipt?> LoadAsync(Guid validationId, string tenantId,
        string purpose, Guid generationId, Guid deliveryRunId, string candidateSha256Digest,
        string staticReportSha256Digest, string staticEvidenceReference, CancellationToken cancellationToken)
    {
        if (validationId == Guid.Empty || generationId == Guid.Empty || deliveryRunId == Guid.Empty)
            throw new ArgumentException("Security prerequisite identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        GovernedAiPlanningRequest.ValidateDigest(candidateSha256Digest, "code candidate");
        GovernedAiPlanningRequest.ValidateDigest(staticReportSha256Digest, "Static report");
        ArgumentException.ThrowIfNullOrWhiteSpace(staticEvidenceReference); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new SecurityValidationDependencyUnavailableException("PostgreSQL Static evidence is not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "validation_id", NpgsqlDbType.Uuid, validationId);
            Add(command, "generation_id", NpgsqlDbType.Uuid, generationId); Add(command, "delivery_run_id", NpgsqlDbType.Uuid, deliveryRunId);
            Add(command, "candidate_sha256_digest", NpgsqlDbType.Text, candidateSha256Digest.ToLowerInvariant());
            Add(command, "report_sha256_digest", NpgsqlDbType.Text, staticReportSha256Digest.ToLowerInvariant());
            Add(command, "evidence_reference", NpgsqlDbType.Text, staticEvidenceReference);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0); var storedDigest = reader.GetString(1);
            var evidenceReference = reader.GetString(2); var storedEvidence = reader.GetFieldValue<string[]>(3).ToImmutableArray();
            var recordedAt = new DateTimeOffset(reader.GetDateTime(4).ToUniversalTime());
            var record = JsonSerializer.Deserialize<StaticValidationEvidenceRecord>(json) ??
                throw new InvalidOperationException("Stored Static evidence is empty.");
            var digest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
            if (record.ValidationId != validationId || record.GenerationId != generationId || record.DeliveryRunId != deliveryRunId ||
                !StringComparer.Ordinal.Equals(record.TenantId, tenantId) || !StringComparer.Ordinal.Equals(record.Purpose, purpose) ||
                !StringComparer.OrdinalIgnoreCase.Equals(record.CandidateSha256Digest, candidateSha256Digest) ||
                !StringComparer.OrdinalIgnoreCase.Equals(record.ReportSha256Digest, staticReportSha256Digest) ||
                !StringComparer.OrdinalIgnoreCase.Equals(storedDigest, digest) || !StringComparer.Ordinal.Equals(evidenceReference, staticEvidenceReference) ||
                !StringComparer.Ordinal.Equals(evidenceReference, $"evidence://static-validation/{validationId:D}/sha256/{digest}") ||
                !record.Report.IsAccepted || record.Report.Gate != Validation.ValidationGate.Static ||
                record.Report.ControlReports.IsDefaultOrEmpty || record.Report.ControlReports.Any(control =>
                    control.Gate != Validation.ValidationGate.Static || !control.Completed || !control.Passed ||
                    string.IsNullOrWhiteSpace(control.EvidenceReference) || control.Findings.IsDefault ||
                    control.Findings.Any(finding => finding.Severity is Validation.ValidationSeverity.Error or Validation.ValidationSeverity.Critical ||
                        string.IsNullOrWhiteSpace(finding.RuleId) || string.IsNullOrWhiteSpace(finding.Message) ||
                        string.IsNullOrWhiteSpace(finding.Location) || string.IsNullOrWhiteSpace(finding.EvidenceReference))) ||
                storedEvidence.IsDefaultOrEmpty || record.ValidatedAt > recordedAt)
                throw new InvalidOperationException("Stored Static evidence failed exact binding validation.");
            var evidence = record.EvidenceReferences.Concat(storedEvidence).Append(evidenceReference)
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
            return new(record.ValidationId, record.GenerationId, record.DeliveryRunId, record.TenantId,
                GovernedIntentPolicyOutcome.Permit, Validation.ValidationGate.Static, true, false, false,
                record.CandidateSha256Digest, record.ReportSha256Digest, record.Report.ControlReports,
                evidenceReference, evidence, "Separately approved Security Validation", recordedAt);
        }
        catch (NpgsqlException) { throw new SecurityValidationDependencyUnavailableException("Static evidence read is unavailable."); }
        catch (TimeoutException) { throw new SecurityValidationDependencyUnavailableException("Static evidence read timed out."); }
        catch (JsonException exception) { throw new InvalidOperationException("Stored Static evidence is malformed.", exception); }
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
