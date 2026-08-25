using Platform.Domain.Security;

namespace Platform.Governance.Mcp;

public sealed record McpToolBinding(
    string TenantId,
    string ActionName,
    string Environment,
    string ServerIdentity,
    string ToolName,
    string InputSchemaSha256Digest,
    DataClassification MaximumClassification,
    bool Enabled,
    string RegistrationEvidenceReference)
{
    public McpToolBinding Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ActionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(ServerIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(ToolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(RegistrationEvidenceReference);
        if (InputSchemaSha256Digest.Length != 64 || InputSchemaSha256Digest.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("MCP tool binding requires an input-schema SHA-256 digest.");
        return this;
    }
}
