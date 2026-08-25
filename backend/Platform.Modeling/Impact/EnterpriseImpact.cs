using System.Collections.Immutable;
using Platform.Domain.Security;
using Platform.EnterpriseModel.Model;

namespace Platform.Modeling.Impact;

public enum ImpactBasis
{
    DirectTarget = 0,
    ConfirmedRelationship = 1,
    DiscoveredRelationship = 2,
    InferredRelationship = 3,
    UnknownRelationship = 4
}

public sealed record ImpactPathEdge(
    EnterpriseObjectId SourceId,
    EnterpriseObjectId TargetId,
    string RelationshipType,
    RelationshipKnowledgeState KnowledgeState,
    decimal Confidence,
    ImmutableArray<string> EvidenceReferences);

public sealed record EnterpriseImpact(
    EnterpriseObjectId ObjectId,
    string ObjectType,
    string OwnerId,
    DataClassification Classification,
    int DistanceFromChange,
    ImpactBasis Basis,
    ImmutableArray<ImpactPathEdge> Path,
    ImmutableArray<string> EvidenceReferences);
