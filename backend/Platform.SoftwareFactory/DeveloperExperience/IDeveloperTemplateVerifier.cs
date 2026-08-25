namespace Platform.SoftwareFactory.DeveloperExperience;

public interface IDeveloperTemplateVerifier
{
    Task<DeveloperTemplateVerification> VerifyAsync(
        ApprovedDeveloperTemplate template,
        CancellationToken cancellationToken);
}
