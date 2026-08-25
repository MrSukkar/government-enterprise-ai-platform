namespace Platform.Infrastructure.Productization;

public interface IProductManifestVerifier
{
    Task<ProductManifestVerification> VerifyAsync(
        GovernmentProductManifest manifest,
        string trustBundleReference,
        CancellationToken cancellationToken);
}
