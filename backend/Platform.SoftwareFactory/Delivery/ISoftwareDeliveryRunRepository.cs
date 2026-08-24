namespace Platform.SoftwareFactory.Delivery;

public interface ISoftwareDeliveryRunRepository
{
    Task<SoftwareDeliveryRun?> GetAsync(Guid id, string tenantId, CancellationToken cancellationToken);
    Task SaveAsync(SoftwareDeliveryRun run, CancellationToken cancellationToken);
}
