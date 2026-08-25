using System.Collections.Immutable;

namespace Platform.AgenticWork.Execution;

public sealed record AgenticWorkResume(
    Guid WorkId,
    string ResumedBySubjectId,
    ImmutableHashSet<string> Permissions,
    string ReviewEvidenceReference,
    DateTimeOffset ResumedAt)
{
    public AgenticWorkResume Validate()
    {
        if (WorkId == Guid.Empty) throw new InvalidOperationException("Resumed work identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(ResumedBySubjectId);
        ArgumentNullException.ThrowIfNull(Permissions);
        ArgumentException.ThrowIfNullOrWhiteSpace(ReviewEvidenceReference);
        if (!Permissions.Contains("agentic.work.resume"))
            throw new UnauthorizedAccessException("The agentic.work.resume permission is required.");
        if (ResumedAt == default) throw new InvalidOperationException("Resume time is required.");
        return this;
    }
}
