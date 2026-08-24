using System.Collections.Immutable;

namespace Platform.Knowledge.Retrieval;

public sealed record AuthorizedKnowledgeContext(
    string Purpose,
    string TenantId,
    ImmutableArray<KnowledgeCandidate> Candidates)
{
    public bool IsEmpty => Candidates.IsDefaultOrEmpty;
}
