using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlAuthorizedApprovedPackagesSnapshotReader(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IAuthorizedApprovedPackagesSnapshotReader
{
    private const string SelectSql = """
        SELECT packages.record_json, packages.record_sha256_digest, packages.evidence_reference,
               packages.evidence_references, packages.recorded_at,
               architecture.record_json, architecture.record_sha256_digest
          FROM software_factory.approved_packages_evidence AS packages
          JOIN software_factory.existing_architecture_evidence AS architecture
            ON architecture.tenant_id = packages.tenant_id
           AND architecture.discovery_id = packages.architecture_discovery_id
         WHERE packages.tenant_id = @tenant_id AND packages.selection_id = @selection_id
        """;

    public async Task<GovernedApprovedPackagesSelectionReceipt?> LoadAsync(
        Guid selectionId, string tenantId, string purpose, CancellationToken cancellationToken)
    {
        if (selectionId == Guid.Empty) throw new ArgumentException("Package selection identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new AiPlanningDependencyUnavailableException("PostgreSQL Approved Packages snapshots are not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "selection_id", NpgsqlDbType.Uuid, selectionId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0); var storedDigest = reader.GetString(1);
            var evidenceReference = reader.GetString(2); var storedEvidence = reader.GetFieldValue<string[]>(3).ToImmutableArray();
            var recordedAt = new DateTimeOffset(reader.GetDateTime(4).ToUniversalTime());
            var record = JsonSerializer.Deserialize<ApprovedPackagesEvidenceRecord>(json)
                ?? throw new InvalidOperationException("Stored Approved Packages evidence is empty.");
            var architectureJson = reader.GetString(5);
            var architectureRecord = JsonSerializer.Deserialize<ExistingArchitectureEvidenceRecord>(architectureJson)
                ?? throw new InvalidOperationException("Stored prerequisite Existing Architecture evidence is empty.");
            var computedRecordDigest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
            var computedArchitectureRecordDigest = Convert.ToHexStringLower(
                SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(architectureRecord)));
            Validate(record, selectionId, tenantId, purpose, storedDigest, computedRecordDigest,
                evidenceReference, storedEvidence, recordedAt);
            if (architectureRecord.DiscoveryId != record.ArchitectureDiscoveryId ||
                architectureRecord.SystemsDiscoveryId == Guid.Empty || architectureRecord.ContextDiscoveryId == Guid.Empty ||
                !StringComparer.Ordinal.Equals(architectureRecord.TenantId, tenantId) ||
                !StringComparer.Ordinal.Equals(architectureRecord.Purpose, purpose) ||
                !StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(6), computedArchitectureRecordDigest) ||
                !StringComparer.OrdinalIgnoreCase.Equals(architectureRecord.ArchitectureSha256Digest,
                    record.ArchitectureSha256Digest))
                throw new InvalidOperationException("Stored Approved Packages lineage is invalid.");
            if (!StringComparer.OrdinalIgnoreCase.Equals(SelectionDigest(record), record.SelectionSha256Digest))
                throw new InvalidOperationException("Stored Approved Packages selection digest is invalid.");
            return new(record.SelectionId, record.ArchitectureDiscoveryId,
                architectureRecord.SystemsDiscoveryId, architectureRecord.ContextDiscoveryId,
                record.RegistrationId, record.RegistrationVersion, record.TenantId,
                record.ArchitectureSha256Digest, GovernedIntentPolicyOutcome.Permit,
                IsSelectionReleased: true, CanAdvance: false, record.SelectionSha256Digest,
                record.Packages, evidenceReference,
                record.EvidenceReferences.Append(evidenceReference).Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal).ToImmutableArray(),
                "AIPlanning", recordedAt);
        }
        catch (NpgsqlException) { throw new AiPlanningDependencyUnavailableException("PostgreSQL Approved Packages snapshot read is unavailable."); }
        catch (TimeoutException) { throw new AiPlanningDependencyUnavailableException("PostgreSQL Approved Packages snapshot read timed out."); }
        catch (JsonException exception) { throw new InvalidOperationException("Stored Approved Packages evidence is malformed.", exception); }
    }

    private static void Validate(ApprovedPackagesEvidenceRecord record, Guid selectionId,
        string tenantId, string purpose, string storedDigest, string computedDigest,
        string evidenceReference, ImmutableArray<string> storedEvidence, DateTimeOffset recordedAt)
    {
        if (record.SelectionId != selectionId || record.ArchitectureDiscoveryId == Guid.Empty ||
            record.RegistrationId == Guid.Empty || record.RegistrationVersion < 0 ||
            record.PolicyDecisionRequestId == Guid.Empty || !StringComparer.Ordinal.Equals(record.TenantId, tenantId) ||
            !StringComparer.Ordinal.Equals(record.Purpose, purpose) || record.Packages.IsDefaultOrEmpty ||
            record.EvidenceReferences.IsDefaultOrEmpty || record.SelectedAt == default || recordedAt < record.SelectedAt ||
            !StringComparer.OrdinalIgnoreCase.Equals(storedDigest, computedDigest) ||
            !StringComparer.Ordinal.Equals(evidenceReference, $"evidence://approved-packages/{selectionId:D}/sha256/{computedDigest}") ||
            !storedEvidence.Order(StringComparer.Ordinal).SequenceEqual(record.EvidenceReferences.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidOperationException("Stored Approved Packages evidence failed exact binding validation.");
        foreach (var digest in new[] { record.ArchitectureSha256Digest, record.PolicyBundleSha256Digest, record.SelectionSha256Digest })
            GovernedAiPlanningRequest.ValidateDigest(digest, "Approved Packages evidence");
        if (record.Packages.Select(item => item.Coordinate).Distinct().Count() != record.Packages.Length)
            throw new InvalidOperationException("Stored Approved Packages evidence contains duplicate coordinates.");
    }

    private static string SelectionDigest(ApprovedPackagesEvidenceRecord record)
    {
        var canonical = new StringBuilder().Append(record.ArchitectureDiscoveryId.ToString("D")).Append('|')
            .Append(record.ArchitectureSha256Digest.ToLowerInvariant()).Append('|')
            .Append(record.PolicyDecisionRequestId.ToString("D")).Append('|')
            .Append(record.PolicyBundleSha256Digest.ToLowerInvariant());
        foreach (var item in record.Packages)
            canonical.Append('|').Append($"{item.Coordinate.Kind}|{item.Coordinate.Name}|{item.Coordinate.Version}|{item.Coordinate.ContentDigest}").Append(':')
                .Append(item.ProvenanceReference).Append(':').Append(item.SbomReference).Append(':')
                .Append(item.SignatureReference).Append(':').Append(item.ApprovalEvidenceReference).Append(':')
                .Append(item.ApprovalDecidedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)).Append(':')
                .AppendJoin(',', item.SupplyChainEvidenceReferences).Append(':')
                .AppendJoin(',', item.AuthorizationEvidenceReferences);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
