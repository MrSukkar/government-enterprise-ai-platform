# Operationalization Wave 09 — Governed Static Validation

Wave 09 operationalizes only the Static Validation boundary approved by CR-010. Deployment operators provide complete PostgreSQL and signed-policy configuration plus an exact set of institutionally approved, cryptographically signed Static control profiles and pinned public keys.

Each profile declares a unique control identity, deterministic case-sensitive required/forbidden text rules, allowed source extensions, approval evidence, and signature. The platform verifies the profile before in-memory inspection, executes it only through `CodeValidationPipeline` at `ValidationGate.Static`, rejects any Error/Critical finding, re-authorizes the complete report, and appends immutable PostgreSQL evidence.

Repository defaults contain no controls, rules, keys, institutional data, endpoints, or credentials. No source is written, compiled, executed, scanned by an external service, or advanced to Security Validation.
