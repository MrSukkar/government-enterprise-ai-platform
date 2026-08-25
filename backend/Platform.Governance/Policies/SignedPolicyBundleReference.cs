namespace Platform.Governance.Policies;

public sealed record SignedPolicyBundleReference(
    string BundleId,
    string Version,
    string Sha256Digest,
    string SignatureReference,
    string Environment,
    DateTimeOffset ActivatedAt)
{
    public SignedPolicyBundleReference Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(BundleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        if (Sha256Digest.Length != 64 || Sha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Policy bundle requires a SHA-256 digest.");
        if (ActivatedAt == default) throw new InvalidOperationException("Policy activation time is required.");
        return this;
    }
}
