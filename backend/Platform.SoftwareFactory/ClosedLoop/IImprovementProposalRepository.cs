namespace Platform.SoftwareFactory.ClosedLoop;

public interface IImprovementProposalRepository
{
    Task<ImprovementProposal> CreateAtomicallyAsync(
        ImprovementProposal proposal,
        CancellationToken cancellationToken);
}
