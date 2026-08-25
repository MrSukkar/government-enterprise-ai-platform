namespace Platform.Modeling.Simulation;

public interface IScenarioSimulationRuntime
{
    Task<SimulationResult> RunAsync(SimulationExecutionContext context, CancellationToken cancellationToken);
}
