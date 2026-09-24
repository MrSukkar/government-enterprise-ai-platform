using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.EnterpriseModel.Model;
using Platform.Integrations.Constitution;

namespace Platform.Integrations.ApiCatalog;

public enum ApiLifecycleState
{
    Proposed,
    Published,
    Deprecated,
    Retired
}

public sealed record ApiCatalogPublicationRequest(
    Guid RequestId,
    string SubjectId,
    IntegrationConstitutionalContract Contract,
    string OpenApiDocument,
    long ExpectedVersion,
    DateTimeOffset RequestedAt)
{
    public ApiCatalogPublicationRequest Validate()
    {
        if (RequestId == Guid.Empty) throw new ArgumentException("Request identity is required.", nameof(RequestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(OpenApiDocument);
        ArgumentNullException.ThrowIfNull(Contract);
        Contract.Validate();
        if (Contract.Kind != IntegrationAssetKind.Api || Contract.Lifecycle != LifecycleState.Proposed)
            throw new InvalidOperationException("API catalog publication requires a Proposed API constitutional contract.");
        if (ExpectedVersion < 0) throw new ArgumentOutOfRangeException(nameof(ExpectedVersion));
        return this;
    }

    public string ComputeFingerprint(string documentDigest)
    {
        Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(documentDigest);
        var canonical = string.Join('|', RequestId, SubjectId, Contract.ContractId, Contract.Version, Contract.EnterpriseObjectId,
            Contract.TenantId, Contract.Purpose, Contract.Environment, Contract.Classification, documentDigest, ExpectedVersion,
            RequestedAt.ToUniversalTime().Ticks);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}

public sealed record ApiCatalogPolicyDecision(
    Guid DecisionId,
    string Action,
    string RequestFingerprint,
    string DocumentSha256Digest,
    string TenantId,
    string Purpose,
    string Environment,
    bool Permit,
    string PolicyBundleReference,
    ImmutableArray<string> EvidenceReferences)
{
    public void RequireExact(ApiCatalogPublicationRequest request, OpenApi31ValidationReport report)
    {
        if (DecisionId == Guid.Empty || !Permit ||
            !string.Equals(Action, "integration.api.catalog.publish", StringComparison.Ordinal) ||
            !string.Equals(RequestFingerprint, request.ComputeFingerprint(report.DocumentSha256Digest), StringComparison.Ordinal) ||
            !string.Equals(DocumentSha256Digest, report.DocumentSha256Digest, StringComparison.Ordinal) ||
            !string.Equals(TenantId, request.Contract.TenantId, StringComparison.Ordinal) ||
            !string.Equals(Purpose, request.Contract.Purpose, StringComparison.Ordinal) ||
            !string.Equals(Environment, request.Contract.Environment, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("OPA denied API publication or returned mismatched scope.");
        ArgumentException.ThrowIfNullOrWhiteSpace(PolicyBundleReference);
        RequireReferences(EvidenceReferences, nameof(EvidenceReferences));
    }

    internal static void RequireReferences(ImmutableArray<string> references, string name)
    {
        if (references.IsDefaultOrEmpty || references.Any(string.IsNullOrWhiteSpace) ||
            references.Distinct(StringComparer.Ordinal).Count() != references.Length)
            throw new ArgumentException($"{name} requires unique non-empty references.", name);
    }
}

public sealed record ApiCatalogCommit(
    Guid CatalogEntryId,
    Guid RequestId,
    string ContractId,
    string ContractVersion,
    string DocumentSha256Digest,
    ApiLifecycleState Lifecycle,
    long Version,
    ImmutableArray<string> OperationIds,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CommittedAt)
{
    public ApiCatalogCommit ValidateFor(ApiCatalogPublicationRequest request, OpenApi31ValidationReport report)
    {
        if (CatalogEntryId == Guid.Empty || RequestId != request.RequestId || Version < 1 || Lifecycle != ApiLifecycleState.Published ||
            !string.Equals(ContractId, request.Contract.ContractId, StringComparison.Ordinal) ||
            !string.Equals(ContractVersion, request.Contract.Version, StringComparison.Ordinal) ||
            !string.Equals(DocumentSha256Digest, report.DocumentSha256Digest, StringComparison.Ordinal) ||
            !OperationIds.SequenceEqual(report.OperationIds, StringComparer.Ordinal))
            throw new InvalidOperationException("Atomic API catalog commit does not match the governed publication.");
        ApiCatalogPolicyDecision.RequireReferences(EvidenceReferences, nameof(EvidenceReferences));
        return this;
    }
}

public sealed record ApiCatalogReleaseDecision(
    Guid DecisionId,
    Guid CatalogEntryId,
    string DocumentSha256Digest,
    bool Permit,
    ImmutableArray<string> EvidenceReferences)
{
    public void RequireExact(ApiCatalogCommit commit)
    {
        if (DecisionId == Guid.Empty || !Permit || CatalogEntryId != commit.CatalogEntryId ||
            !string.Equals(DocumentSha256Digest, commit.DocumentSha256Digest, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("API catalog result release was denied or mismatched.");
        ApiCatalogPolicyDecision.RequireReferences(EvidenceReferences, nameof(EvidenceReferences));
    }
}

public sealed record ApiLifecycleTransitionRequest(ApiLifecycleState From, ApiLifecycleState To)
{
    public ApiLifecycleTransitionRequest Validate()
    {
        var allowed = (From, To) is
            (ApiLifecycleState.Proposed, ApiLifecycleState.Published) or
            (ApiLifecycleState.Published, ApiLifecycleState.Deprecated) or
            (ApiLifecycleState.Deprecated, ApiLifecycleState.Retired);
        if (!allowed)
            throw new InvalidOperationException($"API lifecycle transition {From} -> {To} is prohibited.");
        return this;
    }
}
