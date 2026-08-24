using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.EnterpriseModel.Model;

public sealed record EnterpriseObject
{
    public required EnterpriseObjectId Id { get; init; }
    public required string TenantId { get; init; }
    public required string Type { get; init; }
    public required string State { get; init; }
    public required string OwnerId { get; init; }
    public required DataClassification Classification { get; init; }
    public required ImmutableArray<EnterpriseRelationship> Relationships { get; init; }
    public required ImmutableArray<string> PolicyReferences { get; init; }
    public required ImmutableArray<string> PermittedActions { get; init; }
    public required string Source { get; init; }
    public required decimal Confidence { get; init; }
    public required ImmutableArray<string> EvidenceReferences { get; init; }
    public required LifecycleState Lifecycle { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public EnterpriseObject Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Type);
        ArgumentException.ThrowIfNullOrWhiteSpace(State);
        ArgumentException.ThrowIfNullOrWhiteSpace(OwnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);
        if (Confidence is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(Confidence));
        if (UpdatedAt < CreatedAt) throw new InvalidOperationException("UpdatedAt cannot precede CreatedAt.");
        foreach (var relationship in Relationships) relationship.Validate();
        return this;
    }
}
