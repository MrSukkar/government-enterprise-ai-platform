using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Intelligence;

public sealed record EnterpriseOperationalSignal(
    Guid SignalId,
    EnterpriseObjectId ObjectId,
    string SignalType,
    string ObservedValueReference,
    string TraceId,
    DataClassification Classification,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset ObservedAt)
{
    public void Validate()
    {
        if (SignalId == Guid.Empty || ObjectId.Value == Guid.Empty)
            throw new InvalidOperationException("Signal and object identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(SignalType);
        ArgumentException.ThrowIfNullOrWhiteSpace(ObservedValueReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(TraceId);
        if (EvidenceReferences.IsDefaultOrEmpty || ObservedAt == default)
            throw new InvalidOperationException("Operational signal requires evidence and time.");
        foreach (var value in EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }
}
