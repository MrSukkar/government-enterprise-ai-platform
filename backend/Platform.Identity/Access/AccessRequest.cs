using System.Collections.Immutable;
using Platform.Domain.Security;

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
