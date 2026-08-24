namespace Platform.SoftwareFactory.SupplyChain;

public sealed record ArtifactSupplyChainRecord(
    string ArtifactName,
    string ContentDigest,
    string SourceRepository,
    string SourceCommit,
    string SbomReference,
    string BuildAttestationReference,
    string SignatureReference,
    string RegistryReference)
{
    public ArtifactSupplyChainRecord Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ArtifactName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ContentDigest);
        ArgumentException.ThrowIfNullOrWhiteSpace(SourceRepository);
        ArgumentException.ThrowIfNullOrWhiteSpace(SourceCommit);
        ArgumentException.ThrowIfNullOrWhiteSpace(SbomReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(BuildAttestationReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SignatureReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(RegistryReference);
        if (!ContentDigest.StartsWith("sha256:", StringComparison.Ordinal))
            throw new InvalidOperationException("Artifact digest must be SHA-256 qualified.");
        return this;
    }
}
