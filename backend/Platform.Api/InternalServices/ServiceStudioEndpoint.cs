using Platform.Identity.Access;
using Platform.Identity.Authentication;
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
}
