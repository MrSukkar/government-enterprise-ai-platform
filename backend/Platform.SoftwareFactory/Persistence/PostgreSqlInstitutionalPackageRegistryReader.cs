using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.SoftwareFactory.InternalService;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlInstitutionalPackageRegistryReader(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions)
    : IInstitutionalPackageRegistryReader
{
    private const string SelectSql = """
        SELECT record_json, record_sha256_digest
          FROM software_factory.institutional_package_catalog
         WHERE package_kind = @package_kind AND package_name = @package_name
           AND package_version = @package_version AND content_digest = @content_digest
           AND @tenant_id = ANY(allowed_tenant_ids)
           AND @environment = ANY(allowed_environments)
        """;

    public async Task<InstitutionalPackage?> FindExactAsync(PackageCoordinate coordinate,
        string tenantId, string environment, CancellationToken cancellationToken)
    {
        GovernedApprovedPackagesSelectionRequest.ValidateExactCoordinate(coordinate);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(environment);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new ApprovedPackagesDependencyUnavailableException("The institutional package catalog is not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "package_kind", NpgsqlDbType.Text, coordinate.Kind.ToString()); Add(command, "package_name", NpgsqlDbType.Text, coordinate.Name);
            Add(command, "package_version", NpgsqlDbType.Text, coordinate.Version); Add(command, "content_digest", NpgsqlDbType.Text, coordinate.ContentDigest);
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "environment", NpgsqlDbType.Text, environment);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var json = reader.GetString(0); var storedDigest = reader.GetString(1);
            var package = JsonSerializer.Deserialize<InstitutionalPackage>(json)
                ?? throw new InvalidOperationException("Institutional package record is empty.");
            var digest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(package)));
            if (!StringComparer.OrdinalIgnoreCase.Equals(storedDigest, digest) || package.Coordinate != coordinate ||
                !package.AllowedTenantIds.Contains(tenantId) || !package.AllowedEnvironments.Contains(environment))
                throw new InvalidOperationException("Institutional package record failed exact cryptographic binding.");
            package.Validate();
            return package;
        }
        catch (NpgsqlException) { throw new ApprovedPackagesDependencyUnavailableException("The institutional package catalog is unavailable."); }
        catch (TimeoutException) { throw new ApprovedPackagesDependencyUnavailableException("The institutional package catalog timed out."); }
        catch (JsonException exception) { throw new InvalidOperationException("Institutional package record is malformed.", exception); }
    }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) => command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
