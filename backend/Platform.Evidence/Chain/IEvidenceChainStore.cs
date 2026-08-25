using System.Collections.Immutable;

namespace Platform.Evidence.Chain;

public interface IEvidenceChainStore
{
    Task<EvidenceEntry?> LoadHeadAsync(Guid chainId, string tenantId, CancellationToken cancellationToken);

    Task<EvidenceEntry> AppendAtomicallyAsync(
        EvidenceEntry entry,
        long expectedSequence,
        string expectedPreviousEntrySha256Digest,
        CancellationToken cancellationToken);

    Task<ImmutableArray<EvidenceEntry>> LoadOrderedAsync(
        Guid chainId,
        string tenantId,
        string maximumAuthorizedClassification,
        string authorizationEvidenceReference,
        CancellationToken cancellationToken);
}
