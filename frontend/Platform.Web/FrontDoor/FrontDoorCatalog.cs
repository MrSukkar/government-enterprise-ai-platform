namespace Platform.Web.FrontDoor;

public static class FrontDoorCatalog
{
    public static IReadOnlyList<FrontDoorDestination> Destinations { get; } =
    [
        new("BUILD", "Build", "Create or change governed software", "Validated delivery with human review and evidence", "frontdoor.build.read", "Software Factory foundation available", "build"),
        new("UNDERSTAND", "Understand", "Explore institutional context", "Authorized knowledge, relationships, and impact", "frontdoor.understand.read", "Understanding and modeling foundations available", "understand"),
        new("OPERATE", "Operate", "Assess services and operational posture", "Trace-linked health, resilience, and proactive findings", "frontdoor.operate.read", "Observability and resilience foundations available", "operate"),
        new("ACT", "Act", "Request a governed enterprise action", "OPA decision, approval, MCP execution, and evidence", "frontdoor.act.request", "Governance gateway available", "act"),
        new("PROVE", "Prove", "Review the trust chain", "Policy, approval, result, telemetry, and verification evidence", "frontdoor.evidence.read", "Final cryptographic proof completes in Phase 30", "prove")
    ];
}
