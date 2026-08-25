namespace Platform.Governance.Policies;

public interface IPolicyBundleVerifier
{
    Task<PolicyBundleVerification> VerifyAsync(SignedPolicyBundleReference policyBundle, CancellationToken cancellationToken);
}
