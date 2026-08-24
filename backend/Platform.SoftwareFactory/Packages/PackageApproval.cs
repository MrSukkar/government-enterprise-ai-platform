namespace Platform.SoftwareFactory.Packages;

public sealed record PackageApproval(
    PackageApprovalStatus Status,
    string ReviewerSubjectId,
    string Rationale,
    DateTimeOffset DecidedAt,
    DateTimeOffset? ExpiresAt,
    string? EvidenceReference)
{
    public PackageApproval Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ReviewerSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Rationale);
        if (ExpiresAt <= DecidedAt) throw new InvalidOperationException("Approval expiry must follow its decision time.");
        return this;
    }
}
