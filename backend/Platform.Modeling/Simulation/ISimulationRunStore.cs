namespace Platform.Modeling.Simulation;

public interface ISimulationRunStore
{
    Task<SimulationRunRecord> CreateAtomicallyAsync(
        SimulationRunRecord started,
        CancellationToken cancellationToken);

    Task<SimulationRunRecord> CompleteAtomicallyAsync(
        SimulationRunRecord completed,
        SimulationRunState expectedState,
        CancellationToken cancellationToken);
}
