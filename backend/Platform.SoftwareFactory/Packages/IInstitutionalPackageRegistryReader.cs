namespace Platform.SoftwareFactory.Packages;

public interface IInstitutionalPackageRegistryReader
{
    Task<InstitutionalPackage?> FindExactAsync(
        PackageCoordinate coordinate,
        CancellationToken cancellationToken);
}
