using System.Collections.Immutable;

namespace Platform.Evidence.Chain;

internal static class EvidenceValidation
{
    public const string GenesisSha256Digest = "0000000000000000000000000000000000000000000000000000000000000000";

    public static void RequireSha256(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException($"{name} must be a SHA-256 digest.");
    }

    public static void RequireReferences(ImmutableArray<string> references, string name)
    {
        if (references.IsDefaultOrEmpty) throw new InvalidOperationException($"{name} are required.");
        foreach (var value in references) ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }
}
