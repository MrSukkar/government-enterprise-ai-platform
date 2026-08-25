namespace Platform.Modeling.Simulation;

public enum SimulationNetworkAccess { None = 0 }

public sealed record SimulationIsolationProfile
{
    public bool HasProductionCredentials => false;
    public bool AllowsExternalEffects => false;
    public SimulationNetworkAccess NetworkAccess => SimulationNetworkAccess.None;
}
