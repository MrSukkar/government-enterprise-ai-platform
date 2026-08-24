using Platform.SoftwareFactory.Delivery;

namespace Platform.SoftwareFactory.Validation;

public sealed class CodeValidationPipeline(IEnumerable<ICodeValidationControl> controls)
{
    private readonly IReadOnlyCollection<ICodeValidationControl> _controls = controls.ToArray();

    public async Task<ValidationGateReport> ExecuteAsync(
        CodeValidationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Run.Validate();
        request.Candidate.Validate();

        var requiredPreviousStage = request.Gate switch
        {
            ValidationGate.Static => DeliveryStage.CodeGeneration,
            ValidationGate.Security => DeliveryStage.StaticValidation,
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        if (request.Run.CurrentStage != requiredPreviousStage)
            throw new InvalidOperationException($"Validation gate '{request.Gate}' requires stage '{requiredPreviousStage}'.");

        var selected = _controls.Where(control => control.Gate == request.Gate).ToArray();
        if (selected.Length == 0)
            throw new InvalidOperationException($"No validation controls are registered for gate '{request.Gate}'.");

        var reports = await Task.WhenAll(selected.Select(control => control.ValidateAsync(request, cancellationToken)));
        if (reports.Any(report => report.Gate != request.Gate || string.IsNullOrWhiteSpace(report.ControlId)))
            throw new InvalidOperationException("A validation control returned an invalid report.");

        return new ValidationGateReport(request.Gate, [.. reports]);
    }
}
