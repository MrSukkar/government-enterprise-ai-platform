using Platform.Governance.GovernedActions;

namespace Platform.Governance.Mcp;

public sealed class McpGovernedActionExecutor(
    IMcpToolRegistry toolRegistry,
    IMcpClient mcpClient) : IGovernedActionExecutor
{
    public async Task<GovernedActionResult> ExecuteAsync(
        AuthorizedActionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAuthorizedCommand(command);
        var binding = await toolRegistry.ResolveAsync(
            command.TenantId, command.ActionName, command.Environment, cancellationToken)
            ?? throw new UnauthorizedAccessException("No governed MCP tool binding exists for the action.");
        binding.Validate();
        if (!binding.Enabled ||
            !StringComparer.Ordinal.Equals(binding.TenantId, command.TenantId) ||
            !StringComparer.Ordinal.Equals(binding.ActionName, command.ActionName) ||
            !StringComparer.Ordinal.Equals(binding.Environment, command.Environment) ||
            command.Classification > binding.MaximumClassification)
            throw new UnauthorizedAccessException("MCP tool binding denied the governed action.");

        var invocation = new McpInvocation(
            command.RequestId,
            command.TenantId,
            binding.ServerIdentity,
            binding.ToolName,
            binding.InputSchemaSha256Digest,
            command.Parameters,
            command.IdempotencyKey,
            command.EvidenceReferences.Add(binding.RegistrationEvidenceReference));
        var result = await mcpClient.InvokeAsync(invocation, cancellationToken);
        ValidateMcpResult(invocation, result);
        return new GovernedActionResult(
            result.RequestId,
            result.IdempotencyKey,
            result.Succeeded,
            result.ResultReference,
            result.EvidenceReferences.Add(binding.RegistrationEvidenceReference),
            result.CompletedAt);
    }

    private static void ValidateAuthorizedCommand(AuthorizedActionCommand command)
    {
        if (command.RequestId == Guid.Empty || command.DecisionRequestId == Guid.Empty)
            throw new InvalidOperationException("Authorized action identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorSubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.TargetResource);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.PolicyBundleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.PolicyBundleVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ApprovalEvidenceReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentNullException.ThrowIfNull(command.Parameters);
        if (command.PolicyBundleSha256Digest.Length != 64 || command.EvidenceReferences.IsDefaultOrEmpty)
            throw new UnauthorizedAccessException("Authorized action policy or evidence is invalid.");
    }

    private static void ValidateMcpResult(McpInvocation invocation, McpInvocationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.RequestId != invocation.RequestId ||
            !StringComparer.Ordinal.Equals(result.ServerIdentity, invocation.ServerIdentity) ||
            !StringComparer.Ordinal.Equals(result.ToolName, invocation.ToolName) ||
            !StringComparer.OrdinalIgnoreCase.Equals(result.InputSchemaSha256Digest, invocation.InputSchemaSha256Digest) ||
            !StringComparer.Ordinal.Equals(result.IdempotencyKey, invocation.IdempotencyKey))
            throw new InvalidOperationException("MCP returned a mismatched result.");
        ArgumentException.ThrowIfNullOrWhiteSpace(result.ResultReference);
        if (result.EvidenceReferences.IsDefaultOrEmpty || result.CompletedAt == default)
            throw new InvalidOperationException("MCP results require time and evidence.");
        foreach (var value in result.EvidenceReferences) ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }
}
