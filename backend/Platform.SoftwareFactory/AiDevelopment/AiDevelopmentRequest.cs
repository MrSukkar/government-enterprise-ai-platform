using System.Collections.Immutable;
using Platform.SoftwareFactory.Delivery;
using Platform.SoftwareFactory.Packages;

namespace Platform.SoftwareFactory.AiDevelopment;

public sealed record AiDevelopmentRequest(
    SoftwareDeliveryRun Run,
    AiDevelopmentTaskKind TaskKind,
    string Purpose,
    string PromptTemplateId,
    ImmutableArray<string> AuthorizedContextReferences,
    ImmutableArray<PackageCoordinate> ApprovedPackages,
    ImmutableArray<string> Constraints,
    string? VerifiedPromptContent = null,
    ImmutableArray<AiDevelopmentContextItem> AuthorizedContextItems = default,
    int RequestTimeoutSeconds = 0,
    int MaximumRequestBytes = 0,
    int MaximumResponseBytes = 0,
    ImmutableArray<string> AuthorizedOutputPaths = default)
{
    public AiDevelopmentRequest Validate()
    {
        Run.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(PromptTemplateId);
        if (AuthorizedContextReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Authorized enterprise context is required for AI development.");
        if (!AuthorizedContextItems.IsDefault)
        {
            foreach (var item in AuthorizedContextItems) item.Validate();
            if (!AuthorizedContextItems.Select(item => item.Reference).ToImmutableHashSet(StringComparer.Ordinal)
                    .SetEquals(AuthorizedContextReferences))
                throw new InvalidOperationException("AI context material does not match its authorized references.");
        }
        if (TaskKind == AiDevelopmentTaskKind.Planning &&
            (string.IsNullOrWhiteSpace(VerifiedPromptContent) || AuthorizedContextItems.IsDefaultOrEmpty ||
             RequestTimeoutSeconds <= 0 || MaximumRequestBytes <= 0 || MaximumResponseBytes <= 0))
            throw new InvalidOperationException("Operational AI Planning requires verified content and deployment safety bounds.");
        if (TaskKind == AiDevelopmentTaskKind.CodeGeneration &&
            (string.IsNullOrWhiteSpace(VerifiedPromptContent) || AuthorizedContextItems.IsDefaultOrEmpty ||
             AuthorizedOutputPaths.IsDefaultOrEmpty || RequestTimeoutSeconds <= 0 ||
             MaximumRequestBytes <= 0 || MaximumResponseBytes <= 0))
            throw new InvalidOperationException("Operational Code Generation requires verified content, authorized paths, and deployment safety bounds.");
        foreach (var package in ApprovedPackages) package.Validate();
        return this;
    }
}
