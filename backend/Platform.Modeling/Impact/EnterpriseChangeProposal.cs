using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.Modeling.Impact;

public sealed record EnterpriseChangeProposal(
    EnterpriseObjectId TargetObjectId,
    string ChangeType,
    string CurrentStateReference,
    string ProposedStateReference,
    ImmutableArray<string> EvidenceReferences)
{
    public EnterpriseChangeProposal Validate()
    {
        if (TargetObjectId.Value == Guid.Empty) throw new InvalidOperationException("Change target is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(ChangeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(CurrentStateReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(ProposedStateReference);
        if (EvidenceReferences.IsDefaultOrEmpty) throw new InvalidOperationException("Change proposal requires evidence.");
        foreach (var value in EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return this;
    }
}
