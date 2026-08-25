namespace Platform.Infrastructure.Sovereignty;

public enum SovereignDependencyKind
{
    ModelRuntime,
    ArtifactRegistry,
    PackageRegistry,
    PolicyAuthority,
    IdentityProvider,
    EvidenceStore,
    ObservabilityBackend,
    SecretsManager,
    KeyManagement
}
