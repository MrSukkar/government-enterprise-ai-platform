using System.Collections.Immutable;

namespace Platform.AgenticWork.Execution;

public sealed record AgenticWorkApproval(
    Guid WorkId,
    string ApprovedBySubjectId,
    ImmutableHashSet<string> Permissions,
    string HumanApprovalReference,
    DateTimeOffset ApprovedAt)
{
    public AgenticWorkApproval Validate()
    {
        if (WorkId == Guid.Empty) throw new InvalidOperationException("Approved work identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(ApprovedBySubjectId);
        ArgumentNullException.ThrowIfNull(Permissions);
        ArgumentException.ThrowIfNullOrWhiteSpace(HumanApprovalReference);
        if (!Permissions.Contains("agentic.work.approve"))
            throw new UnauthorizedAccessException("The agentic.work.approve permission is required.");
        if (ApprovedAt == default) throw new InvalidOperationException("Approval time is required.");
        return this;
    }
}
