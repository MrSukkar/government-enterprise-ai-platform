# Phase 15 — OpenTelemetry Core & Redaction

## Purpose

The platform now has an OpenTelemetry SDK boundary for distributed traces and governed metrics. Version 1.17.0 is explicitly pinned and locked because it provides stable .NET 10 support. Central collectors, storage, dashboards, and operator experience remain reserved for Phase 16.

## Signal core

- ASP.NET Core requests are instrumented through the official OpenTelemetry instrumentation package.
- Platform operations use one governed `ActivitySource` for traces.
- Platform measurements use one governed `Meter` with an operation counter and duration histogram.
- Operation names and outcomes must be low-cardinality lowercase tokens.
- Trace and metric resource identity is set to `GovernmentEnterpriseAIPlatform.Api`.
- No exporter or external endpoint is required by this phase, preserving air-gapped and sovereign deployment.

## Fail-closed redaction

Telemetry attributes pass through a strict policy before export:

- authorization, cookies, credentials, passwords, secrets, tokens, API keys, private keys, connection strings, URL queries, exception messages, and exception stack traces are dropped;
- explicitly approved semantic attributes and the governed `platform.*` namespace are retained;
- unknown attribute names are replaced with `[REDACTED]`;
- retained strings are length-bounded;
- all activity baggage is cleared at span start and end to prevent uncontrolled propagation;
- automatic ASP.NET Core spans pass through the redacting activity processor;
- platform metrics expose only the controlled operation name and outcome dimensions.

## Architectural boundary

Phase 15 produces SDK signals and enforces redaction at the application boundary. Phase 16 may attach sovereign collector/exporter adapters and central operational experiences after this processor, without weakening the redaction policy or introducing a vendor dependency into the platform core.
