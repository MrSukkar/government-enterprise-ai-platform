using Platform.EnterpriseModel.Model;

namespace Platform.EnterpriseModel.Registration;

public sealed record AutomaticRegistrationCommit(
    Guid RequestId,
    AutomaticRegistrationKey Key,
    string RequestFingerprint,
    RegistrationDisposition Disposition,
    EnterpriseObject EnterpriseObject,
    string EvidenceReference,
    DateTimeOffset CommittedAt);
