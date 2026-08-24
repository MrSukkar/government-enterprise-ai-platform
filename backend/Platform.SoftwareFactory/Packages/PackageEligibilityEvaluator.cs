namespace Platform.SoftwareFactory.Packages;

public sealed class PackageEligibilityEvaluator : IPackageEligibilityEvaluator
{
    public PackageUseDecision Evaluate(InstitutionalPackage package, PackageUseRequest request)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(request);
        package.Validate();
        request.Coordinate.Validate();

        if (package.Coordinate != request.Coordinate)
            return PackageUseDecision.Deny("coordinate_mismatch", "Only the exact approved version and digest may be used.");
        if (!package.AllowedTenantIds.Contains(request.TenantId))
            return PackageUseDecision.Deny("tenant_denied", "The package is not approved for this tenant.");
        if (!package.AllowedEnvironments.Contains(request.Environment))
            return PackageUseDecision.Deny("environment_denied", "The package is not approved for this environment.");
        if (!package.AvailableInSovereignRegistry)
            return PackageUseDecision.Deny("sovereign_copy_required", "An approved sovereign registry copy is required.");

        var approval = package.CurrentApproval;
        if (approval is null || approval.Status != PackageApprovalStatus.Approved)
            return PackageUseDecision.Deny("approval_required", "The package is not currently approved.");
        if (approval.ExpiresAt is not null && approval.ExpiresAt <= request.RequestedAt)
            return PackageUseDecision.Deny("approval_expired", "The package approval has expired.");

        return PackageUseDecision.Allow();
    }
}
