using System.Collections.Immutable;

namespace Platform.SoftwareFactory.DeveloperExperience;

public sealed record DeveloperEnvironmentSnapshot(
    Guid RequestId,
    string DotNetSdkVersion,
    bool GitAvailable,
    bool LocalPackageSourceAvailable,
    bool HasProductionCredentials,
    bool OutboundNetworkRequired,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset InspectedAt);
