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
    ImmutableArray<string> Constraints)
{
    public AiDevelopmentRequest Validate()
    {
        Run.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(PromptTemplateId);
        if (AuthorizedContextReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Authorized enterprise context is required for AI development.");
        foreach (var package in ApprovedPackages) package.Validate();
        return this;
    }
}
