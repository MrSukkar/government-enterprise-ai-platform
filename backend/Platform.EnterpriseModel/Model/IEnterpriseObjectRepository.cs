namespace Platform.EnterpriseModel.Model;

public interface IEnterpriseObjectRepository
{
    Task<EnterpriseObject?> GetAsync(EnterpriseObjectId id, string tenantId, CancellationToken cancellationToken);
    Task SaveAsync(EnterpriseObject enterpriseObject, CancellationToken cancellationToken);
}
