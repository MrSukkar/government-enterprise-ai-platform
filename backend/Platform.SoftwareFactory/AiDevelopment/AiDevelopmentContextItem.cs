using System.Collections.Immutable;
using Platform.Domain.Security;

namespace Platform.SoftwareFactory.AiDevelopment;

public sealed record AiDevelopmentContextItem(
    string Reference,
    string SourceStation,
    DataClassification Classification,
    string Sha256Digest,
    string Content,
    ImmutableArray<string> EvidenceReferences)
{
    public AiDevelopmentContextItem Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(SourceStation);
        ArgumentException.ThrowIfNullOrWhiteSpace(Content);
        if (!Enum.IsDefined(Classification) || Sha256Digest.Length != 64 ||
            Sha256Digest.Any(character => !Uri.IsHexDigit(character)) ||
            EvidenceReferences.IsDefaultOrEmpty || EvidenceReferences.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Authorized AI context material is invalid.");
        return this;
    }
}
