namespace Platform.SoftwareFactory.Packages;

public interface IInstitutionalPackageRegistry
{
    Task<InstitutionalPackage?> FindExactAsync(PackageCoordinate coordinate, CancellationToken cancellationToken);
    Task SaveAsync(InstitutionalPackage package, CancellationToken cancellationToken);
}
