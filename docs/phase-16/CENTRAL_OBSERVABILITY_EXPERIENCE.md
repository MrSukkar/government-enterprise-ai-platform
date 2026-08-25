# Phase 16 — Central Observability Experience

## Approved signal path

The central experience implements the approved path without introducing a vendor-neutrality deviation:

`Applications -> OpenTelemetry SDK -> Collector Agent -> Trace-aware Routing -> Collector Gateway -> Processing -> Logs/Metrics/Traces -> Storage -> Enterprise Model`

The application exports governed traces and metrics to a configured collector agent using stable OTLP over HTTP/protobuf. Export remains disabled unless a deployment profile supplies an absolute HTTPS endpoint and trust-anchor reference, so local development and air-gapped installations have no accidental external dependency.

## Collector governance

Collector profiles require:

- separate agent and gateway HTTPS endpoints;
- explicit trust-anchor evidence;
- trace-aware routing;
- redaction, tenant isolation, classification enforcement, and batching stages;
- locally operated storage whenever the collector is locally operated;
- exactly one binding for each logs, metrics, and traces signal.

The approved storage mapping is fixed: logs and traces use OpenSearch, while metrics use Prometheus.

## Central read experience

The central query contract requires subject identity, `observability.read`, tenant, environment, purpose, time range, requested signals, and maximum data classification. The service rejects the entire backend response if any record crosses the authorized tenant, environment, signal, time, or classification boundary.

Records are correlated by trace identity across logs, metrics, and traces, ordered by observation time, and retain governed Enterprise Object references. Stored attributes pass through the Phase 15 redactor again at the read boundary; sensitive fields are dropped and unknown fields are redacted before returning results.

## Deferred decisions

No numerical SLO, sampling percentage, retention period, alert threshold, dashboard layout, or workload capacity is invented in this phase. These require workload benchmarking and governed operational decisions.
