namespace Platform.Infrastructure.Productization;

public interface IGovernmentProductRegistry
{
    Task<GovernmentProductPackage> RegisterAtomicallyAsync(
        GovernmentProductPackage package,
        CancellationToken cancellationToken);
}
