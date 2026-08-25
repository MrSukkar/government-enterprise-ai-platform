using System.Collections.Immutable;

namespace Platform.Evidence.Chain;

public sealed record EvidenceEntryProof(
    long Sequence,
    EvidenceStage Stage,
    string EntrySha256Digest,
    string SignatureAlgorithm,
    string KeyId,
    bool HashValid,
    bool SignatureValid);

public sealed record EvidenceProofReport(
    Guid ChainId,
    string TenantId,
    bool IsValid,
    bool IsComplete,
    string? RootSha256Digest,
    string? HeadSha256Digest,
    ImmutableArray<EvidenceEntryProof> EntryProofs,
    ImmutableArray<string> Failures,
    string AuthorizationEvidenceReference,
    DateTimeOffset VerifiedAt);
