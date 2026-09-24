namespace Platform.Integrations.ApiCatalog;

public interface IApiCatalogPolicyAuthorizer
{
    Task<ApiCatalogPolicyDecision> AuthorizePublicationAsync(ApiCatalogPublicationRequest request, OpenApi31ValidationReport report, CancellationToken cancellationToken);
}

public interface IApiCatalogRepository
{
    Task<ApiCatalogCommit> PublishAtomicallyAsync(ApiCatalogPublicationRequest request, OpenApi31ValidationReport report, ApiCatalogPolicyDecision decision, CancellationToken cancellationToken);
}

public interface IApiCatalogResultAuthorizer
{
    Task<ApiCatalogReleaseDecision> AuthorizeReleaseAsync(ApiCatalogCommit commit, CancellationToken cancellationToken);
}

public sealed class GovernedApiCatalog(
    IOpenApi31ContractValidator validator,
    IApiCatalogPolicyAuthorizer policyAuthorizer,
    IApiCatalogRepository repository,
    IApiCatalogResultAuthorizer resultAuthorizer)
{
    public async Task<ApiCatalogCommit> PublishAsync(ApiCatalogPublicationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var report = validator.Validate(request.OpenApiDocument).RequireAccepted();
        var policy = await policyAuthorizer.AuthorizePublicationAsync(request, report, cancellationToken);
        policy.RequireExact(request, report);
        var commit = await repository.PublishAtomicallyAsync(request, report, policy, cancellationToken);
        commit.ValidateFor(request, report);
        var release = await resultAuthorizer.AuthorizeReleaseAsync(commit, cancellationToken);
        release.RequireExact(commit);
        return commit;
    }
}
