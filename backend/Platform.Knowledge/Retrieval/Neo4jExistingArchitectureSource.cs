using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;
using Platform.Integrations.ExistingArchitecture;

namespace Platform.Knowledge.Retrieval;

public sealed class Neo4jExistingArchitectureSource(
    IDriver driver, IOptions<Neo4jEnterpriseGraphOptions> configuredOptions) : IExistingArchitectureSource
{
    private const string ScopedArchitectureCypher = """
        UNWIND $systemIds AS authorizedSystemId
        MATCH (item:EnterpriseArchitecture {tenantId: $tenantId, systemId: authorizedSystemId})
        WHERE item.classificationRank <= $maximumClassificationRank
          AND item.environment = $environment
          AND item.sourceKind = $sourceKind
          AND item.approvalState = $approvedState
          AND item.lifecycle = $activeLifecycle
          AND item.kind IN $itemKinds
          AND (item.relatedSystemId IS NULL OR item.relatedSystemId IN $systemIds)
          AND (item.relationshipType IS NULL OR item.relationshipType IN $relationshipTypes)
        RETURN item.architectureItemId AS architectureItemId, item.systemId AS systemId,
               item.relatedSystemId AS relatedSystemId, item.kind AS kind, item.name AS name,
               item.description AS description, item.relationshipType AS relationshipType,
               item.approvalState AS approvalState, item.version AS version,
               item.classification AS classification, item.environment AS environment,
               item.lifecycle AS lifecycle, item.sourceKind AS sourceKind,
               item.evidenceReferences AS evidenceReferences,
               toString(item.approvedAt) AS approvedAt, toString(item.updatedAt) AS updatedAt,
               coalesce(item.credentialsIncluded, false) AS credentialsIncluded,
               coalesce(item.liveSessionIncluded, false) AS liveSessionIncluded,
               coalesce(item.executableCommandIncluded, false) AS executableCommandIncluded,
               coalesce(item.generatedContentIncluded, false) AS generatedContentIncluded,
               coalesce(item.externalEffectOccurred, false) AS externalEffectOccurred
        ORDER BY item.architectureItemId
        LIMIT $maximumResults
        """;

    public string SourceKind => "enterprise-graph";

    public async Task<IReadOnlyCollection<ExistingArchitectureCandidate>> DiscoverAsync(
        ExistingArchitectureSourceScope scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new KnowledgeRetrievalSourceUnavailableException("The sovereign Enterprise Graph is not validly configured.");
        if (scope.AllowedSystemIds.IsEmpty || scope.AllowedItemKinds.IsEmpty ||
            scope.AllowedRelationshipTypes.IsEmpty || scope.AllowedSourceKinds.Count != 1 ||
            !scope.AllowedSourceKinds.Contains(SourceKind) || scope.MaximumResults <= 0 ||
            scope.MaximumResults > options.MaximumRecords || string.IsNullOrWhiteSpace(scope.TenantId) ||
            string.IsNullOrWhiteSpace(scope.Purpose) || string.IsNullOrWhiteSpace(scope.Environment))
            throw new UnauthorizedAccessException("Existing Architecture Graph scope is empty, unavailable, or excessive.");
        try
        {
            await using var session = driver.AsyncSession(configuration => configuration
                .WithDatabase(options.Database).WithDefaultAccessMode(AccessMode.Read));
            var records = await session.ExecuteReadAsync(async transaction =>
            {
                var cursor = await transaction.RunAsync(ScopedArchitectureCypher, new
                {
                    tenantId = scope.TenantId,
                    systemIds = scope.AllowedSystemIds.Select(id => id.Value.ToString("D")).Order(StringComparer.Ordinal).ToArray(),
                    maximumClassificationRank = (int)scope.MaximumClassification,
                    environment = scope.Environment, sourceKind = SourceKind,
                    approvedState = ExistingArchitectureApprovalState.Approved.ToString(),
                    activeLifecycle = LifecycleState.Active.ToString(),
                    itemKinds = scope.AllowedItemKinds.Select(value => value.ToString()).Order(StringComparer.Ordinal).ToArray(),
                    relationshipTypes = scope.AllowedRelationshipTypes.Order(StringComparer.Ordinal).ToArray(),
                    maximumResults = scope.MaximumResults
                });
                return await cursor.ToListAsync(MapRecord);
            }, configuration => configuration.WithTimeout(TimeSpan.FromSeconds(options.QueryTimeoutSeconds)));
            cancellationToken.ThrowIfCancellationRequested();
            if (records.Count > scope.MaximumResults)
                throw new InvalidOperationException("Enterprise Graph exceeded the authorized Existing Architecture result bound.");
            return records;
        }
        catch (Neo4jException exception)
        {
            throw new KnowledgeRetrievalSourceUnavailableException("The sovereign Enterprise Graph Existing Architecture source is unavailable.", exception);
        }
        catch (TimeoutException exception)
        {
            throw new KnowledgeRetrievalSourceUnavailableException("The sovereign Enterprise Graph Existing Architecture query timed out.", exception);
        }
    }

    private static ExistingArchitectureCandidate MapRecord(IRecord record) => new(
        record["sourceKind"].As<string>(), ParseGuid(record["architectureItemId"].As<string>()),
        new EnterpriseObjectId(ParseGuid(record["systemId"].As<string>())),
        record["relatedSystemId"] is null ? null : new EnterpriseObjectId(ParseGuid(record["relatedSystemId"].As<string>())),
        ParseEnum<ExistingArchitectureItemKind>(record["kind"].As<string>()), record["name"].As<string>(),
        record["description"].As<string>(), record["relationshipType"] is null ? null : record["relationshipType"].As<string>(),
        ParseEnum<ExistingArchitectureApprovalState>(record["approvalState"].As<string>()), record["version"].As<string>(),
        ParseEnum<DataClassification>(record["classification"].As<string>()), record["environment"].As<string>(),
        ParseEnum<LifecycleState>(record["lifecycle"].As<string>()),
        record["evidenceReferences"].As<List<string>>().ToImmutableArray(),
        ParseTimestamp(record["approvedAt"].As<string>()), ParseTimestamp(record["updatedAt"].As<string>()),
        record["credentialsIncluded"].As<bool>(), record["liveSessionIncluded"].As<bool>(),
        record["executableCommandIncluded"].As<bool>(), record["generatedContentIncluded"].As<bool>(),
        record["externalEffectOccurred"].As<bool>());

    private static Guid ParseGuid(string value) => Guid.TryParseExact(value, "D", out var parsed) && parsed != Guid.Empty
        ? parsed : throw new InvalidOperationException("Enterprise Graph returned an invalid architecture identity.");
    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.TryParse<T>(value, false, out var parsed) && Enum.IsDefined(parsed)
            ? parsed : throw new InvalidOperationException("Enterprise Graph returned invalid architecture enumeration data.");
    private static DateTimeOffset ParseTimestamp(string value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed : throw new InvalidOperationException("Enterprise Graph returned an invalid architecture timestamp.");
}
