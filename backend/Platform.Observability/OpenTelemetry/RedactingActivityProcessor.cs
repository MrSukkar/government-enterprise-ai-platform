using System.Diagnostics;
using global::OpenTelemetry;
using Platform.Observability.Redaction;

namespace Platform.Observability.OpenTelemetry;

public sealed class RedactingActivityProcessor(TelemetryAttributeRedactor redactor) : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        ClearBaggage(activity);
    }

    public override void OnEnd(Activity activity)
    {
        foreach (var attribute in activity.TagObjects.ToArray())
        {
            var result = redactor.Redact(attribute.Key, attribute.Value);
            activity.SetTag(
                attribute.Key,
                result.Disposition == TelemetryAttributeDisposition.Drop ? null : result.Value);
        }

        ClearBaggage(activity);
    }

    private static void ClearBaggage(Activity activity)
    {
        foreach (var baggage in activity.Baggage.ToArray())
            activity.SetBaggage(baggage.Key, null);
    }
}
