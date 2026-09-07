using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Platform.Domain.Security;

namespace Platform.Knowledge.Retrieval;

public sealed class Neo4jEnterpriseGraphRetrievalSource(
    IDriver driver,
    IOptions<Neo4jEnterpriseGraphOptions> configuredOptions) : IKnowledgeRetrievalSource
{
    private const string ScopedReadCypher = """
        UNWIND $resourceIds AS authorizedResourceId
        MATCH (resource:EnterpriseObject {tenantId: $tenantId, resourceId: authorizedResourceId})
        WHERE resource.classificationRank <= $maximumClassificationRank
        RETURN resource.resourceId AS resourceId,
               resource.tenantId AS tenantId,
               resource.classification AS classification,
               resource.contextContent AS content,
               resource.contextRelevance AS relevance,
               resource.source AS source,
               resource.evidenceReferences AS evidenceReferences
        ORDER BY resource.resourceId, resource.source
        LIMIT $maximumResults
        """;

    public RetrievalModality Modality => RetrievalModality.Graph;

    public async Task<IReadOnlyCollection<KnowledgeCandidate>> RetrieveAsync(
        string queryText,
        AuthorizedRetrievalScope scope,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryText);
        ArgumentNullException.ThrowIfNull(scope);
        var options = configuredOptions.Value;
        if (!options.IsOperationallyConfigured)
            throw new KnowledgeRetrievalSourceUnavailableException(
                "The sovereign Enterprise Graph is not validly configured.");
        if (scope.AllowedResourceIds.IsEmpty ||
            !scope.Modalities.Contains(RetrievalModality.Graph) ||
            scope.MaximumResults <= 0 || scope.MaximumResults > options.MaximumRecords)
            throw new UnauthorizedAccessException("Enterprise Graph scope is empty, unavailable, or excessive.");
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var session = driver.AsyncSession(configuration => configuration
                .WithDatabase(options.Database)
                .WithDefaultAccessMode(AccessMode.Read));
            var records = await session.ExecuteReadAsync(async transaction =>
            {
                var cursor = await transaction.RunAsync(ScopedReadCypher, new
                {
                    tenantId = scope.TenantId,
                    resourceIds = scope.AllowedResourceIds.Order(StringComparer.Ordinal).ToArray(),
                    maximumClassificationRank = (int)scope.MaximumClassification,
                    maximumResults = scope.MaximumResults
                });
                return await cursor.ToListAsync(MapRecord);
            }, configuration => configuration.WithTimeout(TimeSpan.FromSeconds(options.QueryTimeoutSeconds)));
            cancellationToken.ThrowIfCancellationRequested();
            if (records.Count > scope.MaximumResults)
                throw new InvalidOperationException("Enterprise Graph exceeded the authorized result bound.");
            return records;
        }
        catch (Neo4jException exception)
        {
            throw new KnowledgeRetrievalSourceUnavailableException(
                "The sovereign Enterprise Graph is unavailable.", exception);
        }
        catch (TimeoutException exception)
        {
            throw new KnowledgeRetrievalSourceUnavailableException(
                "The sovereign Enterprise Graph query timed out.", exception);
        }
    }

    private static KnowledgeCandidate MapRecord(IRecord record)
    {
        var classificationText = record["classification"].As<string>();
        if (!Enum.TryParse<DataClassification>(classificationText, ignoreCase: false,
                out var classification) || !Enum.IsDefined(classification))
            throw new InvalidOperationException("Enterprise Graph returned an invalid classification.");
        var evidence = record["evidenceReferences"].As<List<string>>().ToImmutableArray();
        return new KnowledgeCandidate(
            record["resourceId"].As<string>(),
            record["tenantId"].As<string>(),
            classification,
            record["content"].As<string>(),
            RetrievalModality.Graph,
            Convert.ToDecimal(record["relevance"], CultureInfo.InvariantCulture),
            record["source"].As<string>(),
            evidence);
    }
}

public sealed class KnowledgeRetrievalSourceUnavailableException : Exception
{
    public KnowledgeRetrievalSourceUnavailableException(string message) : base(message) { }
    public KnowledgeRetrievalSourceUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}
