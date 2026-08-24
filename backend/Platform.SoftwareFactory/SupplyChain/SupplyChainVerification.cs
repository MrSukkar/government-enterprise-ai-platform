namespace Platform.SoftwareFactory.SupplyChain;

public sealed record SupplyChainVerification(
    SupplyChainControl Control,
    bool Passed,
    string VerifierIdentity,
    string EvidenceReference,
    string Reason);
