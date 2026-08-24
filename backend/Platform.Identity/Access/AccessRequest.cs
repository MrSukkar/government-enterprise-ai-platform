using System.Collections.Immutable;

namespace Platform.Identity.Access;

public sealed record AccessRequest(
    GovernedIdentity Identity,
    string Purpose,
    string Action,
    string ResourceId,
    string ResourceTenantId,
    DataClassification ResourceClassification,
    ImmutableHashSet<string> RequiredRoles,
    string? InitiatorSubjectId,
    bool RequiresDistinctApprover);
