using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.Domain.Security;

namespace Platform.SoftwareFactory.InternalService;

public sealed record GovernedIntentSubmission(
    Guid SubmissionId,
    string TenantId,
    string Purpose,
    DataClassification Classification,
    string ServiceName,
    string Mission,
    string PrimaryUsers,
    string AuthorizationEvidenceReference,
    ImmutableArray<string> IntentEvidenceReferences)
{
    public GovernedIntentSubmission Validate()
    {
        if (SubmissionId == Guid.Empty)
            throw new InvalidOperationException("Submission identity is required.");

        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(Mission);
        ArgumentException.ThrowIfNullOrWhiteSpace(PrimaryUsers);
        ArgumentException.ThrowIfNullOrWhiteSpace(AuthorizationEvidenceReference);

        if (!Enum.IsDefined(Classification))
            throw new InvalidOperationException("A recognized data classification is required.");
        if (IntentEvidenceReferences.IsDefaultOrEmpty)
            throw new InvalidOperationException("Intent evidence is required.");

        foreach (var evidenceReference in IntentEvidenceReferences)
            ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        if (IntentEvidenceReferences.Distinct(StringComparer.Ordinal).Count() != IntentEvidenceReferences.Length)
            throw new InvalidOperationException("Intent evidence references must be unique.");

        return this;
    }
}

public sealed record GovernedIntentValidationReceipt(
    Guid SubmissionId,
    string TenantId,
    string SubjectId,
    DataClassification Classification,
    string IntentDigest,
    DateTimeOffset ValidatedAt,
    string Status,
    bool IsPersisted,
    bool IsPolicyEvaluated,
    bool CanExecute,
    string NextRequiredGate,
    ImmutableArray<string> EvidenceReferences);

public sealed class GovernedIntentSubmissionValidator
{
    public GovernedIntentValidationReceipt Validate(
        GovernedIntentSubmission submission,
        string subjectId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(submission);
        submission.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        if (now == default)
            throw new InvalidOperationException("Server validation time is required.");

        var evidence = submission.IntentEvidenceReferences
            .Append(submission.AuthorizationEvidenceReference)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();

        return new GovernedIntentValidationReceipt(
            submission.SubmissionId,
            submission.TenantId,
            subjectId,
            submission.Classification,
            Digest(submission),
            now,
            "validated-not-persisted",
            IsPersisted: false,
            IsPolicyEvaluated: false,
            CanExecute: false,
            NextRequiredGate: "OPA policy evaluation and governed persistence",
            evidence);
    }

    private static string Digest(GovernedIntentSubmission submission)
    {
        var canonical = string.Join('\u001f',
            submission.SubmissionId.ToString("D"),
            submission.TenantId.Trim(),
            submission.Purpose.Trim(),
            submission.Classification.ToString(),
            submission.ServiceName.Trim(),
            submission.Mission.Trim(),
            submission.PrimaryUsers.Trim(),
            submission.AuthorizationEvidenceReference.Trim(),
            string.Join('\u001e', submission.IntentEvidenceReferences.Select(value => value.Trim())));

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
