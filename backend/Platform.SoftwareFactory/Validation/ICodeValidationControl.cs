namespace Platform.SoftwareFactory.Validation;

public interface ICodeValidationControl
{
    string ControlId { get; }
    ValidationGate Gate { get; }

    Task<ValidationControlReport> ValidateAsync(
        CodeValidationRequest request,
        CancellationToken cancellationToken);
}
