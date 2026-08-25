namespace Platform.EnterpriseModel.Intelligence;

public interface IProactiveIntelligenceContextProvider
{
    Task<ProactiveIntelligenceSnapshot> LoadAuthorizedContextAsync(
        ProactiveIntelligenceRequest request,
        CancellationToken cancellationToken);
}
