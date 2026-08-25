namespace Platform.EnterpriseModel.Understanding;

public interface IUnderstandingContextProvider
{
    Task<UnderstandingSnapshot> LoadAuthorizedSnapshotAsync(
        UnderstandingRequest request,
        CancellationToken cancellationToken);
}
