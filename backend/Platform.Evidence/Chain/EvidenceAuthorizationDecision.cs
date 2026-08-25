namespace Platform.Evidence.Chain;

public sealed record EvidenceAuthorizationDecision(bool Authorized, string EvidenceReference)
{
    public void Demand()
    {
        if (!Authorized) throw new UnauthorizedAccessException("Evidence access was denied.");
        ArgumentException.ThrowIfNullOrWhiteSpace(EvidenceReference);
    }
}

public interface IEvidenceAccessAuthorizer
{
    Task<EvidenceAuthorizationDecision> AuthorizeAppendAsync(
        EvidenceAppendRequest request,
        CancellationToken cancellationToken);

    Task<EvidenceAuthorizationDecision> AuthorizeVerificationAsync(
        EvidenceVerificationRequest request,
        CancellationToken cancellationToken);

    Task<EvidenceAuthorizationDecision> AuthorizeClassificationAsync(
        EvidenceVerificationRequest request,
        string entryClassification,
        CancellationToken cancellationToken);
}
