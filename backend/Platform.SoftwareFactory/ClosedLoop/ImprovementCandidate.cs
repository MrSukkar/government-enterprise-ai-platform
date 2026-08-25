using System.Collections.Immutable;

namespace Platform.SoftwareFactory.ClosedLoop;

public enum ImprovementKind { Reliability = 0, Security = 1, Performance = 2, Maintainability = 3, Compliance = 4 }

public sealed record ImprovementCandidate(
    ImprovementKind Kind,
    string Title,
    string Rationale,
    string ProposedIntent,
    decimal Confidence,
    ImmutableArray<string> EvidenceReferences);
