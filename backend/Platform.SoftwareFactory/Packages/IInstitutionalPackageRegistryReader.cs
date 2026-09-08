namespace Platform.SoftwareFactory.Packages;

public interface IInstitutionalPackageRegistryReader
{
    Task<InstitutionalPackage?> FindExactAsync(
        PackageCoordinate coordinate,
        string tenantId,
        string environment,
        CancellationToken cancellationToken);
}
