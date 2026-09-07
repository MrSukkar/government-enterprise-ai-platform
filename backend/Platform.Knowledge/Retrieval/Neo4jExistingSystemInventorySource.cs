using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Integrations.ExistingSystems;

namespace Platform.Knowledge.Retrieval;

public sealed class Neo4jExistingSystemInventorySource(
    IDriver driver,
    IOptions<Neo4jEnterpriseGraphOptions> configuredOptions) : IExistingSystemInventorySource
{
    private const string ScopedInventoryCypher = """
        UNWIND $systemIds AS authorizedSystemId
        MATCH (system:EnterpriseObject {tenantId: $tenantId, resourceId: authorizedSystemId})
        WHERE system.classificationRank <= $maximumClassificationRank
          AND system.sourceKind = $sourceKind
        OPTIONAL MATCH (system)-[relationship]->(target:EnterpriseObject)
        WHERE target.tenantId = $tenantId
          AND target.resourceId IN $systemIds
          AND type(relationship) IN $relationshipTypes
        WITH system, collect(CASE WHEN relationship IS NULL THEN null ELSE {
            targetSystemId: target.resourceId,
            relationshipType: type(relationship),
            knowledgeState: relationship.knowledgeState,
            confidence: relationship.confidence,
            source: relationship.source,
            evidenceReferences: relationship.evidenceReferences,
            observedAt: toString(relationship.observedAt)
        } END) AS relationships
        RETURN system.resourceId AS systemId,
               system.tenantId AS tenantId,
               system.type AS systemType,
               system.state AS state,
               system.ownerId AS ownerId,
               system.classification AS classification,
               relationships,
               system.policyReferences AS policyReferences,
               system.permittedActions AS permittedActions,
               system.source AS source,
               system.sourceKind AS sourceKind,
               system.confidence AS confidence,
               system.evidenceReferences AS evidenceReferences,
               system.lifecycle AS lifecycle,
               toString(system.createdAt) AS createdAt,
               toString(system.updatedAt) AS updatedAt,
               coalesce(system.credentialsIncluded, false) AS credentialsIncluded,
               coalesce(system.liveSessionIncluded, false) AS liveSessionIncluded,
               coalesce(system.executableCommandIncluded, false) AS executableCommandIncluded,
               coalesce(system.externalEffectOccurred, false) AS externalEffectOccurred
        ORDER BY system.resourceId
        LIMIT $maximumResults
        """;

    public string SourceKind => "enterprise-graph";

    public async Task<IReadOnlyCollection<ExistingSystemInventoryCandidate>> DiscoverAsync(
        ExistingSystemInventoryScope scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new KnowledgeRetrievalSourceUnavailableException(
                "The sovereign Enterprise Graph is not validly configured.");
        if (scope.AllowedSystemIds.IsEmpty || scope.AllowedRelationshipTypes.IsEmpty ||
            scope.AllowedSourceKinds.Count != 1 || !scope.AllowedSourceKinds.Contains(SourceKind) ||
            scope.MaximumResults <= 0 || scope.MaximumResults > options.MaximumRecords ||
            string.IsNullOrWhiteSpace(scope.TenantId) || string.IsNullOrWhiteSpace(scope.Purpose))
            throw new UnauthorizedAccessException("Existing Systems Graph scope is empty, unavailable, or excessive.");
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var session = driver.AsyncSession(configuration => configuration
                .WithDatabase(options.Database).WithDefaultAccessMode(AccessMode.Read));
            var records = await session.ExecuteReadAsync(async transaction =>
            {
                var cursor = await transaction.RunAsync(ScopedInventoryCypher, new
                {
                    tenantId = scope.TenantId,
                    systemIds = scope.AllowedSystemIds.Select(id => id.Value.ToString("D"))
                        .Order(StringComparer.Ordinal).ToArray(),
                    maximumClassificationRank = (int)scope.MaximumClassification,
                    relationshipTypes = scope.AllowedRelationshipTypes.Order(StringComparer.Ordinal).ToArray(),
                    sourceKind = SourceKind,
                    maximumResults = scope.MaximumResults
                });
                return await cursor.ToListAsync(MapRecord);
            }, configuration => configuration.WithTimeout(TimeSpan.FromSeconds(options.QueryTimeoutSeconds)));
            cancellationToken.ThrowIfCancellationRequested();
            if (records.Count > scope.MaximumResults)
                throw new InvalidOperationException("Enterprise Graph exceeded the authorized Existing Systems result bound.");
            return records;
        }
        catch (Neo4jException exception)
        {
            throw new KnowledgeRetrievalSourceUnavailableException(
                "The sovereign Enterprise Graph Existing Systems source is unavailable.", exception);
        }
        catch (TimeoutException exception)
        {
            throw new KnowledgeRetrievalSourceUnavailableException(
                "The sovereign Enterprise Graph Existing Systems query timed out.", exception);
        }
    }

    private static ExistingSystemInventoryCandidate MapRecord(IRecord record)
    {
        var systemId = ParseGuid(record["systemId"].As<string>(), "system");
        var classification = ParseEnum<DataClassification>(record["classification"].As<string>(), "classification");
        var lifecycle = ParseEnum<LifecycleState>(record["lifecycle"].As<string>(), "lifecycle");
        var relationshipMaps = record["relationships"].As<List<Dictionary<string, object>>>()
            .Where(item => item is not null).ToArray();
        var relationships = relationshipMaps.Select(MapRelationship).ToImmutableArray();
        var system = new EnterpriseObject
        {
            Id = new EnterpriseObjectId(systemId),
            TenantId = record["tenantId"].As<string>(),
            Type = record["systemType"].As<string>(),
            State = record["state"].As<string>(),
            OwnerId = record["ownerId"].As<string>(),
            Classification = classification,
            Relationships = relationships,
            PolicyReferences = record["policyReferences"].As<List<string>>().ToImmutableArray(),
            PermittedActions = record["permittedActions"].As<List<string>>().ToImmutableArray(),
            Source = record["source"].As<string>(),
            Confidence = Convert.ToDecimal(record["confidence"].As<double>(), CultureInfo.InvariantCulture),
            EvidenceReferences = record["evidenceReferences"].As<List<string>>().ToImmutableArray(),
            Lifecycle = lifecycle,
            CreatedAt = ParseTimestamp(record["createdAt"].As<string>(), "createdAt"),
            UpdatedAt = ParseTimestamp(record["updatedAt"].As<string>(), "updatedAt")
        }.Validate();
        return new ExistingSystemInventoryCandidate(record["sourceKind"].As<string>(), system,
            record["credentialsIncluded"].As<bool>(), record["liveSessionIncluded"].As<bool>(),
            record["executableCommandIncluded"].As<bool>(), record["externalEffectOccurred"].As<bool>());
    }

    private static EnterpriseRelationship MapRelationship(Dictionary<string, object> values)
    {
        var targetId = ParseGuid(RequiredString(values, "targetSystemId"), "relationship target");
        var relationship = new EnterpriseRelationship(new EnterpriseObjectId(targetId),
            RequiredString(values, "relationshipType"),
            ParseEnum<RelationshipKnowledgeState>(RequiredString(values, "knowledgeState"), "knowledge state"),
            Convert.ToDecimal(values["confidence"], CultureInfo.InvariantCulture),
            RequiredString(values, "source"),
            RequiredStrings(values, "evidenceReferences"),
            ParseTimestamp(RequiredString(values, "observedAt"), "observedAt"));
        return relationship.Validate();
    }

    private static string RequiredString(IReadOnlyDictionary<string, object> values, string key) =>
        values.TryGetValue(key, out var value) && value is not null && !string.IsNullOrWhiteSpace(value.ToString())
            ? value.ToString()!
            : throw new InvalidOperationException($"Enterprise Graph omitted required {key} data.");

    private static ImmutableArray<string> RequiredStrings(
        IReadOnlyDictionary<string, object> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is not IEnumerable<object> items)
            throw new InvalidOperationException($"Enterprise Graph omitted required {key} data.");
        return items.Select(item => item?.ToString()
                ?? throw new InvalidOperationException($"Enterprise Graph returned null {key} data."))
            .ToImmutableArray();
    }

    private static Guid ParseGuid(string value, string field) =>
        Guid.TryParseExact(value, "D", out var parsed) && parsed != Guid.Empty
            ? parsed
            : throw new InvalidOperationException($"Enterprise Graph returned an invalid {field} identifier.");

    private static T ParseEnum<T>(string value, string field) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: false, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new InvalidOperationException($"Enterprise Graph returned an invalid {field}.");

    private static DateTimeOffset ParseTimestamp(string value, string field) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Enterprise Graph returned an invalid {field} timestamp.");
}
