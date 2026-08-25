using System.Collections.Immutable;

namespace Platform.Governance.Mcp;

public sealed record McpInvocation(
    Guid RequestId,
    string TenantId,
    string ServerIdentity,
    string ToolName,
    string InputSchemaSha256Digest,
    ImmutableSortedDictionary<string, string> Arguments,
    string IdempotencyKey,
    ImmutableArray<string> AuthorizationEvidenceReferences);
