using System.Collections.Immutable;

namespace Platform.SoftwareFactory.Validation;

public sealed record ValidationGateReport(
    ValidationGate Gate,
    ImmutableArray<ValidationControlReport> ControlReports)
{
    public bool IsAccepted => !ControlReports.IsDefaultOrEmpty && ControlReports.All(report => report.Passed);
}
