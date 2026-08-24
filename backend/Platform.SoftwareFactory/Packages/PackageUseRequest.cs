namespace Platform.SoftwareFactory.Packages;

public sealed record PackageUseRequest(
    PackageCoordinate Coordinate,
    string TenantId,
    string Environment,
    DateTimeOffset RequestedAt);
