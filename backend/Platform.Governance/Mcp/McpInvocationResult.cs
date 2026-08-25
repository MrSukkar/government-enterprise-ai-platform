using System.Collections.Immutable;

namespace Platform.Governance.Mcp;

public sealed record McpInvocationResult(
    Guid RequestId,
    string ServerIdentity,
    string ToolName,
    string InputSchemaSha256Digest,
    string IdempotencyKey,
    bool Succeeded,
    string ResultReference,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CompletedAt);
