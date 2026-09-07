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

public sealed class PostgreSqlAuthorizedEnterpriseContextSnapshotReader(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IAuthorizedEnterpriseContextSnapshotReader
{
    private const string ReadSql = """
        SELECT registration_id, registration_version, context_sha256_digest,
               record_sha256_digest, record_json, evidence_reference,
               evidence_references, recorded_at
          FROM software_factory.enterprise_context_evidence
         WHERE tenant_id = @tenant_id AND discovery_id = @discovery_id
        """;

    public async Task<AuthorizedEnterpriseContextDiscoveryReceipt?> LoadAsync(
        Guid contextDiscoveryId,
        string tenantId,
        string purpose,
        CancellationToken cancellationToken)
    {
        if (contextDiscoveryId == Guid.Empty)
            throw new InvalidOperationException("Enterprise Context discovery identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new ExistingSystemsDependencyUnavailableException(
                "PostgreSQL Enterprise Context reading is not validly configured.");

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(ReadSql, connection)
            {
                CommandTimeout = options.CommandTimeoutSeconds
            };
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId);
            Add(command, "discovery_id", NpgsqlDbType.Uuid, contextDiscoveryId);
            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SingleRow | CommandBehavior.SequentialAccess, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;

            var registrationId = reader.GetGuid(0);
            var registrationVersion = reader.GetInt64(1);
            var contextDigest = reader.GetString(2);
            var recordDigest = reader.GetString(3);
            var recordJson = reader.GetString(4);
            var evidenceReference = reader.GetString(5);
            var storedEvidence = reader.GetFieldValue<string[]>(6).ToImmutableArray();
            var recordedAt = new DateTimeOffset(reader.GetDateTime(7).ToUniversalTime());
            var record = JsonSerializer.Deserialize<EnterpriseContextEvidenceRecord>(recordJson)
                ?? throw new InvalidOperationException("Stored Enterprise Context evidence is malformed.");
            ValidateStored(contextDiscoveryId, tenantId, purpose, registrationId, registrationVersion,
                contextDigest, recordDigest, evidenceReference, storedEvidence, recordedAt, record);

            return new AuthorizedEnterpriseContextDiscoveryReceipt(
                record.DiscoveryId, record.RegistrationId, record.RegistrationVersion,
                record.TenantId, record.IntentSha256Digest, GovernedIntentPolicyOutcome.Permit,
                IsContextReleased: true, CanAdvance: false, record.ContextSha256Digest,
                record.Items, evidenceReference,
                record.EvidenceReferences.Append(evidenceReference).Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal).ToImmutableArray(),
                "Separately approved Existing Systems discovery", recordedAt);
        }
        catch (NpgsqlException)
        {
            throw new ExistingSystemsDependencyUnavailableException(
                "PostgreSQL Enterprise Context reading is unavailable.");
        }
        catch (TimeoutException)
        {
            throw new ExistingSystemsDependencyUnavailableException(
                "PostgreSQL Enterprise Context reading timed out.");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Stored Enterprise Context evidence is malformed.");
        }
    }

    private static void ValidateStored(
        Guid discoveryId, string tenantId, string purpose, Guid registrationId, long registrationVersion,
        string contextDigest, string recordDigest, string evidenceReference,
        ImmutableArray<string> storedEvidence, DateTimeOffset recordedAt,
        EnterpriseContextEvidenceRecord record)
    {
        var reserializedDigest = Convert.ToHexStringLower(
            SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
        var evidenceDigest = evidenceReference.Split('/').LastOrDefault();
        if (record.DiscoveryId != discoveryId || record.RegistrationId != registrationId ||
            record.RegistrationVersion != registrationVersion ||
            !StringComparer.Ordinal.Equals(record.TenantId, tenantId) ||
            !StringComparer.Ordinal.Equals(record.Purpose, purpose) ||
            !StringComparer.OrdinalIgnoreCase.Equals(record.ContextSha256Digest, contextDigest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(recordDigest, reserializedDigest) ||
            !StringComparer.OrdinalIgnoreCase.Equals(evidenceDigest, recordDigest) ||
            !StringComparer.Ordinal.Equals(evidenceReference,
                $"evidence://enterprise-context/{discoveryId:D}/sha256/{recordDigest.ToLowerInvariant()}") ||
            record.DiscoveredAt != recordedAt || record.PolicyDecisionRequestId == Guid.Empty ||
            record.RegistrationId == Guid.Empty || record.RegistrationVersion < 0 ||
            record.Items.IsDefault || record.EvidenceReferences.IsDefaultOrEmpty ||
            !record.EvidenceReferences.SequenceEqual(storedEvidence) ||
            !StringComparer.OrdinalIgnoreCase.Equals(record.ContextSha256Digest, ComputeContextDigest(record)))
            throw new UnauthorizedAccessException(
                "Stored Enterprise Context evidence failed cryptographic or identity validation.");
        foreach (var value in new[] { record.SubjectId, record.Purpose,
                     record.PolicyBundleId, record.PolicyBundleVersion }
                 .Concat(record.EvidenceReferences))
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        RequireSha256(record.IntentSha256Digest);
        RequireSha256(record.PolicyBundleSha256Digest);
        RequireSha256(record.ContextSha256Digest);
        foreach (var item in record.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ResourceId) || string.IsNullOrWhiteSpace(item.Content) ||
                string.IsNullOrWhiteSpace(item.Source) || !Enum.IsDefined(item.Classification) ||
                !Enum.IsDefined(item.Modality) || item.Relevance is < 0 or > 1 ||
                item.EvidenceReferences.IsDefaultOrEmpty)
                throw new UnauthorizedAccessException("Stored Enterprise Context item is malformed.");
            foreach (var value in item.EvidenceReferences)
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
    }

    private static string ComputeContextDigest(EnterpriseContextEvidenceRecord record)
    {
        var canonicalItems = record.Items.Select(item => string.Join('\u001e',
            item.ResourceId, item.Classification, item.Modality,
            item.Relevance.ToString(CultureInfo.InvariantCulture), item.Source,
            item.Content, string.Join('\u001d', item.EvidenceReferences)));
        var canonical = string.Join('\u001f', record.RegistrationId.ToString("D"),
            record.RegistrationVersion.ToString(CultureInfo.InvariantCulture), record.TenantId,
            record.IntentSha256Digest.ToLowerInvariant(), record.PolicyDecisionRequestId.ToString("D"),
            record.PolicyBundleId, record.PolicyBundleVersion,
            record.PolicyBundleSha256Digest.ToLowerInvariant(), string.Join('\u001c', canonicalItems));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void RequireSha256(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new UnauthorizedAccessException("Stored Enterprise Context evidence contains an invalid SHA-256 digest.");
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
