# Integration Demo Stage 17 runtime contract

Stage 17 extends the localhost-only synthetic demonstration to Governed Sovereign Deployment. It binds exact accepted Artifact evidence and the `Artifact`-stopped run to signed OPA scope, a signed sovereign profile, institutional preflight, a provider-neutral `IInstitutionalSovereignDeploymentGateway`, result authorization, and append-only PostgreSQL evidence.

Deployment operations must provision all Stage 16 inputs outside Git plus `.runtime/integration-demo-stage-17/deployment/seed.sql`, containing the exact `Artifact`-stopped snapshot and signed profile. They must configure `Platform:SoftwareFactory:DeploymentRuntime` with a real approved sovereign non-production runtime, gateway/operator identity, positive bounds, profile, target, and pinned trust. The profile binds every required dependency locally with default-deny outbound networking and no external control plane, API, AI service, or SaaS dependency.

Repository defaults remain empty and fail closed. The demo must never substitute a fake approval, profile, preflight, runtime, activation, rollback, gateway response, or successful result. No credential, secret, private key, endpoint, trust material, institutional data, deployment, or runtime is stored in Git. The receipt records `DeploymentOccurred: true`, `ExternalEffectOccurred: true`, `ProductionEffectOccurred: false`, `TelemetryConfigured: false`, `AutomaticRegistrationOccurred: false`, `EnterpriseModelMutated: false`, and `CanAdvance: false`. Public or production deployment is prohibited.

