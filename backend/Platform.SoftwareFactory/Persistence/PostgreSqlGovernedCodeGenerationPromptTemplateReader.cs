using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.Domain.Security;
using Platform.Evidence.Chain;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlGovernedCodeGenerationPromptTemplateReader(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> persistenceOptions,
    IOptions<CodeGenerationRuntimeOptions> runtimeOptions) : IGovernedCodeGenerationPromptTemplateReader
{
    private const string SelectSql = """
        SELECT content_sha256_digest, content, allowed_tenant_ids, allowed_purposes, allowed_environments,
               maximum_classification, is_code_generation_approved, is_active, approved_at,
               signature_algorithm, signature_key_id, signature_base64, certificate_chain_reference,
               signed_at, signature_evidence_reference
          FROM software_factory.governed_code_generation_prompt_templates
         WHERE template_id = @template_id AND template_version = @template_version
           AND @tenant_id = ANY(allowed_tenant_ids) AND @purpose = ANY(allowed_purposes)
           AND @environment = ANY(allowed_environments)
           AND is_code_generation_approved = TRUE AND is_active = TRUE
        """;

    public async Task<GovernedCodeGenerationPromptTemplate?> LoadExactAsync(string templateId, string version,
        string tenantId, string purpose, string environment, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);
        var database = persistenceOptions.Value; var trust = runtimeOptions.Value;
        if (!database.IsOperationallyConfigured || !trust.IsOperationallyConfigured)
            throw new CodeGenerationDependencyUnavailableException("Code Generation prompt trust or persistence is not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = database.CommandTimeoutSeconds };
            Add(command, "template_id", NpgsqlDbType.Text, templateId); Add(command, "template_version", NpgsqlDbType.Text, version);
            Add(command, "tenant_id", NpgsqlDbType.Text, tenantId); Add(command, "purpose", NpgsqlDbType.Text, purpose);
            Add(command, "environment", NpgsqlDbType.Text, environment);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            var digest = reader.GetString(0); var content = reader.GetString(1);
            var tenants = reader.GetFieldValue<string[]>(2).ToImmutableHashSet(StringComparer.Ordinal);
            var purposes = reader.GetFieldValue<string[]>(3).ToImmutableHashSet(StringComparer.Ordinal);
            var environments = reader.GetFieldValue<string[]>(4).ToImmutableHashSet(StringComparer.Ordinal);
            if (!Enum.TryParse<DataClassification>(reader.GetString(5), false, out var classification) || !Enum.IsDefined(classification))
                throw new InvalidOperationException("Code Generation prompt classification is invalid.");
            var approved = reader.GetBoolean(6); var active = reader.GetBoolean(7);
            var approvedAt = new DateTimeOffset(reader.GetDateTime(8).ToUniversalTime());
            var signature = new SignatureEnvelope(reader.GetString(9), reader.GetString(10), reader.GetString(11),
                reader.GetString(12), new DateTimeOffset(reader.GetDateTime(13).ToUniversalTime()));
            var signatureEvidence = reader.GetString(14);
            var computedDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
            if (!StringComparer.OrdinalIgnoreCase.Equals(digest, computedDigest) || tenants.IsEmpty || purposes.IsEmpty ||
                environments.IsEmpty || !approved || !active || approvedAt == default || signature.SignedAt < approvedAt ||
                string.IsNullOrWhiteSpace(signatureEvidence))
                throw new InvalidOperationException("Code Generation prompt failed content or lifecycle validation.");
            var signedPayload = string.Join('|', templateId, version, computedDigest,
                string.Join(',', tenants.Order(StringComparer.Ordinal)), string.Join(',', purposes.Order(StringComparer.Ordinal)),
                string.Join(',', environments.Order(StringComparer.Ordinal)), classification, approved, active,
                approvedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), signature.Algorithm,
                signatureEvidence, signature.KeyId, signature.CertificateChainReference,
                signature.SignedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            if (!AiPlanningSignatureVerifier.Verify(trust.SignatureAlgorithm, trust.PromptTrustedPublicKeysPem,
                    signature, SHA256.HashData(Encoding.UTF8.GetBytes(signedPayload))))
                throw new UnauthorizedAccessException("Code Generation prompt signature is invalid.");
            return new(templateId, version, computedDigest, content, tenants, purposes, environments,
                classification, true, true, signatureEvidence, true, approvedAt);
        }
        catch (NpgsqlException) { throw new CodeGenerationDependencyUnavailableException("Code Generation prompt read is unavailable."); }
        catch (TimeoutException) { throw new CodeGenerationDependencyUnavailableException("Code Generation prompt read timed out."); }
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
