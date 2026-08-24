namespace Platform.SoftwareFactory.SupplyChain;

public interface ISupplyChainControlVerifier
{
    SupplyChainControl Control { get; }

    Task<SupplyChainVerification> VerifyAsync(
        ArtifactSupplyChainRecord artifact,
        CancellationToken cancellationToken);
}
