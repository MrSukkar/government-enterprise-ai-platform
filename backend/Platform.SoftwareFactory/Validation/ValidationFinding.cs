namespace Platform.SoftwareFactory.Validation;

public sealed record ValidationFinding(
    string RuleId,
    ValidationSeverity Severity,
    string Message,
    string Location,
    string EvidenceReference)
{
    public bool IsBlocking => Severity is ValidationSeverity.Error or ValidationSeverity.Critical;
}
