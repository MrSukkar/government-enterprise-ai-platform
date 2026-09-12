using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.Identity.Authentication;
using Platform.Knowledge.Retrieval;
using Platform.Integrations.ExistingSystems;
using Platform.Integrations.ExistingArchitecture;
using Platform.SoftwareFactory.InternalService;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.AiDevelopment;
using Platform.SoftwareFactory.Validation;
using Platform.SoftwareFactory.Sandbox;
using Platform.SoftwareFactory.SupplyChain;
using Platform.EnterpriseModel.Registration;
using Platform.EnterpriseModel.Model;
using Platform.Evidence.Chain;

namespace Platform.Api.InternalService;

internal static class InternalServiceEndpoint
{
    private sealed record GovernedIntentRegistrationInput(
        Guid RegistrationId,
        GovernedIntentSubmission Submission,
        string Environment,
        IntentPolicyBundleReference PolicyBundle,
        long ExpectedVersion);

    private sealed record EnterpriseContextDiscoveryInput(
        Guid DiscoveryId,
        long ExpectedRegistrationVersion,
        string ExpectedIntentSha256Digest,
        string Purpose,
        DataClassification RegistrationClassification,
        string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record ExistingSystemsDiscoveryInput(
        Guid DiscoveryId,
        long ExpectedRegistrationVersion,
        string ExpectedIntentSha256Digest,
        string ExpectedContextSha256Digest,
        string Purpose,
        DataClassification MaximumClassification,
        string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record ExistingArchitectureDiscoveryInput(
        Guid DiscoveryId,
        long ExpectedRegistrationVersion,
        string ExpectedIntentSha256Digest,
        string ExpectedContextSha256Digest,
        string ExpectedInventorySha256Digest,
        string Purpose,
        DataClassification MaximumClassification,
        string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record ApprovedPackagesSelectionInput(
        Guid SelectionId,
        long ExpectedRegistrationVersion,
        string ExpectedIntentSha256Digest,
        string ExpectedContextSha256Digest,
        string ExpectedInventorySha256Digest,
        string ExpectedArchitectureSha256Digest,
        System.Collections.Immutable.ImmutableArray<PackageCoordinate> RequestedCoordinates,
        string Purpose,
        DataClassification MaximumClassification,
        string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record AiPlanningInput(
        Guid PlanningId, Guid DeliveryRunId, long ExpectedRegistrationVersion,
        string ExpectedArchitectureSha256Digest, string ExpectedSelectionSha256Digest,
        string PromptTemplateId, string PromptTemplateVersion, string RuntimeProfile,
        System.Collections.Immutable.ImmutableArray<string> ContextReferences,
        System.Collections.Immutable.ImmutableArray<string> Constraints,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record CodeGenerationInput(
        Guid GenerationId, Guid DeliveryRunId,
        string ExpectedSelectionSha256Digest, string ExpectedPlanningSha256Digest,
        string PromptTemplateId, string PromptTemplateVersion, string RuntimeProfile,
        System.Collections.Immutable.ImmutableArray<string> ContextReferences,
        System.Collections.Immutable.ImmutableArray<string> Constraints,
        System.Collections.Immutable.ImmutableArray<string> RequestedOutputPaths,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record StaticValidationInput(
        Guid ValidationId, Guid DeliveryRunId,
        string ExpectedCandidateSha256Digest, string ExpectedGenerationEvidenceReference,
        System.Collections.Immutable.ImmutableArray<string> RequiredControlIds,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record SecurityValidationInput(
        Guid ValidationId, Guid DeliveryRunId, string ExpectedCandidateSha256Digest,
        string ExpectedStaticReportSha256Digest, string ExpectedStaticEvidenceReference,
        System.Collections.Immutable.ImmutableArray<string> RequiredControlIds,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record SandboxExecutionInput(
        Guid ExecutionId, Guid DeliveryRunId, string ExpectedCandidateSha256Digest,
        string ExpectedSecurityReportSha256Digest, string ExpectedSecurityEvidenceReference,
        PackageCoordinate SandboxImage, SandboxIsolationPolicy IsolationPolicy,
        System.Collections.Immutable.ImmutableDictionary<string, string> NonSecretEnvironmentReferences,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record TestsExecutionInput(
        Guid ExecutionId, Guid DeliveryRunId, string ExpectedCandidateSha256Digest,
        string ExpectedSecurityReportSha256Digest, string ExpectedSandboxResultSha256Digest,
        string ExpectedSandboxEvidenceReference, string TestManifestReference,
        string ExpectedTestManifestSha256Digest,
        System.Collections.Immutable.ImmutableHashSet<string> RequiredTestIds,
        System.Collections.Immutable.ImmutableHashSet<string> AllowedTestCategories,
        PackageCoordinate TestImage, SandboxIsolationPolicy IsolationPolicy,
        System.Collections.Immutable.ImmutableDictionary<string, string> NonSecretEnvironmentReferences,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record HumanReviewInput(
        Guid ReviewId, Guid DeliveryRunId, string InitiatorSubjectId, string ExpectedCandidateSha256Digest,
        string ExpectedSecurityReportSha256Digest, string ExpectedSandboxResultSha256Digest,
        string ExpectedTestsResultSha256Digest, string ExpectedTestsEvidenceReference,
        HumanReviewDecision Decision, string Rationale, string HumanAttestationReference,
        System.Collections.Immutable.ImmutableHashSet<string> DeclaredConflictingSubjectIds,
        long ExpectedVersion, string Purpose, DataClassification MaximumClassification,
        string Environment, IntentPolicyBundleReference PolicyBundle);

    private sealed record GitSourceCommitInput(
        Guid OperationId, Guid DeliveryRunId, string ExpectedCandidateSha256Digest,
        string ExpectedReviewPackageSha256Digest, string ExpectedReviewEvidenceReference,
        string ExpectedTestsResultSha256Digest, string ExpectedChangeSetSha256Digest,
        System.Collections.Immutable.ImmutableHashSet<string> AuthorizedRelativePaths,
        string RepositoryId, string ExpectedBaseCommitId, string ChangeBranch,
        string CommitMessage, string SigningPolicyReference, string Purpose,
        DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record CiCdExecutionInput(
        Guid ExecutionId, Guid DeliveryRunId, string RepositoryId,
        string ExpectedCommitId, string ExpectedTreeSha256Digest,
        string ExpectedChangeSetSha256Digest, string WorkflowDefinitionId,
        string WorkflowDefinitionVersion, string ExpectedWorkflowSha256Digest,
        string WorkflowSignatureReference, string PipelineProfile, string RunnerPoolId,
        System.Collections.Immutable.ImmutableArray<string> RequiredStageIds,
        System.Collections.Immutable.ImmutableHashSet<string> RequiredControlIds,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record ArtifactPublicationInput(
        Guid PublicationId, Guid DeliveryRunId,
        string ExpectedPipelineManifestSha256Digest, string ExpectedSourceCommitId,
        string ExpectedWorkflowSha256Digest, GovernedArtifactCoordinate Coordinate,
        string ExpectedContentSha256Digest, string RegistryId, string RegistryRepository,
        string SigningPolicyReference,
        System.Collections.Immutable.ImmutableHashSet<SupplyChainControl> RequiredControls,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record SovereignDeploymentInput(
        Guid DeploymentId, Guid DeliveryRunId,
        string ExpectedArtifactContentSha256Digest, string ExpectedImmutableRegistryReference,
        string ExpectedArtifactEvidenceReference, Guid DeploymentProfileId,
        string DeploymentProfileVersion, string ExpectedDeploymentProfileSha256Digest,
        string TargetEnvironment, bool ProductionDeploymentRequested,
        string HumanApprovalReference, string WorkloadIdentityReference,
        string SecretsPolicyReference, string RollbackPolicyReference,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record OpenTelemetryActivationInput(
        Guid ActivationId, Guid DeliveryRunId, string ExpectedRuntimeIdentity,
        string ExpectedArtifactContentSha256Digest, string ExpectedDeploymentEvidenceReference,
        bool ExpectedProductionEffectOccurred, Guid TelemetryProfileId,
        string TelemetryProfileVersion, string ExpectedTelemetryProfileSha256Digest,
        string ExpectedServiceName, string ExpectedServiceVersion,
        System.Collections.Immutable.ImmutableHashSet<GovernedTelemetrySignal> RequiredSignals,
        string ExpectedRedactionPolicyReference, string ExpectedRedactionPolicySha256Digest,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record AutomaticRegistrationInput(
        Guid RegistrationId, Guid DeliveryRunId, Guid ManifestId, string ManifestVersion,
        string ExpectedManifestSha256Digest, string ExpectedRuntimeIdentity,
        string ExpectedServiceIdentity, string ExpectedServiceVersion, string ExpectedArtifactDigest,
        string ExpectedOpenTelemetryEvidenceReference, string Purpose,
        DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record EnterpriseModelContextualizationInput(
        Guid ContextualizationId, Guid DeliveryRunId, Guid ExpectedEnterpriseObjectId,
        string ExpectedRequestFingerprint, string ExpectedRegistrationEvidenceReference,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    private sealed record EvidenceCompletionInput(
        Guid CompletionId, Guid DeliveryRunId, Guid ChainId, string CorrelationId,
        string ExpectedContextualizationEvidenceReference, string PayloadSha256Digest,
        System.Collections.Immutable.ImmutableArray<string> TraceReferences,
        string Purpose, DataClassification MaximumClassification, string Environment,
        IntentPolicyBundleReference PolicyBundle);

    internal static IEndpointConventionBuilder MapInternalServiceFoundation(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet("/api/v1/internal-services/foundation", () =>
            Results.Ok(InternalServiceFoundationCatalog.Current))
            .WithName("GetInternalServiceFoundation")
            .WithTags("Create Internal Service")
            .WithSummary("Read the approved Create Internal Service product foundation.")
            .WithDescription("Public product metadata grants no execution authority.")
            .Produces<InternalServiceFoundation>(StatusCodes.Status200OK)
            .AllowAnonymous();
    }

    internal static IEndpointConventionBuilder MapInternalServiceIntentSubmission(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapPost("/api/v1/internal-services/intents", (
                GovernedIntentSubmission submission,
                HttpContext httpContext,
                GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedIntentSubmissionValidator validator) =>
            {
                try
                {
                    var requestContext = contextFactory.Create(httpContext.User);
                    if (!StringComparer.Ordinal.Equals(
                            submission.AuthorizationEvidenceReference,
                            requestContext.AuthorizationEvidenceReference))
                        throw new UnauthorizedAccessException("Authorization evidence does not match the governed identity context.");

                    var decision = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        requestContext.Identity,
                        submission.Purpose,
                        "internal-service.intent.submit",
                        submission.SubmissionId.ToString("D"),
                        submission.TenantId,
                        submission.Classification,
                        RequiredRoles: [],
                        RequiredPermissions: ["developer.internal-service.create"],
                        InitiatorSubjectId: requestContext.Identity.SubjectId,
                        RequiresDistinctApprover: false));
                    if (!decision.IsAllowed)
                        throw new UnauthorizedAccessException($"Governed intent submission denied: {decision.Code}.");

                    var receipt = validator.Validate(
                        submission,
                        requestContext.Identity.SubjectId,
                        DateTimeOffset.UtcNow);
                    return Results.Ok(receipt);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Governed intent submission denied.");
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Governed intent submission is invalid.");
                }
            })
            .WithName("SubmitInternalServiceIntent")
            .WithTags("Create Internal Service")
            .WithSummary("Validate a governed internal-service intent submission.")
            .WithDescription("Requires an authenticated, tenant-scoped identity with purpose, classification, permission, and authorization evidence. Validation creates no institutional state and cannot execute material work.")
            .Accepts<GovernedIntentSubmission>("application/json")
            .Produces<GovernedIntentValidationReceipt>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceIntentRegistration(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapPost("/api/v1/internal-services/intents/register", async (
                GovernedIntentRegistrationInput input,
                HttpContext httpContext,
                IServiceProvider services,
                GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedIntentRegistrationEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(input.Submission);
                    var requestContext = contextFactory.Create(httpContext.User);
                    var accessDecision = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        requestContext.Identity,
                        input.Submission.Purpose,
                        "internal-service.intent.register",
                        input.RegistrationId.ToString("D"),
                        input.Submission.TenantId,
                        input.Submission.Classification,
                        RequiredRoles: [],
                        RequiredPermissions: ["developer.internal-service.intent.register"],
                        InitiatorSubjectId: requestContext.Identity.SubjectId,
                        RequiresDistinctApprover: false));
                    if (!accessDecision.IsAllowed)
                        throw new UnauthorizedAccessException($"Governed intent registration denied: {accessDecision.Code}.");

                    var policyGate = services.GetService<IGovernedIntentPolicyGate>();
                    var repository = services.GetService<IGovernedIntentRegistrationRepository>();
                    if (policyGate is null || repository is null)
                        return Results.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Governed intent registration is not operationally ready.");

                    var request = new GovernedIntentRegistrationRequest(
                        input.RegistrationId,
                        input.Submission,
                        requestContext.Identity.SubjectId,
                        requestContext.Identity.TenantId,
                        requestContext.Identity.Clearance,
                        requestContext.Identity.Permissions,
                        requestContext.AuthorizationEvidenceReference,
                        input.Environment,
                        input.PolicyBundle,
                        input.ExpectedVersion,
                        DateTimeOffset.UtcNow);
                    var receipt = await engine.RegisterAsync(request, policyGate, repository, cancellationToken);
                    if (receipt.PolicyOutcome != GovernedIntentPolicyOutcome.Permit)
                        return Results.Json(receipt, statusCode: StatusCodes.Status403Forbidden);
                    return Results.Json(
                        receipt,
                        statusCode: receipt.Disposition == GovernedIntentRegistrationDisposition.Created
                            ? StatusCodes.Status201Created
                            : StatusCodes.Status200OK);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Governed intent registration denied.");
                }
                catch (GovernedIntentConcurrencyException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Governed intent registration version conflict.");
                }
                catch (GovernedIntentPersistenceUnavailableException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Governed intent persistence is unavailable.");
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Governed intent registration is invalid.");
                }
            })
            .WithName("RegisterInternalServiceIntent")
            .WithTags("Create Internal Service")
            .WithSummary("Register a validated internal-service intent after OPA approval.")
            .WithDescription("Requires an authenticated, authorized identity plus configured OPA and atomic evidence-bearing persistence adapters. Policy denial or unavailable adapters cannot create institutional state.")
            .Accepts<GovernedIntentRegistrationInput>("application/json")
            .Produces<GovernedIntentRegistrationReceipt>(StatusCodes.Status200OK)
            .Produces<GovernedIntentRegistrationReceipt>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceEnterpriseContextDiscovery(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapPost("/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context", async (
                Guid registrationId,
                EnterpriseContextDiscoveryInput input,
                HttpContext httpContext,
                IServiceProvider services,
                GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator,
                AuthorizedEnterpriseContextDiscoveryEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(input.PolicyBundle);
                    var requestContext = contextFactory.Create(httpContext.User);
                    var accessDecision = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        requestContext.Identity,
                        input.Purpose,
                        "internal-service.enterprise-context.discover",
                        registrationId.ToString("D"),
                        requestContext.Identity.TenantId,
                        input.RegistrationClassification,
                        RequiredRoles: [],
                        RequiredPermissions: ["developer.internal-service.context.discover"],
                        InitiatorSubjectId: requestContext.Identity.SubjectId,
                        RequiresDistinctApprover: false));
                    if (!accessDecision.IsAllowed)
                        throw new UnauthorizedAccessException(
                            $"Enterprise Context discovery denied: {accessDecision.Code}.");

                    var registrationReader = services.GetService<IGovernedIntentRegistrationReader>();
                    var policyGate = services.GetService<IEnterpriseContextPolicyGate>();
                    var evidenceRecorder = services.GetService<IEnterpriseContextEvidenceRecorder>();
                    var retrievalSources = services.GetServices<IKnowledgeRetrievalSource>().ToArray();
                    if (registrationReader is null || policyGate is null || evidenceRecorder is null ||
                        retrievalSources.Length == 0)
                        return Results.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Authorized Enterprise Context discovery is not operationally ready.");

                    var request = new AuthorizedEnterpriseContextDiscoveryRequest(
                        input.DiscoveryId,
                        registrationId,
                        input.ExpectedRegistrationVersion,
                        input.ExpectedIntentSha256Digest,
                        requestContext.Identity,
                        input.Purpose,
                        input.RegistrationClassification,
                        requestContext.AuthorizationEvidenceReference,
                        input.Environment,
                        input.PolicyBundle,
                        DateTimeOffset.UtcNow);
                    var receipt = await engine.DiscoverAsync(
                        request,
                        registrationReader,
                        policyGate,
                        evidenceRecorder,
                        cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit
                        ? Results.Ok(receipt)
                        : Results.Json(receipt, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Authorized Enterprise Context discovery denied.");
                }
                catch (KeyNotFoundException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Governed intent registration was not found.");
                }
                catch (EnterpriseContextDependencyUnavailableException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "An authorized Enterprise Context retrieval dependency is unavailable.");
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Enterprise Context discovery request or boundary result is invalid.");
                }
            })
            .WithName("DiscoverInternalServiceEnterpriseContext")
            .WithTags("Create Internal Service")
            .WithSummary("Discover authorized Enterprise Context for a registered governed intent.")
            .WithDescription("OPA establishes explicit retrieval scope before source access; every candidate is re-authorized and cryptographically evidenced. The result cannot advance to Existing Systems or invoke AI.")
            .Accepts<EnterpriseContextDiscoveryInput>("application/json")
            .Produces<AuthorizedEnterpriseContextDiscoveryReceipt>(StatusCodes.Status200OK)
            .Produces<AuthorizedEnterpriseContextDiscoveryReceipt>(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceExistingSystemsDiscovery(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems",
            async (
                Guid registrationId,
                Guid contextDiscoveryId,
                ExistingSystemsDiscoveryInput input,
                HttpContext httpContext,
                IServiceProvider services,
                GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator,
                AuthorizedExistingSystemsDiscoveryEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(input.PolicyBundle);
                    var requestContext = contextFactory.Create(httpContext.User);
                    var accessDecision = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        requestContext.Identity,
                        input.Purpose,
                        "internal-service.existing-systems.discover",
                        contextDiscoveryId.ToString("D"),
                        requestContext.Identity.TenantId,
                        input.MaximumClassification,
                        RequiredRoles: [],
                        RequiredPermissions: ["developer.internal-service.systems.discover"],
                        InitiatorSubjectId: requestContext.Identity.SubjectId,
                        RequiresDistinctApprover: false));
                    if (!accessDecision.IsAllowed)
                        throw new UnauthorizedAccessException(
                            $"Existing Systems discovery denied: {accessDecision.Code}.");

                    var contextReader = services.GetService<IAuthorizedEnterpriseContextSnapshotReader>();
                    var policyGate = services.GetService<IExistingSystemsPolicyGate>();
                    var resultAuthorizer = services.GetService<IExistingSystemResultAuthorizer>();
                    var evidenceRecorder = services.GetService<IExistingSystemsEvidenceRecorder>();
                    var inventorySources = services.GetServices<IExistingSystemInventorySource>().ToArray();
                    if (contextReader is null || policyGate is null || resultAuthorizer is null ||
                        evidenceRecorder is null || inventorySources.Length == 0)
                        return Results.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Authorized Existing Systems discovery is not operationally ready.");

                    var request = new AuthorizedExistingSystemsDiscoveryRequest(
                        input.DiscoveryId,
                        contextDiscoveryId,
                        registrationId,
                        input.ExpectedRegistrationVersion,
                        input.ExpectedIntentSha256Digest,
                        input.ExpectedContextSha256Digest,
                        requestContext.Identity,
                        input.Purpose,
                        input.MaximumClassification,
                        requestContext.AuthorizationEvidenceReference,
                        input.Environment,
                        input.PolicyBundle,
                        DateTimeOffset.UtcNow);
                    var receipt = await engine.DiscoverAsync(
                        request,
                        contextReader,
                        policyGate,
                        resultAuthorizer,
                        evidenceRecorder,
                        cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit
                        ? Results.Ok(receipt)
                        : Results.Json(receipt, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Authorized Existing Systems discovery denied.");
                }
                catch (KeyNotFoundException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Authorized Enterprise Context snapshot was not found.");
                }
                catch (ExistingSystemsDependencyUnavailableException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "An authorized Existing Systems dependency is unavailable.");
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Existing Systems discovery request or boundary result is invalid.");
                }
            })
            .WithName("DiscoverInternalServiceExistingSystems")
            .WithTags("Create Internal Service")
            .WithSummary("Discover authorized existing systems for an evidence-bearing Enterprise Context snapshot.")
            .WithDescription("OPA establishes explicit system, relationship, and source scope before inventory access. Every system and relationship is structurally validated, re-authorized, and evidenced. No live connector or Existing Architecture advancement is available.")
            .Accepts<ExistingSystemsDiscoveryInput>("application/json")
            .Produces<AuthorizedExistingSystemsDiscoveryReceipt>(StatusCodes.Status200OK)
            .Produces<AuthorizedExistingSystemsDiscoveryReceipt>(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceExistingArchitectureDiscovery(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture",
            async (
                Guid registrationId,
                Guid contextDiscoveryId,
                Guid systemsDiscoveryId,
                ExistingArchitectureDiscoveryInput input,
                HttpContext httpContext,
                IServiceProvider services,
                GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator,
                AuthorizedExistingArchitectureDiscoveryEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(input.PolicyBundle);
                    var requestContext = contextFactory.Create(httpContext.User);
                    var accessDecision = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        requestContext.Identity,
                        input.Purpose,
                        "internal-service.existing-architecture.discover",
                        systemsDiscoveryId.ToString("D"),
                        requestContext.Identity.TenantId,
                        input.MaximumClassification,
                        RequiredRoles: [],
                        RequiredPermissions: ["developer.internal-service.architecture.discover"],
                        InitiatorSubjectId: requestContext.Identity.SubjectId,
                        RequiresDistinctApprover: false));
                    if (!accessDecision.IsAllowed)
                        throw new UnauthorizedAccessException(
                            $"Existing Architecture discovery denied: {accessDecision.Code}.");

                    var systemsReader = services.GetService<IAuthorizedExistingSystemsSnapshotReader>();
                    var policyGate = services.GetService<IExistingArchitecturePolicyGate>();
                    var conformanceValidator = services.GetService<IExistingArchitectureConformanceValidator>();
                    var resultAuthorizer = services.GetService<IExistingArchitectureResultAuthorizer>();
                    var evidenceRecorder = services.GetService<IExistingArchitectureEvidenceRecorder>();
                    var architectureSources = services.GetServices<IExistingArchitectureSource>().ToArray();
                    if (systemsReader is null || policyGate is null || conformanceValidator is null ||
                        resultAuthorizer is null || evidenceRecorder is null || architectureSources.Length == 0)
                        return Results.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Authorized Existing Architecture discovery is not operationally ready.");

                    var request = new AuthorizedExistingArchitectureDiscoveryRequest(
                        input.DiscoveryId,
                        systemsDiscoveryId,
                        contextDiscoveryId,
                        registrationId,
                        input.ExpectedRegistrationVersion,
                        input.ExpectedIntentSha256Digest,
                        input.ExpectedContextSha256Digest,
                        input.ExpectedInventorySha256Digest,
                        requestContext.Identity,
                        input.Purpose,
                        input.MaximumClassification,
                        requestContext.AuthorizationEvidenceReference,
                        input.Environment,
                        input.PolicyBundle,
                        DateTimeOffset.UtcNow);
                    var receipt = await engine.DiscoverAsync(
                        request,
                        systemsReader,
                        policyGate,
                        conformanceValidator,
                        resultAuthorizer,
                        evidenceRecorder,
                        cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit
                        ? Results.Ok(receipt)
                        : Results.Json(receipt, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Authorized Existing Architecture discovery denied.");
                }
                catch (KeyNotFoundException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Authorized Existing Systems snapshot was not found.");
                }
                catch (ExistingArchitectureDependencyUnavailableException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "An authorized Existing Architecture dependency is unavailable.");
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Existing Architecture discovery request or boundary result is invalid.");
                }
            })
            .WithName("DiscoverInternalServiceExistingArchitecture")
            .WithTags("Create Internal Service")
            .WithSummary("Discover authorized existing architecture for an evidence-bearing Existing Systems snapshot.")
            .WithDescription("OPA establishes explicit system, source, item, relationship, and classification scope before architecture access. Every approved architecture item is constitutionally checked, re-authorized, and evidenced. No architecture redesign or Approved Packages advancement is available.")
            .Accepts<ExistingArchitectureDiscoveryInput>("application/json")
            .Produces<AuthorizedExistingArchitectureDiscoveryReceipt>(StatusCodes.Status200OK)
            .Produces<AuthorizedExistingArchitectureDiscoveryReceipt>(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceApprovedPackagesSelection(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages",
            async (
                Guid registrationId,
                Guid contextDiscoveryId,
                Guid systemsDiscoveryId,
                Guid architectureDiscoveryId,
                ApprovedPackagesSelectionInput input,
                HttpContext httpContext,
                IServiceProvider services,
                GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedApprovedPackagesSelectionEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(input.PolicyBundle);
                    var requestContext = contextFactory.Create(httpContext.User);
                    var accessDecision = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        requestContext.Identity, input.Purpose, "internal-service.approved-packages.select",
                        architectureDiscoveryId.ToString("D"), requestContext.Identity.TenantId,
                        input.MaximumClassification, RequiredRoles: [],
                        RequiredPermissions: ["developer.internal-service.packages.select"],
                        InitiatorSubjectId: requestContext.Identity.SubjectId, RequiresDistinctApprover: false));
                    if (!accessDecision.IsAllowed)
                        throw new UnauthorizedAccessException($"Approved Packages selection denied: {accessDecision.Code}.");

                    var architectureReader = services.GetService<IAuthorizedExistingArchitectureSnapshotReader>();
                    var policyGate = services.GetService<IApprovedPackagesPolicyGate>();
                    var registryReader = services.GetService<IInstitutionalPackageRegistryReader>();
                    var supplyChainVerifier = services.GetService<IApprovedPackageSupplyChainVerifier>();
                    var resultAuthorizer = services.GetService<IApprovedPackageResultAuthorizer>();
                    var evidenceRecorder = services.GetService<IApprovedPackagesEvidenceRecorder>();
                    if (architectureReader is null || policyGate is null || registryReader is null ||
                        supplyChainVerifier is null || resultAuthorizer is null || evidenceRecorder is null)
                        return Results.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Governed Approved Packages selection is not operationally ready.");

                    var request = new GovernedApprovedPackagesSelectionRequest(
                        input.SelectionId, architectureDiscoveryId, systemsDiscoveryId, contextDiscoveryId,
                        registrationId, input.ExpectedRegistrationVersion, input.ExpectedIntentSha256Digest,
                        input.ExpectedContextSha256Digest, input.ExpectedInventorySha256Digest,
                        input.ExpectedArchitectureSha256Digest, input.RequestedCoordinates,
                        requestContext.Identity, input.Purpose, input.MaximumClassification,
                        requestContext.AuthorizationEvidenceReference, input.Environment,
                        input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.SelectAsync(
                        request, architectureReader, policyGate, registryReader, supplyChainVerifier,
                        resultAuthorizer, evidenceRecorder, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit
                        ? Results.Ok(receipt)
                        : Results.Json(receipt, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                        title: "Governed Approved Packages selection denied.");
                }
                catch (KeyNotFoundException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status404NotFound,
                        title: "Authorized Existing Architecture snapshot was not found.");
                }
                catch (ApprovedPackagesDependencyUnavailableException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "An Approved Packages dependency is unavailable.");
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                        title: "Approved Packages request or boundary result is invalid.");
                }
            })
            .WithName("SelectInternalServiceApprovedPackages")
            .WithTags("Create Internal Service")
            .WithSummary("Select exact institutionally approved packages for an authorized architecture snapshot.")
            .WithDescription("OPA authorizes exact immutable coordinates before registry reads. Eligibility, provenance, SBOM, signature, sovereign-registry assurance, and per-result authorization are required. No package transfer, execution, or AI Planning advancement is available.")
            .Accepts<ApprovedPackagesSelectionInput>("application/json")
            .Produces<GovernedApprovedPackagesSelectionReceipt>(StatusCodes.Status200OK)
            .Produces<GovernedApprovedPackagesSelectionReceipt>(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceAiPlanning(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages/{packageSelectionId:guid}/ai-planning",
            async (Guid registrationId, Guid contextDiscoveryId, Guid systemsDiscoveryId,
                Guid architectureDiscoveryId, Guid packageSelectionId, AiPlanningInput input,
                HttpContext httpContext, IServiceProvider services,
                GovernedRequestContextFactory contextFactory, IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedAiPlanningEngine engine, CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.ai-planning.create",
                        packageSelectionId.ToString("D"), context.Identity.TenantId,
                        input.MaximumClassification, [], ["developer.internal-service.ai-planning.create"],
                        context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var packagesReader = services.GetService<IAuthorizedApprovedPackagesSnapshotReader>();
                    var runReader = services.GetService<IAiPlanningDeliveryRunReader>();
                    var policyGate = services.GetService<IAiPlanningPolicyGate>();
                    var promptReader = services.GetService<IGovernedPlanningPromptTemplateReader>();
                    var contextAuthorizer = services.GetService<IAiPlanningContextAuthorizer>();
                    var runtime = services.GetService<IAiDevelopmentRuntime>();
                    var evaluator = services.GetService<IAiOutputEvaluator>();
                    var resultAuthorizer = services.GetService<IAiPlanningResultAuthorizer>();
                    var evidenceRecorder = services.GetService<IAiPlanningEvidenceRecorder>();
                    if (packagesReader is null || runReader is null || policyGate is null || promptReader is null ||
                        contextAuthorizer is null || runtime is null || evaluator is null || resultAuthorizer is null || evidenceRecorder is null)
                        return Results.Problem(statusCode: 503, title: "Governed AI Planning is not operationally ready.");
                    var request = new GovernedAiPlanningRequest(
                        input.PlanningId, packageSelectionId, architectureDiscoveryId, systemsDiscoveryId,
                        contextDiscoveryId, registrationId, input.DeliveryRunId, input.ExpectedRegistrationVersion,
                        input.ExpectedArchitectureSha256Digest, input.ExpectedSelectionSha256Digest,
                        input.PromptTemplateId, input.PromptTemplateVersion, input.RuntimeProfile,
                        input.ContextReferences, input.Constraints, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference, input.Environment,
                        input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.PlanAsync(request, packagesReader, runReader, policyGate,
                        promptReader, contextAuthorizer, runtime, evaluator, resultAuthorizer, evidenceRecorder, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed AI Planning denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "AI Planning prerequisite was not found."); }
                catch (AiPlanningDependencyUnavailableException) { return Results.Problem(statusCode: 503, title: "An AI Planning dependency is unavailable."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "AI Planning request or boundary result is invalid."); }
            })
            .WithName("CreateGovernedAiPlanningCandidate").WithTags("Create Internal Service")
            .WithSummary("Create a governed non-executable AI planning candidate.")
            .WithDescription("OPA, verified prompt, re-authorized context, exact approved packages, independent evaluation, result authorization, and evidence are mandatory. This planning endpoint cannot generate files, invoke tools, advance workflow, or invoke Code Generation.")
            .Accepts<AiPlanningInput>("application/json").Produces<GovernedAiPlanningReceipt>(200)
            .Produces<GovernedAiPlanningReceipt>(403).ProducesProblem(400).ProducesProblem(401)
            .ProducesProblem(404).ProducesProblem(503).RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceCodeGeneration(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages/{packageSelectionId:guid}/ai-planning/{planningId:guid}/code-generation",
            async (Guid registrationId, Guid contextDiscoveryId, Guid systemsDiscoveryId,
                Guid architectureDiscoveryId, Guid packageSelectionId, Guid planningId,
                CodeGenerationInput input, HttpContext httpContext, IServiceProvider services,
                GovernedRequestContextFactory contextFactory, IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedCodeGenerationEngine engine, CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.code-generation.create",
                        planningId.ToString("D"), context.Identity.TenantId,
                        input.MaximumClassification, [], ["developer.internal-service.code-generation.create"],
                        context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();

                    var planningReader = services.GetService<IAuthorizedAiPlanningCandidateReader>();
                    var packagesReader = services.GetService<IAuthorizedApprovedPackagesSnapshotReader>();
                    var runReader = services.GetService<ICodeGenerationDeliveryRunReader>();
                    var policyGate = services.GetService<ICodeGenerationPolicyGate>();
                    var promptReader = services.GetService<IGovernedCodeGenerationPromptTemplateReader>();
                    var contextAuthorizer = services.GetService<ICodeGenerationContextAuthorizer>();
                    var runtime = services.GetService<ICodeGenerationAiDevelopmentRuntime>();
                    var evaluator = services.GetService<ICodeGenerationAiOutputEvaluator>();
                    var resultAuthorizer = services.GetService<ICodeGenerationResultAuthorizer>();
                    var evidenceRecorder = services.GetService<ICodeGenerationEvidenceRecorder>();
                    if (planningReader is null || packagesReader is null || runReader is null || policyGate is null ||
                        promptReader is null || contextAuthorizer is null || runtime is null || evaluator is null ||
                        resultAuthorizer is null || evidenceRecorder is null)
                        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Governed Code Generation is not operationally ready.");

                    var request = new GovernedCodeGenerationRequest(
                        input.GenerationId, planningId, packageSelectionId, input.DeliveryRunId,
                        input.ExpectedSelectionSha256Digest, input.ExpectedPlanningSha256Digest,
                        input.PromptTemplateId, input.PromptTemplateVersion, input.RuntimeProfile,
                        input.ContextReferences, input.Constraints, input.RequestedOutputPaths,
                        context.Identity, input.Purpose, input.MaximumClassification,
                        context.AuthorizationEvidenceReference, input.Environment, input.PolicyBundle,
                        DateTimeOffset.UtcNow);
                    var receipt = await engine.GenerateAsync(
                        request, planningReader, packagesReader, runReader, policyGate, promptReader,
                        contextAuthorizer, runtime, evaluator, resultAuthorizer, evidenceRecorder,
                        cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit
                        ? Results.Ok(receipt)
                        : Results.Json(receipt, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                        title: "Governed Code Generation denied.");
                }
                catch (KeyNotFoundException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status404NotFound,
                        title: "Code Generation prerequisite was not found.");
                }
                catch (CodeGenerationDependencyUnavailableException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "A Code Generation dependency is unavailable.");
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                        title: "Code Generation request or boundary result is invalid.");
                }
            })
            .WithName("CreateGovernedCodeGenerationCandidate")
            .WithTags("Create Internal Service")
            .WithSummary("Create a governed non-executable and unapplied code candidate.")
            .WithDescription("OPA, exact AI Planning evidence, verified prompt, re-authorized context, safe relative paths, independent evaluation, result authorization, and evidence are mandatory. This endpoint cannot write or execute files, advance workflow, or invoke Static Validation.")
            .Accepts<CodeGenerationInput>("application/json")
            .Produces<GovernedCodeGenerationReceipt>(StatusCodes.Status200OK)
            .Produces<GovernedCodeGenerationReceipt>(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceStaticValidation(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages/{packageSelectionId:guid}/ai-planning/{planningId:guid}/code-generation/{generationId:guid}/static-validation",
            async (Guid registrationId, Guid contextDiscoveryId, Guid systemsDiscoveryId,
                Guid architectureDiscoveryId, Guid packageSelectionId, Guid planningId, Guid generationId,
                StaticValidationInput input, HttpContext httpContext, IServiceProvider services,
                GovernedRequestContextFactory contextFactory, IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedStaticValidationEngine engine, CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.static-validation.create",
                        generationId.ToString("D"), context.Identity.TenantId,
                        input.MaximumClassification, [], ["developer.internal-service.static-validation.create"],
                        context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();

                    var policyGate = services.GetService<IStaticValidationPolicyGate>();
                    var candidateReader = services.GetService<IStaticValidationCodeGenerationCandidateReader>();
                    var runReader = services.GetService<IStaticValidationDeliveryRunReader>();
                    var controls = services.GetServices<ICodeValidationControl>().ToArray();
                    var resultAuthorizer = services.GetService<IStaticValidationResultAuthorizer>();
                    var evidenceRecorder = services.GetService<IStaticValidationEvidenceRecorder>();
                    if (policyGate is null || candidateReader is null || runReader is null || controls.Length == 0 ||
                        resultAuthorizer is null || evidenceRecorder is null)
                        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Governed Static Validation is not operationally ready.");

                    var request = new GovernedStaticValidationRequest(
                        input.ValidationId, generationId, input.DeliveryRunId,
                        input.ExpectedCandidateSha256Digest, input.ExpectedGenerationEvidenceReference,
                        input.RequiredControlIds, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.ValidateAsync(
                        request, policyGate, candidateReader, runReader, controls,
                        resultAuthorizer, evidenceRecorder, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt)
                        : Results.Json(receipt, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                        title: "Governed Static Validation denied.");
                }
                catch (KeyNotFoundException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status404NotFound,
                        title: "Static Validation prerequisite was not found.");
                }
                catch (StaticValidationDependencyUnavailableException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "A Static Validation dependency is unavailable.");
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                        title: "Static Validation request or boundary result is invalid.");
                }
            })
            .WithName("RunGovernedStaticValidation")
            .WithTags("Create Internal Service")
            .WithSummary("Run governed Static Validation against an authoritative inert code candidate.")
            .WithDescription("Verified OPA authorizes the exact candidate and Static controls before candidate read or control execution. All required controls must pass with evidence. This endpoint cannot invoke Security Validation, execute code, mutate source, or advance workflow.")
            .Accepts<StaticValidationInput>("application/json")
            .Produces<GovernedStaticValidationReceipt>(StatusCodes.Status200OK)
            .Produces<GovernedStaticValidationReceipt>(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceSecurityValidation(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages/{packageSelectionId:guid}/ai-planning/{planningId:guid}/code-generation/{generationId:guid}/static-validation/{staticValidationId:guid}/security-validation",
            async (Guid registrationId, Guid contextDiscoveryId, Guid systemsDiscoveryId, Guid architectureDiscoveryId,
                Guid packageSelectionId, Guid planningId, Guid generationId, Guid staticValidationId,
                SecurityValidationInput input, HttpContext httpContext, IServiceProvider services,
                GovernedRequestContextFactory contextFactory, IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedSecurityValidationEngine engine, CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.security-validation.create",
                        staticValidationId.ToString("D"), context.Identity.TenantId, input.MaximumClassification,
                        [], ["developer.internal-service.security-validation.create"], context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<ISecurityValidationPolicyGate>();
                    var staticReader = services.GetService<IAuthorizedStaticValidationReceiptReader>();
                    var candidateReader = services.GetService<ISecurityValidationCodeGenerationCandidateReader>();
                    var runReader = services.GetService<ISecurityValidationDeliveryRunReader>();
                    var controls = services.GetServices<ICodeValidationControl>().ToArray();
                    var authorizer = services.GetService<ISecurityValidationResultAuthorizer>();
                    var evidence = services.GetService<ISecurityValidationEvidenceRecorder>();
                    if (policy is null || staticReader is null || candidateReader is null || runReader is null ||
                        controls.Length == 0 || authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed Security Validation is not operationally ready.");
                    var request = new GovernedSecurityValidationRequest(
                        input.ValidationId, staticValidationId, generationId, input.DeliveryRunId,
                        input.ExpectedCandidateSha256Digest, input.ExpectedStaticReportSha256Digest,
                        input.ExpectedStaticEvidenceReference, input.RequiredControlIds, context.Identity,
                        input.Purpose, input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.ValidateAsync(request, policy, staticReader, candidateReader,
                        runReader, controls, authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Security Validation denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Security Validation prerequisite was not found."); }
                catch (SecurityValidationDependencyUnavailableException) { return Results.Problem(statusCode: 503, title: "A Security Validation dependency is unavailable."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Security Validation request or boundary result is invalid."); }
            })
            .WithName("RunGovernedSecurityValidation").WithTags("Create Internal Service")
            .WithSummary("Run governed Security Validation after accepted Static Validation.")
            .WithDescription("Verified OPA authorizes exact Static evidence, candidate, run, and signed Security controls before reads or control execution. Every exact control must pass with evidence. This endpoint cannot invoke Sandbox, execute code, mutate source, or advance workflow.")
            .Accepts<SecurityValidationInput>("application/json")
            .Produces<GovernedSecurityValidationReceipt>(200).Produces<GovernedSecurityValidationReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceSandbox(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages/{packageSelectionId:guid}/ai-planning/{planningId:guid}/code-generation/{generationId:guid}/static-validation/{staticValidationId:guid}/security-validation/{securityValidationId:guid}/sandbox",
            async (Guid registrationId, Guid contextDiscoveryId, Guid systemsDiscoveryId, Guid architectureDiscoveryId,
                Guid packageSelectionId, Guid planningId, Guid generationId, Guid staticValidationId,
                Guid securityValidationId, SandboxExecutionInput input, HttpContext httpContext,
                IServiceProvider services, GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator, GovernedSandboxExecutionEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.sandbox.execute",
                        securityValidationId.ToString("D"), context.Identity.TenantId,
                        input.MaximumClassification, [], ["developer.internal-service.sandbox.execute"],
                        context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<ISandboxPolicyGate>();
                    var securityReader = services.GetService<ISandboxSecurityValidationReceiptReader>();
                    var candidateReader = services.GetService<ISandboxCodeGenerationCandidateReader>();
                    var runReader = services.GetService<ISandboxDeliveryRunReader>();
                    var registry = services.GetService<IInstitutionalPackageRegistryReader>();
                    var assurance = services.GetService<IApprovedPackageSupplyChainVerifier>();
                    var runtime = services.GetService<ISecuritySandboxRuntime>();
                    var authorizer = services.GetService<ISandboxResultAuthorizer>();
                    var evidence = services.GetService<ISandboxEvidenceRecorder>();
                    if (policy is null || securityReader is null || candidateReader is null || runReader is null ||
                        registry is null || assurance is null || runtime is null || authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed Sandbox is not operationally ready.");
                    var request = new GovernedSandboxExecutionRequest(
                        input.ExecutionId, securityValidationId, generationId, input.DeliveryRunId,
                        input.ExpectedCandidateSha256Digest, input.ExpectedSecurityReportSha256Digest,
                        input.ExpectedSecurityEvidenceReference, input.SandboxImage, input.IsolationPolicy,
                        input.NonSecretEnvironmentReferences, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.ExecuteAsync(request, policy, securityReader, candidateReader,
                        runReader, registry, assurance, runtime, authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Sandbox denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Sandbox prerequisite was not found."); }
                catch (SandboxDependencyUnavailableException) { return Results.Problem(statusCode: 503, title: "A Sandbox dependency is unavailable."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Sandbox request or boundary result is invalid."); }
            })
            .WithName("RunGovernedSandbox").WithTags("Create Internal Service")
            .WithSummary("Execute an authoritative code candidate in the governed security sandbox.")
            .WithDescription("Verified OPA authorizes the exact immutable Security evidence chain, institutional image, Firecracker-class isolation, environment, and network scope before a signed provider-neutral sovereign Sandbox invocation. Repository defaults contain no runtime, image, limits, keys, endpoint, or institutional data. No production effect, Tests approval, or workflow advancement is available.")
            .Accepts<SandboxExecutionInput>("application/json")
            .Produces<GovernedSandboxExecutionReceipt>(200).Produces<GovernedSandboxExecutionReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceTests(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages/{packageSelectionId:guid}/ai-planning/{planningId:guid}/code-generation/{generationId:guid}/static-validation/{staticValidationId:guid}/security-validation/{securityValidationId:guid}/sandbox/{sandboxExecutionId:guid}/tests",
            async (Guid registrationId, Guid contextDiscoveryId, Guid systemsDiscoveryId, Guid architectureDiscoveryId,
                Guid packageSelectionId, Guid planningId, Guid generationId, Guid staticValidationId,
                Guid securityValidationId, Guid sandboxExecutionId, TestsExecutionInput input,
                HttpContext httpContext, IServiceProvider services, GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator, GovernedTestsExecutionEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.tests.execute",
                        sandboxExecutionId.ToString("D"), context.Identity.TenantId,
                        input.MaximumClassification, [], ["developer.internal-service.tests.execute"],
                        context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<ITestsPolicyGate>();
                    var sandboxReader = services.GetService<ITestsSandboxExecutionReceiptReader>();
                    var securityReader = services.GetService<ITestsSecurityValidationReceiptReader>();
                    var candidateReader = services.GetService<ITestsCodeGenerationCandidateReader>();
                    var runReader = services.GetService<ITestsDeliveryRunReader>();
                    var manifestReader = services.GetService<IGovernedTestManifestReader>();
                    var registry = services.GetService<IInstitutionalPackageRegistryReader>();
                    var assurance = services.GetService<IApprovedPackageSupplyChainVerifier>();
                    var runtime = services.GetService<IGovernedTestRuntime>();
                    var authorizer = services.GetService<ITestsResultAuthorizer>();
                    var evidence = services.GetService<ITestsEvidenceRecorder>();
                    if (policy is null || sandboxReader is null || securityReader is null || candidateReader is null ||
                        runReader is null || manifestReader is null || registry is null || assurance is null ||
                        runtime is null || authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed Tests are not operationally ready.");
                    var request = new GovernedTestsExecutionRequest(
                        input.ExecutionId, sandboxExecutionId, securityValidationId, generationId,
                        input.DeliveryRunId, input.ExpectedCandidateSha256Digest,
                        input.ExpectedSecurityReportSha256Digest, input.ExpectedSandboxResultSha256Digest,
                        input.ExpectedSandboxEvidenceReference, input.TestManifestReference,
                        input.ExpectedTestManifestSha256Digest, input.RequiredTestIds,
                        input.AllowedTestCategories, input.TestImage, input.IsolationPolicy,
                        input.NonSecretEnvironmentReferences, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.ExecuteAsync(request, policy, sandboxReader, securityReader,
                        candidateReader, runReader, manifestReader, registry, assurance, runtime,
                        authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Tests denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Tests prerequisite was not found."); }
                catch (TestsDependencyUnavailableException) { return Results.Problem(statusCode: 503, title: "A Tests dependency is unavailable."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Tests request or boundary result is invalid."); }
            })
            .WithName("RunGovernedTests").WithTags("Create Internal Service")
            .WithSummary("Run deterministic governed tests after accepted Sandbox execution.")
            .WithDescription("Verified OPA authorizes the exact immutable Sandbox chain, governed manifest, institutional image, Firecracker-class isolation, environment, and network scope before a bounded signed sovereign test invocation. No production effect, Human Review approval, or workflow advancement is available.")
            .Accepts<TestsExecutionInput>("application/json")
            .Produces<GovernedTestsExecutionReceipt>(200).Produces<GovernedTestsExecutionReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceHumanReview(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages/{packageSelectionId:guid}/ai-planning/{planningId:guid}/code-generation/{generationId:guid}/static-validation/{staticValidationId:guid}/security-validation/{securityValidationId:guid}/sandbox/{sandboxExecutionId:guid}/tests/{testsExecutionId:guid}/human-review",
            async (Guid registrationId, Guid contextDiscoveryId, Guid systemsDiscoveryId, Guid architectureDiscoveryId,
                Guid packageSelectionId, Guid planningId, Guid generationId, Guid staticValidationId,
                Guid securityValidationId, Guid sandboxExecutionId, Guid testsExecutionId,
                HumanReviewInput input, HttpContext httpContext, IServiceProvider services,
                GovernedRequestContextFactory contextFactory, IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedHumanReviewEngine engine, CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.human-review.decide",
                        testsExecutionId.ToString("D"), context.Identity.TenantId,
                        input.MaximumClassification, [], ["developer.internal-service.human-review.decide"],
                        input.InitiatorSubjectId, true));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<IHumanReviewPolicyGate>();
                    var testsReader = services.GetService<IHumanReviewTestsReceiptReader>();
                    var sandboxReader = services.GetService<IHumanReviewSandboxReceiptReader>();
                    var securityReader = services.GetService<IHumanReviewSecurityReceiptReader>();
                    var candidateReader = services.GetService<IHumanReviewCandidateReader>();
                    var runReader = services.GetService<IHumanReviewDeliveryRunReader>();
                    var attestation = services.GetService<IHumanReviewAttestationVerifier>();
                    var repository = services.GetService<IAtomicHumanReviewRepository>();
                    if (policy is null || testsReader is null || sandboxReader is null || securityReader is null ||
                        candidateReader is null || runReader is null || attestation is null || repository is null)
                        return Results.Problem(statusCode: 503, title: "Governed Human Review is not operationally ready.");
                    var request = new GovernedHumanReviewRequest(
                        input.ReviewId, testsExecutionId, sandboxExecutionId, securityValidationId,
                        generationId, input.DeliveryRunId, input.InitiatorSubjectId, input.ExpectedCandidateSha256Digest,
                        input.ExpectedSecurityReportSha256Digest, input.ExpectedSandboxResultSha256Digest,
                        input.ExpectedTestsResultSha256Digest, input.ExpectedTestsEvidenceReference,
                        input.Decision, input.Rationale, input.HumanAttestationReference,
                        input.DeclaredConflictingSubjectIds, input.ExpectedVersion, context.Identity,
                        input.Purpose, input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.ReviewAsync(request, policy, testsReader, sandboxReader,
                        securityReader, candidateReader, runReader, attestation, repository, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsRecorded
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Human Review denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Human Review prerequisite was not found."); }
                catch (HumanReviewDependencyUnavailableException) { return Results.Problem(statusCode: 503, title: "A Human Review dependency is unavailable."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Human Review request or boundary result is invalid."); }
            })
            .WithName("RecordGovernedHumanReview").WithTags("Create Internal Service")
            .WithSummary("Record an attested separation-of-duties Human Review decision after accepted Tests.")
            .WithDescription("OPA authorizes the exact human reviewer, decision, evidence package, and conflict scope before reads. The signed human decision is recorded atomically with evidence and cannot advance to Git or production.")
            .Accepts<HumanReviewInput>("application/json")
            .Produces<GovernedHumanReviewReceipt>(200).Produces<GovernedHumanReviewReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceGit(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/intents/{registrationId:guid}/enterprise-context/{contextDiscoveryId:guid}/existing-systems/{systemsDiscoveryId:guid}/existing-architecture/{architectureDiscoveryId:guid}/approved-packages/{packageSelectionId:guid}/ai-planning/{planningId:guid}/code-generation/{generationId:guid}/static-validation/{staticValidationId:guid}/security-validation/{securityValidationId:guid}/sandbox/{sandboxExecutionId:guid}/tests/{testsExecutionId:guid}/human-review/{reviewId:guid}/git",
            async (Guid registrationId, Guid contextDiscoveryId, Guid systemsDiscoveryId, Guid architectureDiscoveryId,
                Guid packageSelectionId, Guid planningId, Guid generationId, Guid staticValidationId,
                Guid securityValidationId, Guid sandboxExecutionId, Guid testsExecutionId, Guid reviewId,
                GitSourceCommitInput input, HttpContext httpContext, IServiceProvider services,
                GovernedRequestContextFactory contextFactory, IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedGitSourceCommitEngine engine, CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.git.commit",
                        input.RepositoryId, context.Identity.TenantId, input.MaximumClassification,
                        [], ["developer.internal-service.git.commit"], context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<IGitPolicyGate>();
                    var reviewReader = services.GetService<IGitHumanReviewReceiptReader>();
                    var testsReader = services.GetService<IGitTestsReceiptReader>();
                    var candidateReader = services.GetService<IGitCandidateReader>();
                    var runReader = services.GetService<IGitDeliveryRunReader>();
                    var materializer = services.GetService<IGovernedGitChangeSetMaterializer>();
                    var changeValidator = services.GetService<IGitChangePolicyValidator>();
                    var gitGateway = services.GetService<IInstitutionalGitGateway>();
                    var authorizer = services.GetService<IGitResultAuthorizer>();
                    var evidence = services.GetService<IGitEvidenceRecorder>();
                    if (policy is null || reviewReader is null || testsReader is null || candidateReader is null ||
                        runReader is null || materializer is null || changeValidator is null || gitGateway is null ||
                        authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed Git is not operationally ready.");
                    var request = new GovernedGitSourceCommitRequest(
                        input.OperationId, reviewId, testsExecutionId, generationId, input.DeliveryRunId,
                        input.ExpectedCandidateSha256Digest, input.ExpectedReviewPackageSha256Digest,
                        input.ExpectedReviewEvidenceReference, input.ExpectedTestsResultSha256Digest,
                        input.ExpectedChangeSetSha256Digest, input.AuthorizedRelativePaths,
                        input.RepositoryId, input.ExpectedBaseCommitId, input.ChangeBranch,
                        input.CommitMessage, input.SigningPolicyReference, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.CommitAsync(request, policy, reviewReader, testsReader,
                        candidateReader, runReader, materializer, changeValidator, gitGateway,
                        authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsCommitted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Git denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Git prerequisite was not found."); }
                catch (GitDependencyUnavailableException) { return Results.Problem(statusCode: 503, title: "A Git dependency is unavailable."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Git request or boundary result is invalid."); }
            })
            .WithName("CreateGovernedGitCommit").WithTags("Create Internal Service")
            .WithSummary("Create one governed signed source commit after approving Human Review.")
            .WithDescription("OPA authorizes the exact candidate, review, change set, repository, base commit, non-protected branch, and metadata before repository access or source mutation. No force update, PR, CI/CD, workflow advancement, or production effect is available.")
            .Accepts<GitSourceCommitInput>("application/json")
            .Produces<GovernedGitSourceCommitReceipt>(200).Produces<GovernedGitSourceCommitReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceCiCd(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/git/{gitOperationId:guid}/cicd",
            async (Guid gitOperationId, CiCdExecutionInput input, HttpContext httpContext, IServiceProvider services,
                GovernedRequestContextFactory contextFactory, IAccessPolicyEvaluator accessPolicyEvaluator,
                GovernedCiCdExecutionEngine engine, CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.cicd.execute",
                        input.RepositoryId, context.Identity.TenantId, input.MaximumClassification,
                        [], ["developer.internal-service.cicd.execute"], context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<ICiCdPolicyGate>();
                    var gitReader = services.GetService<IAuthorizedGitSourceCommitReceiptReader>();
                    var runReader = services.GetService<ICiCdDeliveryRunReader>();
                    var workflowReader = services.GetService<IGovernedCiCdWorkflowDefinitionReader>();
                    var workflowValidator = services.GetService<ICiCdWorkflowValidator>();
                    var gateway = services.GetService<IInstitutionalCiCdGateway>();
                    var authorizer = services.GetService<ICiCdResultAuthorizer>();
                    var evidence = services.GetService<ICiCdEvidenceRecorder>();
                    if (policy is null || gitReader is null || runReader is null || workflowReader is null ||
                        workflowValidator is null || gateway is null || authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed CI/CD is not operationally ready.");
                    var request = new GovernedCiCdExecutionRequest(
                        input.ExecutionId, gitOperationId, input.DeliveryRunId, input.RepositoryId,
                        input.ExpectedCommitId, input.ExpectedTreeSha256Digest,
                        input.ExpectedChangeSetSha256Digest, input.WorkflowDefinitionId,
                        input.WorkflowDefinitionVersion, input.ExpectedWorkflowSha256Digest,
                        input.WorkflowSignatureReference, input.PipelineProfile, input.RunnerPoolId,
                        input.RequiredStageIds, input.RequiredControlIds, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.ExecuteAsync(
                        request, policy, gitReader, runReader, workflowReader, workflowValidator,
                        gateway, authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed CI/CD denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "CI/CD prerequisite was not found."); }
                catch (CiCdDependencyUnavailableException) { return Results.Problem(statusCode: 503, title: "A CI/CD dependency is unavailable."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "CI/CD request or boundary result is invalid."); }
            })
            .WithName("ExecuteGovernedCiCd").WithTags("Create Internal Service")
            .WithSummary("Execute one governed CI/CD workflow for the exact signed Git commit.")
            .WithDescription("OPA authorizes the exact Git receipt, immutable workflow, isolated runner, stages, and controls before checkout or invocation. No source mutation, artifact publication, deployment, production effect, or workflow advancement is available.")
            .Accepts<CiCdExecutionInput>("application/json")
            .Produces<GovernedCiCdExecutionReceipt>(200).Produces<GovernedCiCdExecutionReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceArtifact(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/cicd/{ciCdExecutionId:guid}/artifact",
            async (Guid ciCdExecutionId, ArtifactPublicationInput input, HttpContext httpContext,
                IServiceProvider services, GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator, GovernedArtifactPublicationEngine engine,
                SupplyChainVerificationPipeline supplyChainPipeline, CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.artifact.publish",
                        input.RegistryRepository, context.Identity.TenantId, input.MaximumClassification,
                        [], ["developer.internal-service.artifact.publish"], context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<IArtifactPolicyGate>();
                    var ciCdReader = services.GetService<IAuthorizedCiCdExecutionReceiptReader>();
                    var manifestReader = services.GetService<IAuthorizedPipelineOutputManifestReader>();
                    var runReader = services.GetService<IArtifactDeliveryRunReader>();
                    var packageValidator = services.GetService<IArtifactPackageValidator>();
                    var registryGateway = services.GetService<IInstitutionalArtifactRegistryGateway>();
                    var authorizer = services.GetService<IArtifactResultAuthorizer>();
                    var evidence = services.GetService<IArtifactEvidenceRecorder>();
                    if (policy is null || ciCdReader is null || manifestReader is null || runReader is null ||
                        packageValidator is null || registryGateway is null || authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed Artifact publication is not operationally ready.");
                    var request = new GovernedArtifactPublicationRequest(
                        input.PublicationId, ciCdExecutionId, input.DeliveryRunId,
                        input.ExpectedPipelineManifestSha256Digest, input.ExpectedSourceCommitId,
                        input.ExpectedWorkflowSha256Digest, input.Coordinate,
                        input.ExpectedContentSha256Digest, input.RegistryId, input.RegistryRepository,
                        input.SigningPolicyReference, input.RequiredControls, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.PublishAsync(
                        request, policy, ciCdReader, manifestReader, runReader, packageValidator,
                        registryGateway, supplyChainPipeline, authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Artifact publication denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Artifact prerequisite was not found."); }
                catch (ArtifactDependencyUnavailableException) { return Results.Problem(statusCode: 503, title: "An Artifact dependency is unavailable."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Artifact request or boundary result is invalid."); }
            })
            .WithName("PublishGovernedArtifact").WithTags("Create Internal Service")
            .WithSummary("Publish one exact immutable Artifact from an accepted CI/CD output.")
            .WithDescription("OPA authorizes the exact pipeline output, coordinate, registry, signing policy, and supply-chain controls before reads or publication. No deployment, production effect, or workflow advancement is available.")
            .Accepts<ArtifactPublicationInput>("application/json")
            .Produces<GovernedArtifactPublicationReceipt>(200).Produces<GovernedArtifactPublicationReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceDeployment(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/artifacts/{artifactPublicationId:guid}/deployment",
            async (Guid artifactPublicationId, SovereignDeploymentInput input, HttpContext httpContext,
                IServiceProvider services, GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator, GovernedSovereignDeploymentEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.deployment.execute",
                        input.TargetEnvironment, context.Identity.TenantId, input.MaximumClassification,
                        [], ["operator.internal-service.deployment.execute"], context.Identity.SubjectId, true));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<IDeploymentPolicyGate>();
                    var artifactReceiptReader = services.GetService<IAuthorizedArtifactPublicationReceiptReader>();
                    var artifactReader = services.GetService<IAuthorizedDeploymentArtifactReader>();
                    var runReader = services.GetService<IDeploymentDeliveryRunReader>();
                    var profileReader = services.GetService<IGovernedSovereignDeploymentProfileReader>();
                    var preflight = services.GetService<IInstitutionalDeploymentPreflightValidator>();
                    var gateway = services.GetService<IInstitutionalSovereignDeploymentGateway>();
                    var authorizer = services.GetService<IDeploymentResultAuthorizer>();
                    var evidence = services.GetService<IDeploymentEvidenceRecorder>();
                    if (policy is null || artifactReceiptReader is null || artifactReader is null || runReader is null ||
                        profileReader is null || preflight is null || gateway is null || authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed Deployment is not operationally ready.");
                    var request = new GovernedSovereignDeploymentRequest(
                        input.DeploymentId, artifactPublicationId, input.DeliveryRunId,
                        input.ExpectedArtifactContentSha256Digest, input.ExpectedImmutableRegistryReference,
                        input.ExpectedArtifactEvidenceReference, input.DeploymentProfileId,
                        input.DeploymentProfileVersion, input.ExpectedDeploymentProfileSha256Digest,
                        input.TargetEnvironment, input.ProductionDeploymentRequested,
                        input.HumanApprovalReference, input.WorkloadIdentityReference,
                        input.SecretsPolicyReference, input.RollbackPolicyReference,
                        context.Identity, input.Purpose, input.MaximumClassification,
                        context.AuthorizationEvidenceReference, input.Environment,
                        input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.DeployAsync(
                        request, policy, artifactReceiptReader, artifactReader, runReader,
                        profileReader, preflight, gateway, authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Deployment denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Deployment prerequisite was not found."); }
                catch (DeploymentDependencyUnavailableException) { return Results.Problem(statusCode: 503, title: "A Deployment dependency is unavailable."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Deployment request or boundary result is invalid."); }
            })
            .WithName("ExecuteGovernedSovereignDeployment").WithTags("Create Internal Service")
            .WithSummary("Deploy one exact verified Artifact to one approved sovereign environment.")
            .WithDescription("OPA authorizes the exact Artifact, sovereign profile, target, production intent, and human approval before reads or invocation. No telemetry, registration, Enterprise Model mutation, or workflow advancement is available.")
            .Accepts<SovereignDeploymentInput>("application/json")
            .Produces<GovernedSovereignDeploymentReceipt>(200).Produces<GovernedSovereignDeploymentReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceOpenTelemetry(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/deployments/{deploymentId:guid}/opentelemetry",
            async (Guid deploymentId, OpenTelemetryActivationInput input, HttpContext httpContext,
                IServiceProvider services, GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator, GovernedOpenTelemetryActivationEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.telemetry.activate",
                        input.ExpectedRuntimeIdentity, context.Identity.TenantId, input.MaximumClassification,
                        [], ["operator.internal-service.telemetry.activate"], context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<IOpenTelemetryPolicyGate>();
                    var deploymentReader = services.GetService<IAuthorizedSovereignDeploymentReceiptReader>();
                    var runReader = services.GetService<IOpenTelemetryDeliveryRunReader>();
                    var profileReader = services.GetService<IGovernedOpenTelemetryProfileReader>();
                    var redactionVerifier = services.GetService<IOpenTelemetryRedactionPolicyVerifier>();
                    var gateway = services.GetService<IInstitutionalOpenTelemetryGateway>();
                    var authorizer = services.GetService<IOpenTelemetryResultAuthorizer>();
                    var evidence = services.GetService<IOpenTelemetryEvidenceRecorder>();
                    if (policy is null || deploymentReader is null || runReader is null || profileReader is null ||
                        redactionVerifier is null || gateway is null || authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed OpenTelemetry is not operationally ready.");
                    var request = new GovernedOpenTelemetryActivationRequest(
                        input.ActivationId, deploymentId, input.DeliveryRunId,
                        input.ExpectedRuntimeIdentity, input.ExpectedArtifactContentSha256Digest,
                        input.ExpectedDeploymentEvidenceReference, input.ExpectedProductionEffectOccurred,
                        input.TelemetryProfileId, input.TelemetryProfileVersion,
                        input.ExpectedTelemetryProfileSha256Digest, input.ExpectedServiceName,
                        input.ExpectedServiceVersion, input.RequiredSignals,
                        input.ExpectedRedactionPolicyReference, input.ExpectedRedactionPolicySha256Digest,
                        context.Identity, input.Purpose, input.MaximumClassification,
                        context.AuthorizationEvidenceReference, input.Environment,
                        input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.ActivateAsync(
                        request, policy, deploymentReader, runReader, profileReader,
                        redactionVerifier, gateway, authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed OpenTelemetry denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "OpenTelemetry prerequisite was not found."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "OpenTelemetry request or boundary result is invalid."); }
            })
            .WithName("ActivateGovernedOpenTelemetry").WithTags("Create Internal Service")
            .WithSummary("Activate governed OpenTelemetry for one exact Deployment.")
            .WithDescription("OPA authorizes the exact Deployment, profile, resource, signals, collectors, and redaction before reads or configuration. No automatic registration, Enterprise Model mutation, or workflow advancement is available.")
            .Accepts<OpenTelemetryActivationInput>("application/json")
            .Produces<GovernedOpenTelemetryActivationReceipt>(200).Produces<GovernedOpenTelemetryActivationReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceAutomaticRegistration(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/opentelemetry/{activationId:guid}/automatic-registration",
            async (Guid activationId, AutomaticRegistrationInput input, HttpContext httpContext,
                IServiceProvider services, GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator, GovernedAutomaticRegistrationEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.registration.execute",
                        input.ExpectedServiceIdentity, context.Identity.TenantId, input.MaximumClassification,
                        [], ["operator.internal-service.registration.execute"], context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<IAutomaticRegistrationPolicyGate>();
                    var activationReader = services.GetService<IAuthorizedOpenTelemetryActivationReceiptReader>();
                    var runReader = services.GetService<IAutomaticRegistrationDeliveryRunReader>();
                    var manifestReader = services.GetService<IGovernedAutomaticRegistrationManifestReader>();
                    var repository = services.GetService<IAutomaticRegistrationRepository>();
                    var authorizer = services.GetService<IAutomaticRegistrationResultAuthorizer>();
                    var evidence = services.GetService<IAutomaticRegistrationEvidenceRecorder>();
                    if (policy is null || activationReader is null || runReader is null || manifestReader is null ||
                        repository is null || authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed Automatic Registration is not operationally ready.");
                    var request = new GovernedAutomaticRegistrationRequest(
                        input.RegistrationId, activationId, input.DeliveryRunId, input.ManifestId,
                        input.ManifestVersion, input.ExpectedManifestSha256Digest, input.ExpectedRuntimeIdentity,
                        input.ExpectedServiceIdentity, input.ExpectedServiceVersion, input.ExpectedArtifactDigest,
                        input.ExpectedOpenTelemetryEvidenceReference, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference, input.Environment,
                        input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.RegisterAsync(request, policy, activationReader, runReader,
                        manifestReader, repository, authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Automatic Registration denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Automatic Registration prerequisite was not found."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Automatic Registration request or boundary result is invalid."); }
            })
            .WithName("ExecuteGovernedAutomaticRegistration").WithTags("Create Internal Service")
            .WithSummary("Register one exact deployed and observed internal service.")
            .WithDescription("OPA authorizes the exact OpenTelemetry receipt, run, signed manifest, service key, policies, actions, relationships, and evidence before atomic registration. No workflow advancement or later station is available.")
            .Accepts<AutomaticRegistrationInput>("application/json")
            .Produces<GovernedAutomaticRegistrationReceipt>(200).Produces<GovernedAutomaticRegistrationReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceEnterpriseModel(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/registrations/{registrationId:guid}/enterprise-model",
            async (Guid registrationId, EnterpriseModelContextualizationInput input, HttpContext httpContext,
                IServiceProvider services, GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator, GovernedEnterpriseModelContextualizationEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.enterprise-model.contextualize",
                        input.ExpectedEnterpriseObjectId.ToString(), context.Identity.TenantId, input.MaximumClassification,
                        [], ["operator.internal-service.enterprise-model.contextualize"], context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<IEnterpriseModelContextPolicyGate>();
                    var registrationReader = services.GetService<IAuthorizedAutomaticRegistrationReceiptReader>();
                    var runReader = services.GetService<IEnterpriseModelDeliveryRunReader>();
                    var objectReader = services.GetService<IAuthorizedRegisteredEnterpriseObjectReader>();
                    var authorizer = services.GetService<IEnterpriseModelContextResultAuthorizer>();
                    var evidence = services.GetService<IEnterpriseModelContextEvidenceRecorder>();
                    if (policy is null || registrationReader is null || runReader is null || objectReader is null || authorizer is null || evidence is null)
                        return Results.Problem(statusCode: 503, title: "Governed Enterprise Model contextualization is not operationally ready.");
                    var request = new GovernedEnterpriseModelContextualizationRequest(
                        input.ContextualizationId, registrationId, input.DeliveryRunId,
                        new EnterpriseObjectId(input.ExpectedEnterpriseObjectId), input.ExpectedRequestFingerprint,
                        input.ExpectedRegistrationEvidenceReference, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.ContextualizeAsync(request, policy, registrationReader, runReader,
                        objectReader, authorizer, evidence, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Enterprise Model contextualization denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Enterprise Model prerequisite was not found."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Enterprise Model request or boundary result is invalid."); }
            })
            .WithName("ContextualizeGovernedEnterpriseModel").WithTags("Create Internal Service")
            .WithSummary("Confirm one exact registered service in authorized Enterprise Model context.")
            .WithDescription("OPA authorizes the exact registration, Enterprise Object, tenant, classification, and evidence before reads. No model mutation, analysis, simulation, workflow advancement, or Evidence completion is available.")
            .Accepts<EnterpriseModelContextualizationInput>("application/json")
            .Produces<GovernedEnterpriseModelContextualizationReceipt>(200).Produces<GovernedEnterpriseModelContextualizationReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(503)
            .RequireAuthorization();
    }

    internal static IEndpointConventionBuilder MapInternalServiceEvidenceCompletion(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/api/v1/internal-services/enterprise-model/{contextualizationId:guid}/evidence",
            async (Guid contextualizationId, EvidenceCompletionInput input, HttpContext httpContext,
                IServiceProvider services, GovernedRequestContextFactory contextFactory,
                IAccessPolicyEvaluator accessPolicyEvaluator, GovernedEvidenceCompletionEngine engine,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var context = contextFactory.Create(httpContext.User);
                    var access = accessPolicyEvaluator.Evaluate(new AccessRequest(
                        context.Identity, input.Purpose, "internal-service.evidence.complete",
                        input.ChainId.ToString(), context.Identity.TenantId, input.MaximumClassification,
                        [], ["operator.internal-service.evidence.complete", "evidence.append", "evidence.verify"],
                        context.Identity.SubjectId, false));
                    if (!access.IsAllowed) throw new UnauthorizedAccessException();
                    var policy = services.GetService<IEvidenceCompletionPolicyGate>();
                    var contextualizationReader = services.GetService<IAuthorizedEnterpriseModelContextualizationReceiptReader>();
                    var runReader = services.GetService<IEvidenceCompletionDeliveryRunReader>();
                    var store = services.GetService<IEvidenceChainStore>();
                    var evidenceAccess = services.GetService<IEvidenceAccessAuthorizer>();
                    var signer = services.GetService<IEvidenceSigner>();
                    var signatureVerifier = services.GetService<IEvidenceSignatureVerifier>();
                    var authorizer = services.GetService<IEvidenceCompletionResultAuthorizer>();
                    if (policy is null || contextualizationReader is null || runReader is null || store is null || evidenceAccess is null ||
                        signer is null || signatureVerifier is null || authorizer is null)
                        return Results.Problem(statusCode: 503, title: "Governed Evidence completion is not operationally ready.");
                    var request = new GovernedEvidenceCompletionRequest(
                        input.CompletionId, contextualizationId, input.DeliveryRunId, input.ChainId,
                        input.CorrelationId, input.ExpectedContextualizationEvidenceReference,
                        input.PayloadSha256Digest, input.TraceReferences, context.Identity, input.Purpose,
                        input.MaximumClassification, context.AuthorizationEvidenceReference,
                        input.Environment, input.PolicyBundle, DateTimeOffset.UtcNow);
                    var receipt = await engine.CompleteAsync(request, policy, contextualizationReader, runReader,
                        store, evidenceAccess, signer, signatureVerifier, authorizer, cancellationToken);
                    return receipt.PolicyOutcome == GovernedIntentPolicyOutcome.Permit && receipt.IsAccepted
                        ? Results.Ok(receipt) : Results.Json(receipt, statusCode: 403);
                }
                catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Governed Evidence completion denied."); }
                catch (KeyNotFoundException) { return Results.Problem(statusCode: 404, title: "Evidence completion prerequisite was not found."); }
                catch (System.Security.Cryptography.CryptographicException) { return Results.Problem(statusCode: 409, title: "Evidence chain cryptographic verification failed."); }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                { return Results.Problem(statusCode: 400, title: "Evidence completion request or boundary result is invalid."); }
            })
            .WithName("CompleteGovernedEvidence").WithTags("Create Internal Service")
            .WithSummary("Append and verify the final cryptographic Evidence entry.")
            .WithDescription("OPA and Evidence authorization precede access. The Phase 30 engine appends only the final Evidence entry and verifies the exact complete ten-stage chain. No later station exists.")
            .Accepts<EvidenceCompletionInput>("application/json")
            .Produces<GovernedEvidenceCompletionReceipt>(200).Produces<GovernedEvidenceCompletionReceipt>(403)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(404).ProducesProblem(409).ProducesProblem(503)
            .RequireAuthorization();
    }
}
