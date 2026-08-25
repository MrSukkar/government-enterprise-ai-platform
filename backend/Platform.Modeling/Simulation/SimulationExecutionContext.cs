namespace Platform.Modeling.Simulation;

public sealed record SimulationExecutionContext(
    EnterpriseSimulationRequest Request,
    DigitalTwinSnapshot DigitalTwin,
    SimulationIsolationProfile Isolation,
    string IdempotencyKey,
    DateTimeOffset StartedAt);
