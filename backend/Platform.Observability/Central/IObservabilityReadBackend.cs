namespace Platform.Observability.Central;

public interface IObservabilityReadBackend
{
    Task<IReadOnlyList<ObservabilityRecord>> QueryAsync(
        CentralObservabilityQuery query,
        CancellationToken cancellationToken);
}
