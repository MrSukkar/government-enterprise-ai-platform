using Platform.Domain.Security;
using Platform.Identity.Access;
using Platform.Identity.Authentication;
using Platform.Knowledge.Retrieval;
using Platform.SoftwareFactory.InternalService;

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
}
