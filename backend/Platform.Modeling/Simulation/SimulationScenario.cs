using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.Modeling.Simulation;

public sealed record SimulationPerturbation(
    EnterpriseObjectId TargetObjectId,
    string ChangeType,
    string BaselineStateReference,
    string SimulatedStateReference,
    ImmutableArray<string> EvidenceReferences)
{
    public void Validate()
    {
        if (TargetObjectId.Value == Guid.Empty) throw new InvalidOperationException("Perturbation target is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(ChangeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(BaselineStateReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SimulatedStateReference);
        if (EvidenceReferences.IsDefaultOrEmpty) throw new InvalidOperationException("Perturbation evidence is required.");
        foreach (var value in EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }
}

public sealed record SimulationScenario(
    Guid ScenarioId,
    string Name,
    ImmutableArray<SimulationPerturbation> Perturbations,
    ImmutableArray<string> Assumptions,
    string RecoveryPlanReference,
    string RecoveryPlanVersion,
    string RecoveryPlanSha256Digest,
    ImmutableArray<string> RecoveryEvidenceReferences)
{
    public SimulationScenario Validate()
    {
        if (ScenarioId == Guid.Empty) throw new InvalidOperationException("Simulation scenario identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(RecoveryPlanReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(RecoveryPlanVersion);
        if (RecoveryPlanSha256Digest.Length != 64 || RecoveryPlanSha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Recovery plan requires a SHA-256 digest.");
        if (Perturbations.IsDefaultOrEmpty || Assumptions.IsDefaultOrEmpty || RecoveryEvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Scenario perturbations, assumptions, and recovery evidence are required.");
        foreach (var perturbation in Perturbations) perturbation.Validate();
        foreach (var value in Assumptions) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in RecoveryEvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return this;
    }
}
