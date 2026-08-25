namespace Platform.EnterpriseModel.Registration;

public sealed record AutomaticRegistrationKey(
    string TenantId,
    string EnvironmentName,
    string ServiceIdentity)
{
    public AutomaticRegistrationKey Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(EnvironmentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceIdentity);
        return this;
    }
}
