namespace Platform.Web.FrontDoor;

public sealed record FrontDoorDestination(
    string Key,
    string Name,
    string Intent,
    string Outcome,
    string RequiredPermission,
    string Availability,
    string Anchor);
