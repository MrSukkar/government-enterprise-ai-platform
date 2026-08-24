using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.Knowledge.Retrieval;

public sealed record KnowledgeCandidate(
    string ResourceId,
    string TenantId,
    DataClassification Classification,
    string Content,
    RetrievalModality Modality,
    decimal Relevance,
    string Source,
    ImmutableArray<string> EvidenceReferences);
