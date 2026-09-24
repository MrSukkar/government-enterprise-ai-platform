using System.Collections.Immutable;

namespace Platform.Integrations.Constitution;

public static class IntegrationEnterpriseModelTypes
{
    private static readonly ImmutableDictionary<IntegrationAssetKind, string> ObjectTypes =
        new Dictionary<IntegrationAssetKind, string>
        {
            [IntegrationAssetKind.Consumer] = "Integration.Consumer",
            [IntegrationAssetKind.Channel] = "Integration.Channel",
            [IntegrationAssetKind.Api] = "Integration.Api",
            [IntegrationAssetKind.Message] = "Integration.Message",
            [IntegrationAssetKind.Schema] = "Integration.Schema",
            [IntegrationAssetKind.Integration] = "Integration.Flow",
            [IntegrationAssetKind.Orchestration] = "Integration.Orchestration"
        }.ToImmutableDictionary();

    public static string GetObjectType(IntegrationAssetKind kind)
    {
        if (!Enum.IsDefined(kind) || !ObjectTypes.TryGetValue(kind, out var objectType))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), "Unknown integration asset kind fails closed.");
        }

        return objectType;
    }

    public static IReadOnlyDictionary<IntegrationAssetKind, string> All => ObjectTypes;
}
