using System.Collections.Immutable;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.InternalService;

namespace Platform.SoftwareFactory.Persistence;

public sealed class PostgreSqlCodeGenerationContextAuthorizer(
    NpgsqlDataSource dataSource, IOptions<PostgreSqlIntentRegistrationOptions> configuredOptions,
    IAccessPolicyEvaluator accessPolicyEvaluator) : ICodeGenerationContextAuthorizer
{
    private const string SelectSql = """
        SELECT source_station, record_json, record_sha256_digest, evidence_reference, evidence_references, recorded_at
          FROM (
            SELECT 'enterprise-context' AS source_station, record_json, record_sha256_digest, evidence_reference, evidence_references, recorded_at
              FROM software_factory.enterprise_context_evidence WHERE tenant_id = @tenant_id AND evidence_reference = @evidence_reference
            UNION ALL
            SELECT 'existing-systems', record_json, record_sha256_digest, evidence_reference, evidence_references, recorded_at
              FROM software_factory.existing_systems_evidence WHERE tenant_id = @tenant_id AND evidence_reference = @evidence_reference
            UNION ALL
            SELECT 'existing-architecture', record_json, record_sha256_digest, evidence_reference, evidence_references, recorded_at
              FROM software_factory.existing_architecture_evidence WHERE tenant_id = @tenant_id AND evidence_reference = @evidence_reference
            UNION ALL
            SELECT 'approved-packages', record_json, record_sha256_digest, evidence_reference, evidence_references, recorded_at
              FROM software_factory.approved_packages_evidence WHERE tenant_id = @tenant_id AND evidence_reference = @evidence_reference
            UNION ALL
            SELECT 'ai-planning', record_json, record_sha256_digest, evidence_reference, evidence_references, recorded_at
              FROM software_factory.ai_planning_evidence
             WHERE tenant_id = @tenant_id AND planning_id = @planning_id AND evidence_reference = @evidence_reference
          ) authorized_context
        """;

    public async Task<CodeGenerationContextAuthorizationDecision> AuthorizeAsync(
        CodeGenerationContextAuthorizationRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request); var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured) throw new CodeGenerationDependencyUnavailableException("PostgreSQL Code Generation context is not configured.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(SelectSql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            Add(command, "tenant_id", NpgsqlDbType.Text, request.TenantId);
            Add(command, "planning_id", NpgsqlDbType.Uuid, request.PlanningId);
            Add(command, "evidence_reference", NpgsqlDbType.Text, request.ContextReference);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) throw new UnauthorizedAccessException("Authorized Code Generation context was not found.");
            var station = reader.GetString(0); var json = reader.GetString(1); var storedDigest = reader.GetString(2);
            var reference = reader.GetString(3); var storedEvidence = reader.GetFieldValue<string[]>(4).ToImmutableArray();
            var recordedAt = new DateTimeOffset(reader.GetDateTime(5).ToUniversalTime());
            if (await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Code Generation context reference resolved to multiple records.");
            var material = Parse(station, json, request, recordedAt);
            var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material.Content)));
            if (!StringComparer.Ordinal.Equals(reference, request.ContextReference) ||
                !StringComparer.OrdinalIgnoreCase.Equals(storedDigest, digest) || storedEvidence.IsDefaultOrEmpty ||
                material.Classification > request.MaximumClassification)
                throw new UnauthorizedAccessException("Code Generation context failed exact binding validation.");
            var access = accessPolicyEvaluator.Evaluate(new AccessRequest(request.Identity, request.Purpose,
                "ai-context.read", reference, request.TenantId, material.Classification, request.RequiredRoles,
                ImmutableHashSet.Create(StringComparer.Ordinal, "developer.internal-service.code-generation.create"),
                request.SubjectId, false));
            var authorizationDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
                request.AuthorizationRequestId.ToString("D"), request.GenerationId.ToString("D"), request.PlanningId.ToString("D"),
                request.TenantId, request.SubjectId, request.Purpose, request.Environment, reference, digest,
                material.Classification, access.IsAllowed, access.Code))));
            var evidence = request.EvidenceReferences.Concat(storedEvidence).Append(reference)
                .Append($"evidence://code-generation/context-authorization/{request.AuthorizationRequestId:D}/sha256/{authorizationDigest}")
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
            var item = new AiDevelopmentContextItem(reference, station, material.Classification, digest, material.Content, evidence);
            return new(request.AuthorizationRequestId, request.GenerationId, request.TenantId, reference,
                access.IsAllowed, access.Code, access.IsAllowed ? item : null, evidence, request.RequestedAt);
        }
        catch (NpgsqlException) { throw new CodeGenerationDependencyUnavailableException("PostgreSQL Code Generation context read is unavailable."); }
        catch (TimeoutException) { throw new CodeGenerationDependencyUnavailableException("PostgreSQL Code Generation context read timed out."); }
        catch (JsonException exception) { throw new InvalidOperationException("Authorized Code Generation context is malformed.", exception); }
    }

    private static (string Content, DataClassification Classification) Parse(string station, string json,
        CodeGenerationContextAuthorizationRequest request, DateTimeOffset recordedAt) => station switch
        {
            "enterprise-context" => Normalize(JsonSerializer.Deserialize<EnterpriseContextEvidenceRecord>(json)
                ?? throw new InvalidOperationException("Enterprise Context evidence is empty."), request, recordedAt),
            "existing-systems" => Normalize(JsonSerializer.Deserialize<ExistingSystemsEvidenceRecord>(json)
                ?? throw new InvalidOperationException("Existing Systems evidence is empty."), request, recordedAt),
            "existing-architecture" => Normalize(JsonSerializer.Deserialize<ExistingArchitectureEvidenceRecord>(json)
                ?? throw new InvalidOperationException("Existing Architecture evidence is empty."), request, recordedAt),
            "approved-packages" => Normalize(JsonSerializer.Deserialize<ApprovedPackagesEvidenceRecord>(json)
                ?? throw new InvalidOperationException("Approved Packages evidence is empty."), request, recordedAt),
            "ai-planning" => Normalize(JsonSerializer.Deserialize<AiPlanningEvidenceRecord>(json)
                ?? throw new InvalidOperationException("AI Planning evidence is empty."), request, recordedAt),
            _ => throw new UnauthorizedAccessException("Code Generation context source is not approved.")
        };

    private static (string, DataClassification) Normalize(EnterpriseContextEvidenceRecord value,
        CodeGenerationContextAuthorizationRequest request, DateTimeOffset recordedAt)
    { ValidateCommon(value.TenantId, value.Purpose, value.DiscoveredAt, value.EvidenceReferences, request, recordedAt); return (JsonSerializer.Serialize(value), value.Items.Max(item => item.Classification)); }
    private static (string, DataClassification) Normalize(ExistingSystemsEvidenceRecord value,
        CodeGenerationContextAuthorizationRequest request, DateTimeOffset recordedAt)
    { ValidateCommon(value.TenantId, value.Purpose, value.DiscoveredAt, value.EvidenceReferences, request, recordedAt); return (JsonSerializer.Serialize(value), value.Systems.Max(item => item.Classification)); }
    private static (string, DataClassification) Normalize(ExistingArchitectureEvidenceRecord value,
        CodeGenerationContextAuthorizationRequest request, DateTimeOffset recordedAt)
    { ValidateCommon(value.TenantId, value.Purpose, value.DiscoveredAt, value.EvidenceReferences, request, recordedAt); return (JsonSerializer.Serialize(value), value.Items.Max(item => item.Classification)); }
    private static (string, DataClassification) Normalize(ApprovedPackagesEvidenceRecord value,
        CodeGenerationContextAuthorizationRequest request, DateTimeOffset recordedAt)
    { ValidateCommon(value.TenantId, value.Purpose, value.SelectedAt, value.EvidenceReferences, request, recordedAt); return (JsonSerializer.Serialize(value), request.MaximumClassification); }
    private static (string, DataClassification) Normalize(AiPlanningEvidenceRecord value,
        CodeGenerationContextAuthorizationRequest request, DateTimeOffset recordedAt)
    {
        ValidateCommon(value.TenantId, value.Purpose, value.PlannedAt, value.EvidenceReferences, request, recordedAt);
        if (value.PlanningId != request.PlanningId || value.Candidate.EvidenceReferences.IsDefaultOrEmpty || !value.Evaluation.IsAccepted)
            throw new UnauthorizedAccessException("AI Planning context does not match Code Generation.");
        return (JsonSerializer.Serialize(value), request.MaximumClassification);
    }

    private static void ValidateCommon(string tenantId, string purpose, DateTimeOffset createdAt,
        ImmutableArray<string> evidence, CodeGenerationContextAuthorizationRequest request, DateTimeOffset recordedAt)
    {
        if (!StringComparer.Ordinal.Equals(tenantId, request.TenantId) || !StringComparer.Ordinal.Equals(purpose, request.Purpose) ||
            createdAt == default || createdAt > recordedAt || evidence.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Code Generation context scope is invalid.");
    }

    private static void ValidateRequest(CodeGenerationContextAuthorizationRequest request)
    {
        if (request.AuthorizationRequestId == Guid.Empty || request.GenerationId == Guid.Empty || request.PlanningId == Guid.Empty ||
            request.RequestedAt == default || request.RequiredRoles.IsEmpty ||
            !request.AllowedContextReferences.Contains(request.ContextReference) || request.EvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Code Generation context authorization request is incomplete.");
        if (!request.Identity.IsAuthenticated || !StringComparer.Ordinal.Equals(request.Identity.SubjectId, request.SubjectId) ||
            !StringComparer.Ordinal.Equals(request.Identity.TenantId, request.TenantId))
            throw new UnauthorizedAccessException("Code Generation context identity is mismatched.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose); ArgumentException.ThrowIfNullOrWhiteSpace(request.Environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ContextReference);
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value });
}
