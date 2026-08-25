using System.Collections.Immutable;

namespace Platform.SoftwareFactory.VerticalSlice;

public sealed record InternalServiceVerticalSliceRequest(
    Guid RunId,
    string TenantId,
    string DeveloperSubjectId,
    ImmutableHashSet<string> Permissions,
    string ServiceName,
    string Intent,
    ImmutableArray<string> EnterpriseContextReferences,
    ImmutableArray<string> ExistingSystemReferences,
    string ExistingArchitectureReference,
    ImmutableArray<string> ApprovedPackageReferences,
    ImmutableArray<string> IntentEvidenceReferences,
    DateTimeOffset RequestedAt)
{
    public InternalServiceVerticalSliceRequest Validate()
    {
        if (RunId == Guid.Empty) throw new InvalidOperationException("Vertical slice run identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(DeveloperSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(Intent);
        ArgumentException.ThrowIfNullOrWhiteSpace(ExistingArchitectureReference);
        ArgumentNullException.ThrowIfNull(Permissions);
        if (!Permissions.Contains("developer.internal-service.create"))
            throw new UnauthorizedAccessException("The developer.internal-service.create permission is required.");
        if (EnterpriseContextReferences.IsDefaultOrEmpty || ExistingSystemReferences.IsDefault ||
            ApprovedPackageReferences.IsDefaultOrEmpty || IntentEvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Vertical slice context, packages, and intent evidence are required.");
        foreach (var value in EnterpriseContextReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in ExistingSystemReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in ApprovedPackageReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        foreach (var value in IntentEvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (RequestedAt == default) throw new InvalidOperationException("Vertical slice request time is required.");
        return this;
    }
}
