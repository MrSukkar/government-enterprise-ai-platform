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

public sealed class PostgreSqlAuthorizedExistingArchitectureSnapshotReader(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IAuthorizedExistingArchitectureSnapshotReader
{
    private const string SelectSql = """
        SELECT record_json, record_sha256_digest, evidence_reference,
               evidence_references, recorded_at
          FROM software_factory.existing_architecture_evidence
         WHERE tenant_id = @tenant_id AND discovery_id = @discovery_id
        """;

    public async Task<AuthorizedExistingArchitectureDiscoveryReceipt?> LoadAsync(
        Guid architectureDiscoveryId, string tenantId, string purpose,
        CancellationToken cancellationToken)
    {
        if (architectureDiscoveryId == Guid.Empty) throw new ArgumentException("Architecture discovery identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new ApprovedPackagesDependencyUnavailableException("PostgreSQL Existing Architecture snapshots are not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "discovery_id", NpgsqlDbType.Uuid, architectureDiscoveryId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0); var storedDigest = reader.GetString(1);
            var evidenceReference = reader.GetString(2); var storedEvidence = reader.GetFieldValue<string[]>(3).ToImmutableArray();
            var recordedAt = new DateTimeOffset(reader.GetDateTime(4).ToUniversalTime());
            var record = JsonSerializer.Deserialize<ExistingArchitectureEvidenceRecord>(json)
                ?? throw new InvalidOperationException("Stored Existing Architecture evidence is empty.");
            var computedRecordDigest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
            Validate(record, architectureDiscoveryId, tenantId, purpose, storedDigest,
                computedRecordDigest, evidenceReference, storedEvidence, recordedAt);
            if (!StringComparer.OrdinalIgnoreCase.Equals(Digest(record), record.ArchitectureSha256Digest))
                throw new InvalidOperationException("Stored Existing Architecture digest is invalid.");
            return new(record.DiscoveryId, record.SystemsDiscoveryId, record.ContextDiscoveryId,
                record.RegistrationId, record.RegistrationVersion, record.TenantId,
                record.IntentSha256Digest, record.ContextSha256Digest, record.InventorySha256Digest,
                GovernedIntentPolicyOutcome.Permit, IsArchitectureReleased: true, CanAdvance: false,
                record.ArchitectureSha256Digest, record.Items, evidenceReference,
                record.EvidenceReferences.Append(evidenceReference).Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal).ToImmutableArray(), "ApprovedPackages", recordedAt);
        }
        catch (NpgsqlException) { throw new ApprovedPackagesDependencyUnavailableException("PostgreSQL Existing Architecture snapshot read is unavailable."); }
        catch (TimeoutException) { throw new ApprovedPackagesDependencyUnavailableException("PostgreSQL Existing Architecture snapshot read timed out."); }
        catch (JsonException exception) { throw new InvalidOperationException("Stored Existing Architecture evidence is malformed.", exception); }
    }

    private static void Validate(ExistingArchitectureEvidenceRecord record, Guid discoveryId,
        string tenantId, string purpose, string storedDigest, string computedDigest,
        string evidenceReference, ImmutableArray<string> storedEvidence, DateTimeOffset recordedAt)
    {
        if (record.DiscoveryId != discoveryId || record.SystemsDiscoveryId == Guid.Empty ||
            record.ContextDiscoveryId == Guid.Empty || record.RegistrationId == Guid.Empty ||
            record.RegistrationVersion < 0 || record.PolicyDecisionRequestId == Guid.Empty ||
            !StringComparer.Ordinal.Equals(record.TenantId, tenantId) || !StringComparer.Ordinal.Equals(record.Purpose, purpose) ||
            record.Items.IsDefaultOrEmpty || record.EvidenceReferences.IsDefaultOrEmpty || record.DiscoveredAt == default ||
            recordedAt < record.DiscoveredAt || !StringComparer.OrdinalIgnoreCase.Equals(storedDigest, computedDigest) ||
            !StringComparer.Ordinal.Equals(evidenceReference, $"evidence://existing-architecture/{discoveryId:D}/sha256/{computedDigest}") ||
            !storedEvidence.Order(StringComparer.Ordinal).SequenceEqual(record.EvidenceReferences.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidOperationException("Stored Existing Architecture evidence failed exact binding validation.");
        foreach (var digest in new[] { record.IntentSha256Digest, record.ContextSha256Digest,
                     record.InventorySha256Digest, record.PolicyBundleSha256Digest, record.ArchitectureSha256Digest })
            RequireDigest(digest);
        if (record.Items.Any(item => item.ArchitectureItemId == Guid.Empty) ||
            record.Items.Select(item => item.ArchitectureItemId).Distinct().Count() != record.Items.Length)
            throw new InvalidOperationException("Stored Existing Architecture evidence contains invalid items.");
    }

    private static string Digest(ExistingArchitectureEvidenceRecord record)
    {
        var canonical = new StringBuilder().Append(record.SystemsDiscoveryId.ToString("D")).Append('|')
            .Append(record.InventorySha256Digest.ToLowerInvariant()).Append('|')
            .Append(record.PolicyDecisionRequestId.ToString("D")).Append('|')
            .Append(record.PolicyBundleSha256Digest.ToLowerInvariant());
        foreach (var item in record.Items.OrderBy(item => item.ArchitectureItemId))
            canonical.Append('|').Append(item.ArchitectureItemId.ToString("D")).Append(':')
                .Append(item.SystemId.ToString("D")).Append(':').Append(item.RelatedSystemId?.ToString("D") ?? string.Empty)
                .Append(':').Append(item.Kind).Append(':').Append(item.Name).Append(':').Append(item.Description)
                .Append(':').Append(item.RelationshipType ?? string.Empty).Append(':').Append(item.Version)
                .Append(':').Append((int)item.Classification).Append(':').Append(item.Environment)
                .Append(':').Append(item.Lifecycle).Append(':').Append(item.SourceKind)
                .Append(':').Append(item.ApprovedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))
                .Append(':').Append(item.UpdatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))
                .Append(':').AppendJoin(',', item.EvidenceReferences).Append(':').AppendJoin(',', item.ConformanceEvidenceReferences)
                .Append(':').AppendJoin(',', item.AuthorizationEvidenceReferences);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
    private static void RequireDigest(string value) { if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character))) throw new InvalidOperationException("Existing Architecture evidence contains an invalid digest."); }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) => command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
