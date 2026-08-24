namespace Platform.SoftwareFactory.Packages;

public sealed record PackageUseDecision(bool IsAllowed, string Code, string Reason)
{
    public static PackageUseDecision Allow() => new(true, "package_approved", "The exact package is approved for this governed scope.");
    public static PackageUseDecision Deny(string code, string reason) => new(false, code, reason);
}
