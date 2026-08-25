using System.Collections.Immutable;
using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Registration;

public sealed record AutomaticRegistrationProposal(
    Guid RequestId,
    AutomaticRegistrationKey Key,
    string RequestFingerprint,
    EnterpriseObject Candidate,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset ProposedAt);
