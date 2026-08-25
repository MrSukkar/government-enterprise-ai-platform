namespace Platform.EnterpriseModel.Intelligence;

public sealed record IntelligencePolicyReference(
    string PolicyId,
    string Version,
    string Sha256Digest,
    string SignatureReference,
    string Environment)
{
    public IntelligencePolicyReference Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(PolicyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        if (Sha256Digest.Length != 64 || Sha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Intelligence policy requires a SHA-256 digest.");
        return this;
    }
}
