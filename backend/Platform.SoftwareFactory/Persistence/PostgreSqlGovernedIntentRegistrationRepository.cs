using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.Domain.Security;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlGovernedIntentRegistrationRepository(
    NpgsqlDataSource dataSource,
    IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IGovernedIntentRegistrationRepository
{
    private const string SelectSql = """
        SELECT submission_id, subject_id, classification, purpose, service_name, mission,
               primary_users, intent_sha256_digest, policy_decision_request_id,
               policy_bundle_id, policy_bundle_version, policy_bundle_sha256_digest,
               idempotency_key, version, evidence_references, registration_evidence_reference,
               registered_at
          FROM software_factory.governed_intent_registration
         WHERE tenant_id = @tenant_id AND registration_id = @registration_id
         FOR UPDATE
        """;

    private const string InsertSql = """
        INSERT INTO software_factory.governed_intent_registration
            (tenant_id, registration_id, submission_id, subject_id, classification, purpose,
             service_name, mission, primary_users, intent_sha256_digest,
             policy_decision_request_id, policy_bundle_id, policy_bundle_version,
             policy_bundle_sha256_digest, idempotency_key, version, evidence_references,
             registration_evidence_reference, registered_at)
        VALUES
            (@tenant_id, @registration_id, @submission_id, @subject_id, @classification, @purpose,
             @service_name, @mission, @primary_users, @intent_sha256_digest,
             @policy_decision_request_id, @policy_bundle_id, @policy_bundle_version,
             @policy_bundle_sha256_digest, @idempotency_key, @version, @evidence_references,
             @registration_evidence_reference, @registered_at)
        ON CONFLICT DO NOTHING
        """;

    public async Task<GovernedIntentRegistrationResult> RegisterAtomicallyAsync(
        RegisteredGovernedIntent candidate,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new InvalidOperationException("PostgreSQL intent registration is not validly configured.");

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var existing = await LoadAsync(connection, transaction, candidate.TenantId,
                candidate.RegistrationId, options.CommandTimeoutSeconds, cancellationToken);
            if (existing is not null)
            {
                ValidateUnchanged(candidate, existing);
                await transaction.CommitAsync(cancellationToken);
                return new GovernedIntentRegistrationResult(
                    GovernedIntentRegistrationDisposition.Unchanged, existing);
            }
            if (expectedVersion != -1 || candidate.Version != 0)
                throw new GovernedIntentConcurrencyException(
                    "A new governed intent requires expected version -1 and candidate version 0.");

            var registeredAt = NormalizePostgreSqlTimestamp(candidate.RegisteredAt);
            var normalized = candidate with
            {
                RegisteredAt = registeredAt
            };
            var evidenceDigest = Convert.ToHexString(SHA256.HashData(
                JsonSerializer.SerializeToUtf8Bytes(new
                {
                    normalized.RegistrationId,
                    normalized.SubmissionId,
                    normalized.TenantId,
                    normalized.SubjectId,
                    Classification = normalized.Classification.ToString(),
                    normalized.Purpose,
                    normalized.ServiceName,
                    normalized.Mission,
                    normalized.PrimaryUsers,
                    normalized.IntentSha256Digest,
                    normalized.PolicyDecisionRequestId,
                    normalized.PolicyBundleId,
                    normalized.PolicyBundleVersion,
                    normalized.PolicyBundleSha256Digest,
                    normalized.IdempotencyKey,
                    normalized.Version,
                    EvidenceReferences = normalized.EvidenceReferences,
                    normalized.RegisteredAt
                }))).ToLowerInvariant();
            var persisted = normalized with
            {
                RegistrationEvidenceReference =
                    $"evidence://intent-registration/{candidate.RegistrationId:D}/sha256/{evidenceDigest}"
            };
            var inserted = await InsertAsync(connection, transaction, persisted,
                options.CommandTimeoutSeconds, cancellationToken);
            if (inserted == 0)
            {
                existing = await LoadAsync(connection, transaction, candidate.TenantId,
                    candidate.RegistrationId, options.CommandTimeoutSeconds, cancellationToken)
                    ?? throw new GovernedIntentConcurrencyException(
                        "A conflicting governed intent registration exists outside the authorized key.");
                ValidateUnchanged(candidate, existing);
                await transaction.CommitAsync(cancellationToken);
                return new GovernedIntentRegistrationResult(
                    GovernedIntentRegistrationDisposition.Unchanged, existing);
            }

            await transaction.CommitAsync(cancellationToken);
            return new GovernedIntentRegistrationResult(
                GovernedIntentRegistrationDisposition.Created, persisted);
        }
        catch (PostgresException exception) when (
            exception.SqlState is PostgresErrorCodes.UniqueViolation or
                PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new GovernedIntentConcurrencyException(
                "Governed intent registration encountered a concurrent database change.");
        }
        catch (NpgsqlException exception)
        {
            throw new GovernedIntentPersistenceUnavailableException(
                "Governed intent persistence is unavailable.", exception);
        }
        catch (TimeoutException exception)
        {
            throw new GovernedIntentPersistenceUnavailableException(
                "Governed intent persistence timed out.", exception);
        }
    }

    private static async Task<int> InsertAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        RegisteredGovernedIntent value,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(InsertSql, connection, transaction)
        {
            CommandTimeout = commandTimeoutSeconds
        };
        Add(command, "tenant_id", NpgsqlDbType.Text, value.TenantId);
        Add(command, "registration_id", NpgsqlDbType.Uuid, value.RegistrationId);
        Add(command, "submission_id", NpgsqlDbType.Uuid, value.SubmissionId);
        Add(command, "subject_id", NpgsqlDbType.Text, value.SubjectId);
        Add(command, "classification", NpgsqlDbType.Text, value.Classification.ToString());
        Add(command, "purpose", NpgsqlDbType.Text, value.Purpose);
        Add(command, "service_name", NpgsqlDbType.Text, value.ServiceName);
        Add(command, "mission", NpgsqlDbType.Text, value.Mission);
        Add(command, "primary_users", NpgsqlDbType.Text, value.PrimaryUsers);
        Add(command, "intent_sha256_digest", NpgsqlDbType.Text, value.IntentSha256Digest);
        Add(command, "policy_decision_request_id", NpgsqlDbType.Uuid, value.PolicyDecisionRequestId);
        Add(command, "policy_bundle_id", NpgsqlDbType.Text, value.PolicyBundleId);
        Add(command, "policy_bundle_version", NpgsqlDbType.Text, value.PolicyBundleVersion);
        Add(command, "policy_bundle_sha256_digest", NpgsqlDbType.Text, value.PolicyBundleSha256Digest);
        Add(command, "idempotency_key", NpgsqlDbType.Text, value.IdempotencyKey);
        Add(command, "version", NpgsqlDbType.Bigint, value.Version);
        Add(command, "evidence_references", NpgsqlDbType.Array | NpgsqlDbType.Text, value.EvidenceReferences.ToArray());
        Add(command, "registration_evidence_reference", NpgsqlDbType.Text, value.RegistrationEvidenceReference);
        Add(command, "registered_at", NpgsqlDbType.TimestampTz, value.RegisteredAt);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<RegisteredGovernedIntent?> LoadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string tenantId,
        Guid registrationId,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql, connection, transaction)
        {
            CommandTimeout = commandTimeoutSeconds
        };
        Add(command, "tenant_id", NpgsqlDbType.Text, tenantId);
        Add(command, "registration_id", NpgsqlDbType.Uuid, registrationId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var classificationText = reader.GetString(2);
        if (!Enum.TryParse<DataClassification>(classificationText, ignoreCase: false, out var classification) ||
            !Enum.IsDefined(classification))
            throw new InvalidOperationException("Stored governed intent classification is invalid.");
        return new RegisteredGovernedIntent(
            registrationId, reader.GetGuid(0), tenantId, reader.GetString(1), classification,
            reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
            reader.GetString(7), reader.GetGuid(8), reader.GetString(9), reader.GetString(10),
            reader.GetString(11), reader.GetString(12), reader.GetInt64(13),
            reader.GetFieldValue<string[]>(14).ToImmutableArray(), reader.GetString(15),
            new DateTimeOffset(reader.GetDateTime(16).ToUniversalTime()));
    }

    private static void ValidateUnchanged(
        RegisteredGovernedIntent candidate,
        RegisteredGovernedIntent existing)
    {
        if (existing.RegistrationId != candidate.RegistrationId ||
            existing.SubmissionId != candidate.SubmissionId ||
            !StringComparer.Ordinal.Equals(existing.TenantId, candidate.TenantId) ||
            !StringComparer.Ordinal.Equals(existing.SubjectId, candidate.SubjectId) ||
            existing.Classification != candidate.Classification ||
            !StringComparer.Ordinal.Equals(existing.Purpose, candidate.Purpose) ||
            !StringComparer.Ordinal.Equals(existing.ServiceName, candidate.ServiceName) ||
            !StringComparer.Ordinal.Equals(existing.Mission, candidate.Mission) ||
            !StringComparer.Ordinal.Equals(existing.PrimaryUsers, candidate.PrimaryUsers) ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.IntentSha256Digest, candidate.IntentSha256Digest) ||
            existing.PolicyDecisionRequestId != candidate.PolicyDecisionRequestId ||
            !StringComparer.Ordinal.Equals(existing.PolicyBundleId, candidate.PolicyBundleId) ||
            !StringComparer.Ordinal.Equals(existing.PolicyBundleVersion, candidate.PolicyBundleVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(existing.PolicyBundleSha256Digest, candidate.PolicyBundleSha256Digest) ||
            !StringComparer.Ordinal.Equals(existing.IdempotencyKey, candidate.IdempotencyKey) ||
            existing.Version != candidate.Version ||
            !existing.EvidenceReferences.SequenceEqual(candidate.EvidenceReferences, StringComparer.Ordinal) ||
            existing.RegisteredAt < candidate.RegisteredAt)
            throw new GovernedIntentConcurrencyException(
                "Stored governed intent does not exactly match the authorized idempotent candidate.");
    }

    private static DateTimeOffset NormalizePostgreSqlTimestamp(DateTimeOffset value)
    {
        var utcTicks = value.UtcTicks;
        var remainder = utcTicks % 10;
        return new DateTimeOffset(remainder == 0 ? utcTicks : utcTicks + (10 - remainder), TimeSpan.Zero);
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
