using Platform.Identity.Access;
using Platform.Identity.Authentication;
using Platform.SoftwareFactory.InternalService;

namespace Platform.Api.InternalService;

internal static class InternalServiceEndpoint
{
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
}
