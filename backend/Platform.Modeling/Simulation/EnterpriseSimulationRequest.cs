using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.Modeling.Simulation;

public sealed record EnterpriseSimulationRequest(
    Guid RequestId,
    string TenantId,
    string SubjectId,
    ImmutableHashSet<string> Permissions,
    string Purpose,
    ImmutableHashSet<EnterpriseObjectId> AuthorizedObjectScope,
    DataClassification MaximumClassification,
    SimulationScenario Scenario,
    DateTimeOffset RequestedAt)
{
    public EnterpriseSimulationRequest Validate()
    {
        if (RequestId == Guid.Empty) throw new InvalidOperationException("Simulation request identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentNullException.ThrowIfNull(Permissions);
        ArgumentNullException.ThrowIfNull(AuthorizedObjectScope);
        ArgumentNullException.ThrowIfNull(Scenario);
        if (!Permissions.Contains("enterprise.simulation.run"))
            throw new UnauthorizedAccessException("The enterprise.simulation.run permission is required.");
        if (AuthorizedObjectScope.IsEmpty) throw new UnauthorizedAccessException("Simulation requires explicit authorized scope.");
        Scenario.Validate();
        if (Scenario.Perturbations.Any(item => !AuthorizedObjectScope.Contains(item.TargetObjectId)))
            throw new UnauthorizedAccessException("Every perturbation target must be inside authorized scope.");
        if (RequestedAt == default) throw new InvalidOperationException("Simulation request time is required.");
        return this;
    }
}
