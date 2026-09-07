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

public sealed class PostgreSqlAuthorizedExistingSystemsSnapshotReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IAuthorizedExistingSystemsSnapshotReader
{
    private const string SelectSql = """
        SELECT record_json, record_sha256_digest, evidence_reference,
               evidence_references, recorded_at
          FROM software_factory.existing_systems_evidence
         WHERE tenant_id = @tenant_id AND discovery_id = @discovery_id
        """;

    public async Task<AuthorizedExistingSystemsDiscoveryReceipt?> LoadAsync(
        Guid systemsDiscoveryId, string tenantId, string purpose,
        CancellationToken cancellationToken)
    {
        if (systemsDiscoveryId == Guid.Empty) throw new ArgumentException("A systems discovery identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new ExistingArchitectureDependencyUnavailableException("PostgreSQL Existing Systems snapshots are not validly configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            command.Parameters.Add(new NpgsqlParameter("tenant_id", NpgsqlDbType.Text) { Value = tenantId });
            command.Parameters.Add(new NpgsqlParameter("discovery_id", NpgsqlDbType.Uuid) { Value = systemsDiscoveryId });
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0);
            var storedDigest = reader.GetString(1);
            var evidenceReference = reader.GetString(2);
            var storedEvidence = reader.GetFieldValue<string[]>(3).ToImmutableArray();
            var recordedAt = new DateTimeOffset(reader.GetDateTime(4).ToUniversalTime());
            var record = JsonSerializer.Deserialize<ExistingSystemsEvidenceRecord>(json)
                ?? throw new InvalidOperationException("Stored Existing Systems evidence is empty.");
            var computedRecordDigest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
            Validate(record, systemsDiscoveryId, tenantId, purpose, storedDigest,
                computedRecordDigest, evidenceReference, storedEvidence, recordedAt);
            var inventoryDigest = Digest(record);
            if (!StringComparer.OrdinalIgnoreCase.Equals(inventoryDigest, record.InventorySha256Digest))
                throw new InvalidOperationException("Stored Existing Systems inventory digest is invalid.");
            return new AuthorizedExistingSystemsDiscoveryReceipt(
                record.DiscoveryId, record.ContextDiscoveryId, record.RegistrationId,
                record.RegistrationVersion, record.TenantId, record.IntentSha256Digest,
                record.ContextSha256Digest, GovernedIntentPolicyOutcome.Permit,
                IsInventoryReleased: true, CanAdvance: false, record.InventorySha256Digest,
                record.Systems, evidenceReference,
                record.EvidenceReferences.Append(evidenceReference).Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal).ToImmutableArray(),
                "ExistingArchitecture", recordedAt);
        }
        catch (NpgsqlException)
        {
            throw new ExistingArchitectureDependencyUnavailableException("PostgreSQL Existing Systems snapshot read is unavailable.");
        }
        catch (TimeoutException)
        {
            throw new ExistingArchitectureDependencyUnavailableException("PostgreSQL Existing Systems snapshot read timed out.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Stored Existing Systems evidence is malformed.", exception);
        }
    }

    private static void Validate(ExistingSystemsEvidenceRecord record, Guid discoveryId,
        string tenantId, string purpose, string storedDigest, string computedDigest,
        string evidenceReference, ImmutableArray<string> storedEvidence, DateTimeOffset recordedAt)
    {
        if (record.DiscoveryId != discoveryId || record.ContextDiscoveryId == Guid.Empty ||
            record.RegistrationId == Guid.Empty || record.RegistrationVersion < 0 ||
            record.PolicyDecisionRequestId == Guid.Empty ||
            !StringComparer.Ordinal.Equals(record.TenantId, tenantId) ||
            !StringComparer.Ordinal.Equals(record.Purpose, purpose) || record.Systems.IsDefaultOrEmpty ||
            record.EvidenceReferences.IsDefaultOrEmpty || record.DiscoveredAt == default ||
            recordedAt < record.DiscoveredAt ||
            !StringComparer.OrdinalIgnoreCase.Equals(storedDigest, computedDigest) ||
            !StringComparer.Ordinal.Equals(evidenceReference,
                $"evidence://existing-systems/{discoveryId:D}/sha256/{computedDigest}") ||
            !storedEvidence.Order(StringComparer.Ordinal).SequenceEqual(record.EvidenceReferences.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidOperationException("Stored Existing Systems evidence failed exact binding validation.");
        foreach (var digest in new[] { record.IntentSha256Digest, record.ContextSha256Digest,
                     record.PolicyBundleSha256Digest, record.InventorySha256Digest })
            if (digest.Length != 64 || digest.Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidOperationException("Stored Existing Systems evidence contains an invalid digest.");
        if (record.Systems.Any(system => system.SystemId == Guid.Empty) ||
            record.Systems.Select(system => system.SystemId).Distinct().Count() != record.Systems.Length)
            throw new InvalidOperationException("Stored Existing Systems evidence contains invalid system identities.");
    }

    private static string Digest(ExistingSystemsEvidenceRecord record)
    {
        var canonicalSystems = record.Systems.Select(system => string.Join('\u001e',
            system.SystemId.ToString("D"), system.Type, system.State, system.OwnerId,
            system.Classification, system.Source, system.SourceKind,
            system.Confidence.ToString(CultureInfo.InvariantCulture), system.Lifecycle,
            system.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            system.UpdatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            string.Join('\u001d', system.PolicyReferences), string.Join('\u001d', system.PermittedActions),
            string.Join('\u001d', system.EvidenceReferences), string.Join('\u001d', system.AuthorizationEvidenceReferences),
            string.Join('\u001c', system.Relationships.Select(relationship => string.Join('\u001b',
                relationship.TargetSystemId.ToString("D"), relationship.RelationshipType,
                relationship.KnowledgeState, relationship.Confidence.ToString(CultureInfo.InvariantCulture),
                relationship.Source, relationship.ObservedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                string.Join('\u001a', relationship.EvidenceReferences),
                string.Join('\u001a', relationship.AuthorizationEvidenceReferences))))));
        var canonical = string.Join('\u001f', record.RegistrationId.ToString("D"),
            record.RegistrationVersion.ToString(CultureInfo.InvariantCulture), record.ContextDiscoveryId.ToString("D"),
            record.IntentSha256Digest.ToLowerInvariant(), record.ContextSha256Digest.ToLowerInvariant(),
            record.PolicyDecisionRequestId.ToString("D"), record.PolicyBundleId, record.PolicyBundleVersion,
            record.PolicyBundleSha256Digest.ToLowerInvariant(), string.Join('\u0019', canonicalSystems));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
