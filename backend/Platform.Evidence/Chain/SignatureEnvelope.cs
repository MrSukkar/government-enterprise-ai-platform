namespace Platform.Evidence.Chain;

public sealed record SignatureEnvelope(
    string Algorithm,
    string KeyId,
    string SignatureBase64,
    string CertificateChainReference,
    DateTimeOffset SignedAt)
{
    public SignatureEnvelope Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Algorithm);
        ArgumentException.ThrowIfNullOrWhiteSpace(KeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureBase64);
        ArgumentException.ThrowIfNullOrWhiteSpace(CertificateChainReference);
        try { _ = Convert.FromBase64String(SignatureBase64); }
        catch (FormatException exception) { throw new InvalidOperationException("Evidence signature is not valid Base64.", exception); }
        if (SignedAt == default) throw new InvalidOperationException("Evidence signature time is required.");
        return this;
    }
}

public interface IEvidenceSigner
{
    Task<SignatureEnvelope> SignAsync(
        string tenantId,
        string sha256Digest,
        CancellationToken cancellationToken);
}

public interface IEvidenceSignatureVerifier
{
    Task<bool> VerifyAsync(
        string tenantId,
        string sha256Digest,
        SignatureEnvelope signature,
        CancellationToken cancellationToken);
}
