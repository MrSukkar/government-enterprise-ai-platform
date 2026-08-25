namespace Platform.SoftwareFactory.ClosedLoop;

public sealed record ClosedLoopPolicyReference(
    string PolicyId,
    string Version,
    string Sha256Digest,
    string SignatureReference)
{
    public ClosedLoopPolicyReference Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(PolicyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureReference);
        if (Sha256Digest.Length != 64 || Sha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Closed-loop policy requires a SHA-256 digest.");
        return this;
    }
}
