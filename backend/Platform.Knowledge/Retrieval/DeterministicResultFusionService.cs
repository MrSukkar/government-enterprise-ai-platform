namespace Platform.Knowledge.Retrieval;

public sealed class DeterministicResultFusionService : IResultFusionService
{
    public IReadOnlyList<KnowledgeCandidate> FuseAndRerank(
        IEnumerable<KnowledgeCandidate> candidates,
        int maximumResults)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (maximumResults <= 0) throw new ArgumentOutOfRangeException(nameof(maximumResults));

        return candidates
            .GroupBy(candidate => (candidate.ResourceId, candidate.Content), StringTupleComparer.Instance)
            .Select(group => group
                .OrderByDescending(candidate => candidate.Relevance)
                .ThenBy(candidate => candidate.Source, StringComparer.Ordinal)
                .First())
            .OrderByDescending(candidate => candidate.Relevance)
            .ThenBy(candidate => candidate.ResourceId, StringComparer.Ordinal)
            .Take(maximumResults)
            .ToArray();
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string ResourceId, string Content)>
    {
        internal static StringTupleComparer Instance { get; } = new();

        public bool Equals((string ResourceId, string Content) x, (string ResourceId, string Content) y) =>
            StringComparer.Ordinal.Equals(x.ResourceId, y.ResourceId) &&
            StringComparer.Ordinal.Equals(x.Content, y.Content);

        public int GetHashCode((string ResourceId, string Content) value) =>
            HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(value.ResourceId),
                StringComparer.Ordinal.GetHashCode(value.Content));
    }
}
