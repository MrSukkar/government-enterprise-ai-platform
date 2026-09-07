using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.EnterpriseModel.Model;
using Platform.Integrations.ExistingArchitecture;

namespace Platform.SoftwareFactory.InternalService;

public sealed class DeterministicExistingArchitectureConformanceValidator : IExistingArchitectureConformanceValidator
{
    private const string Authority = "docs/PROJECT_MASTER_SPECIFICATION_V2.md";
    private static readonly ImmutableDictionary<string, string> ApprovedBaselines =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Backend"] = ".NET 10 / ASP.NET Core",
            ["Architecture"] = "Modular Monolith",
            ["Frontend"] = "Blazor WebAssembly",
            ["API"] = "REST + OpenAPI 3.1",
            ["Primary Database"] = "PostgreSQL",
            ["Enterprise Graph"] = "Neo4j",
            ["Policy"] = "OPA",
            ["Telemetry"] = "OpenTelemetry",
            ["Identity"] = "OIDC/OAuth2 + RBAC/ABAC",
            ["Evidence"] = "Cryptographic / tamper-evident",
            ["Deployment"] = "Air-gapped / on-premises ready"
        }.ToImmutableDictionary(StringComparer.Ordinal);

    public Task<ExistingArchitectureConformanceDecision> ValidateScopeAsync(
        ExistingArchitectureConformanceScopeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var conformant = request.DiscoveryId != Guid.Empty &&
            !string.IsNullOrWhiteSpace(request.TenantId) &&
            StringComparer.Ordinal.Equals(request.ArchitectureAuthorityReference, Authority) &&
            !request.AllowedSystemIds.IsEmpty && !request.AllowedItemKinds.IsEmpty &&
            !request.AllowedRelationshipTypes.IsEmpty && !request.EvidenceReferences.IsDefaultOrEmpty &&
            request.RequestedAt != default && request.AllowedSystemIds.All(id => id.Value != Guid.Empty) &&
            request.AllowedItemKinds.All(Enum.IsDefined) &&
            request.AllowedRelationshipTypes.All(value => !string.IsNullOrWhiteSpace(value));
        return Task.FromResult(Decision(request.DiscoveryId, null, request.TenantId,
            conformant, conformant ? "master_spec_scope_conformant" : "master_spec_scope_denied",
            request.EvidenceReferences, request.RequestedAt));
    }

    public Task<ExistingArchitectureConformanceDecision> ValidateItemAsync(
        ExistingArchitectureItemConformanceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Candidate);
        cancellationToken.ThrowIfCancellationRequested();
        var item = request.Candidate;
        var conformant = request.DiscoveryId != Guid.Empty && !string.IsNullOrWhiteSpace(request.TenantId) &&
            StringComparer.Ordinal.Equals(request.ArchitectureAuthorityReference, Authority) &&
            item.ArchitectureItemId != Guid.Empty && item.SystemId.Value != Guid.Empty &&
            item.ApprovalState == ExistingArchitectureApprovalState.Approved &&
            item.Lifecycle == LifecycleState.Active && !string.IsNullOrWhiteSpace(item.Version) &&
            !string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.Description) &&
            !item.EvidenceReferences.IsDefaultOrEmpty && item.ApprovedAt != default &&
            item.UpdatedAt >= item.ApprovedAt && !item.CredentialsIncluded && !item.LiveSessionIncluded &&
            !item.ExecutableCommandIncluded && !item.GeneratedContentIncluded && !item.ExternalEffectOccurred &&
            HasConformantSemantics(item);
        return Task.FromResult(Decision(request.DiscoveryId, item.ArchitectureItemId, request.TenantId,
            conformant, conformant ? "master_spec_item_conformant" : "master_spec_item_denied",
            request.EvidenceReferences.Concat(item.EvidenceReferences).ToImmutableArray(), request.RequestedAt));
    }

    private static bool HasConformantSemantics(ExistingArchitectureCandidate item)
    {
        if (item.Kind == ExistingArchitectureItemKind.TechnologyBaselineReference)
            return ApprovedBaselines.TryGetValue(item.Name, out var baseline) &&
                StringComparer.Ordinal.Equals(baseline, item.Description);
        var text = $"{item.Name} {item.Description}";
        return !text.Contains("AI -> Production", StringComparison.OrdinalIgnoreCase) &&
            !text.Contains("AI to Production", StringComparison.OrdinalIgnoreCase) &&
            !text.Contains("mandatory external control plane", StringComparison.OrdinalIgnoreCase) &&
            !text.Contains("bypass authorization", StringComparison.OrdinalIgnoreCase) &&
            !text.Contains("mutable evidence", StringComparison.OrdinalIgnoreCase);
    }

    private static ExistingArchitectureConformanceDecision Decision(Guid discoveryId, Guid? itemId,
        string tenantId, bool conformant, string code, ImmutableArray<string> evidence, DateTimeOffset decidedAt)
    {
        var canonical = string.Join('|', discoveryId.ToString("D"), itemId?.ToString("D") ?? "scope",
            tenantId, Authority, conformant, code);
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return new(discoveryId, itemId, tenantId, conformant, code,
            evidence.Append($"evidence://existing-architecture/conformance/{discoveryId:D}/{itemId?.ToString("D") ?? "scope"}/sha256/{digest}")
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray(), decidedAt);
    }
}
