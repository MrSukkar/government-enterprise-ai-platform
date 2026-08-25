using System.Collections.Immutable;

namespace Platform.SoftwareFactory.DeveloperExperience;

public sealed class GovernedDeveloperExperienceService(
    IDeveloperEnvironmentInspector environmentInspector,
    IDeveloperTemplateVerifier templateVerifier,
    IDeveloperWorkspacePlanRegistry planRegistry)
{
    public async Task<DeveloperWorkspacePlan> PrepareAsync(
        DeveloperWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var verification = await templateVerifier.VerifyAsync(request.Template, cancellationToken);
        ValidateTemplateVerification(request, verification);
        var environment = await environmentInspector.InspectAsync(request, cancellationToken);
        ValidateEnvironment(request, verification, environment);

        var foundationEvidence = request.Template.EvidenceReferences
            .Add(verification.VerificationEvidenceReference)
            .AddRange(environment.EvidenceReferences)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var steps = CreateGoldenPath(request, foundationEvidence);
        var plan = new DeveloperWorkspacePlan(
            request.RequestId, request.TenantId, request.DeveloperSubjectId, request.WorkspaceRelativePath,
            request.GitRepositoryReference, request.GitBranch, request.Template.TemplateId,
            request.Template.Version, request.Template.Sha256Digest, steps, foundationEvidence, environment.InspectedAt);
        var registered = await planRegistry.RegisterAtomicallyAsync(plan, cancellationToken);
        ValidateRegistered(plan, registered);
        return registered;
    }

    private static ImmutableArray<DeveloperWorkflowStep> CreateGoldenPath(
        DeveloperWorkspaceRequest request,
        ImmutableArray<string> evidence) =>
    [
        new(0, DeveloperWorkflowStage.MaterializeApprovedTemplate, "platform-dev", ["template", "materialize", request.Template.TemplateId, request.Template.Version], false, evidence),
        new(1, DeveloperWorkflowStage.RestoreLockedDependencies, "dotnet", ["restore", "--locked-mode", "--source", request.LocalPackageSourceReference], false, evidence),
        new(2, DeveloperWorkflowStage.VerifyProject, "powershell", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/verify-project.ps1"], false, evidence),
        new(3, DeveloperWorkflowStage.Build, "dotnet", ["build", "--no-restore"], false, evidence),
        new(4, DeveloperWorkflowStage.Test, "dotnet", ["test", "--no-build"], false, evidence),
        new(5, DeveloperWorkflowStage.ReviewChanges, "git", ["diff", "--check"], true, evidence),
        new(6, DeveloperWorkflowStage.SubmitToGitAndCi, "git", ["status", "--short"], true, evidence)
    ];

    private static void ValidateTemplateVerification(
        DeveloperWorkspaceRequest request,
        DeveloperTemplateVerification verification)
    {
        ArgumentNullException.ThrowIfNull(verification);
        if (!verification.SignatureValid ||
            !StringComparer.Ordinal.Equals(verification.TemplateId, request.Template.TemplateId) ||
            !StringComparer.Ordinal.Equals(verification.Version, request.Template.Version) ||
            !StringComparer.OrdinalIgnoreCase.Equals(verification.Sha256Digest, request.Template.Sha256Digest))
            throw new UnauthorizedAccessException("Developer template verification failed closed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(verification.VerificationEvidenceReference);
        if (verification.VerifiedAt < request.RequestedAt)
            throw new InvalidOperationException("Template verification predates the workspace request.");
    }

    private static void ValidateEnvironment(
        DeveloperWorkspaceRequest request,
        DeveloperTemplateVerification verification,
        DeveloperEnvironmentSnapshot environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (environment.RequestId != request.RequestId || !environment.GitAvailable ||
            !environment.LocalPackageSourceAvailable || environment.HasProductionCredentials ||
            environment.OutboundNetworkRequired ||
            !StringComparer.Ordinal.Equals(environment.DotNetSdkVersion, request.Template.RequiredDotNetSdkVersion))
            throw new InvalidOperationException("Developer environment is not approved or sovereign-ready.");
        if (environment.EvidenceReferences.IsDefaultOrEmpty || environment.InspectedAt < verification.VerifiedAt)
            throw new InvalidOperationException("Developer environment requires current evidence.");
    }

    private static void ValidateRegistered(DeveloperWorkspacePlan expected, DeveloperWorkspacePlan registered)
    {
        ArgumentNullException.ThrowIfNull(registered);
        if (registered.RequestId != expected.RequestId ||
            !StringComparer.Ordinal.Equals(registered.TenantId, expected.TenantId) ||
            !StringComparer.Ordinal.Equals(registered.DeveloperSubjectId, expected.DeveloperSubjectId) ||
            !StringComparer.Ordinal.Equals(registered.WorkspaceRelativePath, expected.WorkspaceRelativePath) ||
            !StringComparer.Ordinal.Equals(registered.TemplateSha256Digest, expected.TemplateSha256Digest) ||
            !registered.Steps.SequenceEqual(expected.Steps) ||
            !registered.EvidenceReferences.ToHashSet(StringComparer.Ordinal).SetEquals(expected.EvidenceReferences) ||
            registered.CreatedAt != expected.CreatedAt || registered.IsProductionDeploymentCapable ||
            !registered.RequiresHumanReview)
            throw new InvalidOperationException("Developer workspace plan registry changed governed state.");
    }
}
