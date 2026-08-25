namespace Platform.Modeling.Simulation;

public interface IDigitalTwinSnapshotProvider
{
    Task<DigitalTwinSnapshot> LoadAuthorizedIsolatedSnapshotAsync(
        EnterpriseSimulationRequest request,
        CancellationToken cancellationToken);
}
