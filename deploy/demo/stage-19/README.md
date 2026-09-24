# Integration Demo Stage 19 runtime contract

Stage 19 extends the localhost-only synthetic demonstration to Governed Automatic Registration. It binds the exact accepted OpenTelemetry evidence and `OpenTelemetry`-stopped run to signed OPA scope, an immutable deployment-controlled registration manifest, the existing deterministic Phase 17 registration engine and atomic repository, result authorization, and append-only PostgreSQL evidence.

Operations must provision all Stage 18 inputs outside Git plus `.runtime/integration-demo-stage-19/automatic-registration/seed.sql`, containing the exact signed registration manifest. Repository defaults remain empty and fail closed. The demo never substitutes a fake manifest, registration, repository commit, Enterprise Object, or evidence result, and stores no credential, secret, private key, endpoint, trust material, institutional data, or runtime in Git.

The receipt records `AutomaticRegistrationOccurred: true`, `EnterpriseModelObjectPersisted: true`, `WorkflowAdvanced: false`, and `CanAdvance: false`. Enterprise Model contextualization, Evidence completion, understanding, inference, autonomous action, and production effects remain prohibited.
