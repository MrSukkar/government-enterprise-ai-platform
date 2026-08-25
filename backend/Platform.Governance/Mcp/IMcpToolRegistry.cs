namespace Platform.Governance.Mcp;

public interface IMcpToolRegistry
{
    Task<McpToolBinding?> ResolveAsync(
        string tenantId,
        string actionName,
        string environment,
        CancellationToken cancellationToken);
}
