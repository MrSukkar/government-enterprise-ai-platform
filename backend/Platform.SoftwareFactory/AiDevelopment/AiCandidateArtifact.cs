using System.Collections.Immutable;

namespace Platform.SoftwareFactory.AiDevelopment;

public sealed record AiCandidateArtifact(
    Guid InvocationId,
    string RuntimeProfile,
    string Content,
    ImmutableArray<string> ContextReferences,
    ImmutableArray<string> GeneratedFilePaths,
    DateTimeOffset CreatedAt,
    ImmutableArray<string> EvidenceReferences = default)
{
    public AiCandidateArtifact Validate()
    {
        if (InvocationId == Guid.Empty) throw new InvalidOperationException("Invocation identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(RuntimeProfile);
        ArgumentException.ThrowIfNullOrWhiteSpace(Content);
        if (CreatedAt == default || EvidenceReferences.IsDefaultOrEmpty || EvidenceReferences.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("AI candidate requires time and cryptographic evidence.");
        return this;
    }
}
