namespace Platform.Modeling.Impact;

public interface IEnterpriseModelSnapshotProvider
{
    Task<EnterpriseModelSnapshot> LoadAuthorizedSnapshotAsync(
        EnterpriseModelingRequest request,
        CancellationToken cancellationToken);
}
