using System.Collections.Immutable;

namespace Platform.SoftwareFactory.Packages;

public sealed record InstitutionalPackage
{
    public required PackageCoordinate Coordinate { get; init; }
    public required PackageProvenance Provenance { get; init; }
    public required string LicenseExpression { get; init; }
    public required ImmutableHashSet<string> AllowedTenantIds { get; init; }
    public required ImmutableHashSet<string> AllowedEnvironments { get; init; }
    public required ImmutableArray<PackageApproval> ApprovalHistory { get; init; }
    public string? SbomReference { get; init; }
    public string? SignatureReference { get; init; }
    public bool AvailableInSovereignRegistry { get; init; }

    public PackageApproval? CurrentApproval => ApprovalHistory
        .OrderByDescending(approval => approval.DecidedAt)
        .FirstOrDefault();

    public InstitutionalPackage Validate()
    {
        Coordinate.Validate();
        Provenance.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(LicenseExpression);
        if (AllowedTenantIds.IsEmpty) throw new InvalidOperationException("At least one tenant scope is required.");
        if (AllowedEnvironments.IsEmpty) throw new InvalidOperationException("At least one environment scope is required.");
        foreach (var approval in ApprovalHistory) approval.Validate();
        return this;
    }
}
