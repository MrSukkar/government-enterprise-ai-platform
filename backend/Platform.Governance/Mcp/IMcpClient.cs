namespace Platform.Governance.Mcp;

public interface IMcpClient
{
    Task<McpInvocationResult> InvokeAsync(McpInvocation invocation, CancellationToken cancellationToken);
}
