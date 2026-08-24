namespace Platform.SoftwareFactory.Packages;

public sealed record PackageProvenance(
    string Source,
    string Publisher,
    string ProvenanceReference,
    DateTimeOffset ObservedAt)
{
    public PackageProvenance Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);
        ArgumentException.ThrowIfNullOrWhiteSpace(Publisher);
        ArgumentException.ThrowIfNullOrWhiteSpace(ProvenanceReference);
        return this;
    }
}
