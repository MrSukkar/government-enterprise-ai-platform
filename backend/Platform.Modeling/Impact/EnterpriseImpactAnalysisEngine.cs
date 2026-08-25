using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.Modeling.Impact;

public sealed class EnterpriseImpactAnalysisEngine(
    IEnterpriseModelSnapshotProvider snapshotProvider,
    TimeProvider timeProvider)
{
    public async Task<EnterpriseImpactAnalysisReport> AnalyzeAsync(
        EnterpriseModelingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var snapshot = await snapshotProvider.LoadAuthorizedSnapshotAsync(request, cancellationToken);
        var generatedAt = timeProvider.GetUtcNow();
        ValidateSnapshot(request, snapshot, generatedAt);

        var objectMap = snapshot.Objects.ToDictionary(item => item.Id);
        if (!objectMap.ContainsKey(request.Change.TargetObjectId))
            throw new UnauthorizedAccessException("Authorized snapshot omitted the requested change target.");

        var adjacency = objectMap.Keys.ToDictionary(
            key => key,
            _ => new List<(EnterpriseObjectId Neighbor, ImpactPathEdge Edge)>());
        var excludedRelationshipCount = 0;
        foreach (var source in snapshot.Objects)
        {
            foreach (var relationship in source.Relationships)
            {
                if (!objectMap.ContainsKey(relationship.TargetId))
                {
                    excludedRelationshipCount++;
                    continue;
                }

                var edge = new ImpactPathEdge(
                    source.Id, relationship.TargetId, relationship.RelationshipType,
                    relationship.KnowledgeState, relationship.Confidence, relationship.EvidenceReferences);
                adjacency[source.Id].Add((relationship.TargetId, edge));
                adjacency[relationship.TargetId].Add((source.Id, edge));
            }
        }

        var paths = new Dictionary<EnterpriseObjectId, ImmutableArray<ImpactPathEdge>>
        {
            [request.Change.TargetObjectId] = []
        };
        var queue = new Queue<EnterpriseObjectId>();
        queue.Enqueue(request.Change.TargetObjectId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentPath = paths[current];
            if (currentPath.Length >= request.MaximumTraversalDepth) continue;
            foreach (var connection in adjacency[current]
                         .OrderBy(item => item.Neighbor.Value)
                         .ThenBy(item => item.Edge.RelationshipType, StringComparer.Ordinal))
            {
                if (paths.ContainsKey(connection.Neighbor)) continue;
                paths[connection.Neighbor] = currentPath.Add(connection.Edge);
                queue.Enqueue(connection.Neighbor);
            }
        }

        var impacts = paths
            .Select(item => CreateImpact(objectMap[item.Key], item.Value))
            .OrderBy(item => item.DistanceFromChange)
            .ThenBy(item => item.ObjectId.Value)
            .ToImmutableArray();
        var limitations = ImmutableArray.CreateBuilder<string>();
        limitations.Add("Impact analysis reports authorized structural reachability; it does not simulate outcomes.");
        if (excludedRelationshipCount > 0)
            limitations.Add($"{excludedRelationshipCount} relationship(s) ended outside the authorized snapshot and were not traversed.");
        if (paths.Values.Any(path => path.Length == request.MaximumTraversalDepth))
            limitations.Add("Traversal stopped at the request's explicit maximum depth.");

        var evidence = snapshot.AuthorizationEvidenceReferences
            .AddRange(request.Change.EvidenceReferences)
            .AddRange(impacts.SelectMany(item => item.EvidenceReferences))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        return new EnterpriseImpactAnalysisReport(
            request.RequestId, request.TenantId, request.Change, impacts,
            limitations.ToImmutable(), evidence, snapshot.CapturedAt, generatedAt);
    }

    private static void ValidateSnapshot(
        EnterpriseModelingRequest request,
        EnterpriseModelSnapshot snapshot,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.RequestId != request.RequestId ||
            !StringComparer.Ordinal.Equals(snapshot.TenantId, request.TenantId))
            throw new UnauthorizedAccessException("Enterprise model snapshot does not match the authorized request.");
        if (snapshot.Objects.IsDefaultOrEmpty || snapshot.AuthorizationEvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Authorized model snapshot requires objects and authorization evidence.");
        if (snapshot.CapturedAt < request.RequestedAt || snapshot.CapturedAt > generatedAt)
            throw new InvalidOperationException("Enterprise model snapshot time is invalid.");
        if (snapshot.Objects.Select(item => item.Id).Distinct().Count() != snapshot.Objects.Length)
            throw new InvalidOperationException("Enterprise model snapshot contains duplicate objects.");
        foreach (var enterpriseObject in snapshot.Objects)
        {
            enterpriseObject.Validate();
            if (!StringComparer.Ordinal.Equals(enterpriseObject.TenantId, request.TenantId) ||
                !request.AuthorizedObjectScope.Contains(enterpriseObject.Id) ||
                enterpriseObject.Classification > request.MaximumClassification)
                throw new UnauthorizedAccessException("Enterprise model provider exceeded authorized scope.");
        }
        foreach (var value in snapshot.AuthorizationEvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }

    private static EnterpriseImpact CreateImpact(
        EnterpriseObject enterpriseObject,
        ImmutableArray<ImpactPathEdge> path)
    {
        var basis = path.IsEmpty
            ? ImpactBasis.DirectTarget
            : path.Min(edge => edge.KnowledgeState) switch
            {
                RelationshipKnowledgeState.Confirmed => ImpactBasis.ConfirmedRelationship,
                RelationshipKnowledgeState.Discovered => ImpactBasis.DiscoveredRelationship,
                RelationshipKnowledgeState.Inferred => ImpactBasis.InferredRelationship,
                _ => ImpactBasis.UnknownRelationship
            };
        var evidence = enterpriseObject.EvidenceReferences
            .AddRange(path.SelectMany(edge => edge.EvidenceReferences))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        return new EnterpriseImpact(
            enterpriseObject.Id, enterpriseObject.Type, enterpriseObject.OwnerId,
            enterpriseObject.Classification, path.Length, basis, path, evidence);
    }
}
