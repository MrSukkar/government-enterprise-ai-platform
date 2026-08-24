namespace Platform.SoftwareFactory.Packages;

public sealed record PackageCoordinate(PackageKind Kind, string Name, string Version, string ContentDigest)
{
    public PackageCoordinate Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(ContentDigest);
        if (!ContentDigest.Contains(':', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Content digest must include its algorithm prefix.");
        }

        return this;
    }
}
