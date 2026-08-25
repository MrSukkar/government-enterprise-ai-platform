using System.Collections.Immutable;

namespace Platform.Infrastructure.Productization;

public sealed class GovernmentProductizationService(
    IProductManifestVerifier manifestVerifier,
    IGovernmentProductRegistry productRegistry)
{
    public async Task<GovernmentProductPackage> PublishAsync(
        GovernmentProductizationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        ValidateComplianceCoverage(request);
        if (request.Manifest.Artifacts.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() !=
            request.Manifest.Artifacts.Length)
            throw new InvalidOperationException("Government product artifact names must be unique.");

        var verification = await manifestVerifier.VerifyAsync(
            request.Manifest, request.Jurisdiction.TrustBundleReference, cancellationToken);
        ValidateVerification(request, verification);
        var controls = request.Manifest.ComplianceControls
            .Select(item => item.ControlId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var evidence = request.Manifest.ReleaseEvidenceReferences
            .Add(request.ApprovalEvidenceReference)
            .Add(verification.VerificationEvidenceReference)
            .AddRange(request.Manifest.Artifacts.Select(item => item.SupplyChainVerificationEvidenceReference))
            .AddRange(request.Manifest.ComplianceControls.SelectMany(item => item.EvidenceReferences))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        var package = new GovernmentProductPackage(
            request.RequestId, request.Manifest.ProductId, request.Manifest.ProductVersion,
            request.TenantId, request.Jurisdiction.JurisdictionCode, request.DeploymentProfile.Topology,
            request.Manifest.ManifestSha256Digest, request.Jurisdiction.PolicyAuthorityReference,
            request.Jurisdiction.IdentityAuthorityReference, controls, evidence, verification.VerifiedAt);
        var registered = await productRegistry.RegisterAtomicallyAsync(package, cancellationToken);
        ValidateRegistered(package, registered);
        return registered;
    }

    private static void ValidateComplianceCoverage(GovernmentProductizationRequest request)
    {
        var mappings = request.Manifest.ComplianceControls;
        if (mappings.Select(item => item.ControlId).Distinct(StringComparer.Ordinal).Count() != mappings.Length)
            throw new InvalidOperationException("Compliance controls must have exactly one mapping.");
        var mapped = mappings.Select(item => item.ControlId).ToHashSet(StringComparer.Ordinal);
        if (!request.Jurisdiction.RequiredComplianceControls.IsSubsetOf(mapped))
            throw new InvalidOperationException("Government product does not cover every required jurisdiction control.");
    }

    private static void ValidateVerification(
        GovernmentProductizationRequest request,
        ProductManifestVerification verification)
    {
        ArgumentNullException.ThrowIfNull(verification);
        if (!verification.SignatureValid || verification.ProductId != request.Manifest.ProductId ||
            !StringComparer.Ordinal.Equals(verification.ProductVersion, request.Manifest.ProductVersion) ||
            !StringComparer.OrdinalIgnoreCase.Equals(verification.ManifestSha256Digest, request.Manifest.ManifestSha256Digest) ||
            !StringComparer.Ordinal.Equals(verification.TrustBundleReference, request.Jurisdiction.TrustBundleReference))
            throw new UnauthorizedAccessException("Government product signature verification failed closed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(verification.VerificationEvidenceReference);
        if (verification.VerifiedAt < request.Manifest.ReleasedAt)
            throw new InvalidOperationException("Manifest verification predates the product release.");
    }

    private static void ValidateRegistered(GovernmentProductPackage expected, GovernmentProductPackage registered)
    {
        ArgumentNullException.ThrowIfNull(registered);
        if (registered.RequestId != expected.RequestId || registered.ProductId != expected.ProductId ||
            !StringComparer.Ordinal.Equals(registered.ProductVersion, expected.ProductVersion) ||
            !StringComparer.Ordinal.Equals(registered.TenantId, expected.TenantId) ||
            !StringComparer.Ordinal.Equals(registered.JurisdictionCode, expected.JurisdictionCode) ||
            registered.Topology != expected.Topology ||
            !StringComparer.OrdinalIgnoreCase.Equals(registered.ManifestSha256Digest, expected.ManifestSha256Digest) ||
            !StringComparer.Ordinal.Equals(registered.PolicyAuthorityReference, expected.PolicyAuthorityReference) ||
            !StringComparer.Ordinal.Equals(registered.IdentityAuthorityReference, expected.IdentityAuthorityReference) ||
            !registered.ComplianceControlIds.SequenceEqual(expected.ComplianceControlIds, StringComparer.Ordinal) ||
            !registered.EvidenceReferences.ToHashSet(StringComparer.Ordinal).SetEquals(expected.EvidenceReferences) ||
            registered.RegisteredAt != expected.RegisteredAt)
            throw new InvalidOperationException("Government product registry changed governed package state.");
    }
}
