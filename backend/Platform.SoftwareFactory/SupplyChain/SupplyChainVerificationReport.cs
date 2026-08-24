using System.Collections.Immutable;

namespace Platform.SoftwareFactory.SupplyChain;

public sealed record SupplyChainVerificationReport(
    ArtifactSupplyChainRecord Artifact,
    ImmutableArray<SupplyChainVerification> Verifications)
{
    private static readonly ImmutableHashSet<SupplyChainControl> RequiredControls =
        Enum.GetValues<SupplyChainControl>().ToImmutableHashSet();

    public bool IsVerified =>
        Verifications.Select(item => item.Control).ToImmutableHashSet().IsSupersetOf(RequiredControls) &&
        Verifications.All(item => item.Passed &&
            !string.IsNullOrWhiteSpace(item.VerifierIdentity) &&
            !string.IsNullOrWhiteSpace(item.EvidenceReference));
}
