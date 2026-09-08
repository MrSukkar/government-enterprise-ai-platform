using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlStaticValidationDeliveryRunReader(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IStaticValidationDeliveryRunReader
{
    private const string SelectSql = """
        SELECT record_json, record_sha256_digest, evidence_reference, recorded_at
          FROM software_factory.static_validation_delivery_run_snapshots
         WHERE tenant_id = @tenant_id AND run_id = @run_id AND purpose = @purpose
           AND generation_id = @generation_id AND candidate_sha256_digest = @candidate_sha256_digest
        """;

    public async Task<SoftwareDeliveryRun?> LoadAsync(Guid runId, string tenantId, string purpose,
        Guid generationId, string candidateSha256Digest, CancellationToken cancellationToken)
    {
        if (runId == Guid.Empty || generationId == Guid.Empty) throw new ArgumentException("Static run identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        GovernedAiPlanningRequest.ValidateDigest(candidateSha256Digest, "code candidate"); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured) throw new StaticValidationDependencyUnavailableException("Static delivery-run snapshots are not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "run_id", NpgsqlDbType.Uuid, runId);
            Add(command, "purpose", NpgsqlDbType.Text, purpose); Add(command, "generation_id", NpgsqlDbType.Uuid, generationId);
            Add(command, "candidate_sha256_digest", NpgsqlDbType.Text, candidateSha256Digest.ToLowerInvariant());
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0); var storedDigest = reader.GetString(1); var evidence = reader.GetString(2);
            var recordedAt = new DateTimeOffset(reader.GetDateTime(3).ToUniversalTime());
            var run = JsonSerializer.Deserialize<SoftwareDeliveryRun>(json) ?? throw new InvalidOperationException("Stored Static run is empty.");
            var digest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(run))); run.Validate();
            if (run.Id != runId || !StringComparer.Ordinal.Equals(run.TenantId, tenantId) ||
                !StringComparer.OrdinalIgnoreCase.Equals(storedDigest, digest) ||
                !StringComparer.Ordinal.Equals(evidence, $"evidence://delivery-runs/{runId:D}/sha256/{digest}") ||
                run.CurrentStage != DeliveryStage.CodeGeneration || run.History.IsDefaultOrEmpty ||
                run.History.Any(item => string.IsNullOrWhiteSpace(item.EvidenceReference) || item.CompletedAt > recordedAt))
                throw new InvalidOperationException("Stored Static run failed exact binding validation.");
            return run;
        }
        catch (NpgsqlException) { throw new StaticValidationDependencyUnavailableException("Static delivery-run read is unavailable."); }
        catch (TimeoutException) { throw new StaticValidationDependencyUnavailableException("Static delivery-run read timed out."); }
        catch (JsonException exception) { throw new InvalidOperationException("Stored Static run is malformed.", exception); }
    }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) => command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
