namespace Platform.Governance.Evidence;

public interface IGovernanceEvidenceJournal
{
    Task AppendAsync(GovernanceEvidenceRecord record, CancellationToken cancellationToken);
}
