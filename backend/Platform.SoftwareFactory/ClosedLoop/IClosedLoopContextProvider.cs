namespace Platform.SoftwareFactory.ClosedLoop;

public interface IClosedLoopContextProvider
{
    Task<ClosedLoopContext> LoadAuthorizedContextAsync(
        ClosedLoopEvaluationRequest request,
        CancellationToken cancellationToken);
}
