using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.Identity.Authentication;
using Platform.Knowledge.Retrieval;
using Platform.Integrations.ExistingSystems;
using Platform.Integrations.ExistingArchitecture;
using Platform.SoftwareFactory.InternalService;
using Platform.SoftwareFactory.Packages;
using Platform.SoftwareFactory.AiDevelopment;

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
            .WithDescription("OPA, verified prompt, re-authorized context, exact approved packages, independent evaluation, result authorization, and evidence are mandatory. No generated files, tools, workflow advancement, or Code Generation is available.")
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
                    var runtime = services.GetService<IAiDevelopmentRuntime>();
                    var evaluator = services.GetService<IAiOutputEvaluator>();
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
            .WithDescription("OPA, exact AI Planning evidence, verified prompt, re-authorized context, safe relative paths, independent evaluation, result authorization, and evidence are mandatory. No filesystem access, execution, workflow advancement, or Static Validation is available.")
            .Accepts<CodeGenerationInput>("application/json")
            .Produces<GovernedCodeGenerationReceipt>(StatusCodes.Status200OK)
            .Produces<GovernedCodeGenerationReceipt>(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();
    }
}
