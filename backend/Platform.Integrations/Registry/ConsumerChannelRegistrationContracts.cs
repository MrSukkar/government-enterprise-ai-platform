using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Platform.EnterpriseModel.Model;
using Platform.Integrations.Constitution;

namespace Platform.Integrations.Registry;

public sealed record ConsumerChannelRegistrationRequest(
    Guid RequestId,
    string SubjectId,
    IntegrationConstitutionalContract Contract,
    long ExpectedVersion,
    DateTimeOffset RequestedAt)
{
    public ConsumerChannelRegistrationRequest Validate()
    {
        if (RequestId == Guid.Empty) throw new ArgumentException("Request identity is required.", nameof(RequestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentNullException.ThrowIfNull(Contract);
        Contract.Validate();
        if (Contract.Kind is not (IntegrationAssetKind.Consumer or IntegrationAssetKind.Channel))
        {
            throw new InvalidOperationException("V3-02 registers Consumer or Channel contracts only.");
        }
        if (Contract.Lifecycle != LifecycleState.Proposed)
        {
            throw new InvalidOperationException("New registrations must begin in Proposed lifecycle state.");
        }
        if (ExpectedVersion < 0) throw new ArgumentOutOfRangeException(nameof(ExpectedVersion));
        return this;
    }

    public string ComputeFingerprint()
    {
        Validate();
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this))));
    }
}

public sealed record ConsumerChannelPolicyDecision(
    Guid DecisionId,
    string Action,
    string ContractFingerprint,
    string TenantId,
    string Purpose,
    string Environment,
    bool Permit,
    string PolicyBundleReference,
    ImmutableArray<string> EvidenceReferences)
{
    public ConsumerChannelPolicyDecision ValidateFor(
        string expectedAction,
        string expectedFingerprint,
        string expectedTenant,
        string expectedPurpose,
        string expectedEnvironment)
    {
        if (DecisionId == Guid.Empty) throw new InvalidOperationException("Policy decision identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(PolicyBundleReference);
        if (!Permit || !string.Equals(Action, expectedAction, StringComparison.Ordinal) ||
            !string.Equals(ContractFingerprint, expectedFingerprint, StringComparison.Ordinal) ||
            !string.Equals(TenantId, expectedTenant, StringComparison.Ordinal) ||
            !string.Equals(Purpose, expectedPurpose, StringComparison.Ordinal) ||
            !string.Equals(Environment, expectedEnvironment, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("OPA decision denied or did not exactly match the governed request.");
        }
        ReferenceValidation.Require(EvidenceReferences, nameof(EvidenceReferences));
        return this;
    }
}

public enum ConsumerChannelRegistrationDisposition
{
    Created,
    Unchanged
}

public sealed record ConsumerChannelRegistrationCommit(
    Guid RegistrationId,
    Guid RequestId,
    string ContractId,
    string ContractFingerprint,
    long Version,
    ConsumerChannelRegistrationDisposition Disposition,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CommittedAt)
{
    public ConsumerChannelRegistrationCommit ValidateFor(ConsumerChannelRegistrationRequest request)
    {
        if (RegistrationId == Guid.Empty || RequestId != request.RequestId || Version < 1 ||
            !string.Equals(ContractId, request.Contract.ContractId, StringComparison.Ordinal) ||
            !string.Equals(ContractFingerprint, request.ComputeFingerprint(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Atomic registration commit does not match the governed request.");
        }
        if (!Enum.IsDefined(Disposition)) throw new InvalidOperationException("Unknown registration disposition.");
        ReferenceValidation.Require(EvidenceReferences, nameof(EvidenceReferences));
        return this;
    }
}

internal static class ReferenceValidation
{
    internal static void Require(ImmutableArray<string> references, string name)
    {
        if (references.IsDefaultOrEmpty || references.Any(string.IsNullOrWhiteSpace) ||
            references.Distinct(StringComparer.Ordinal).Count() != references.Length)
        {
            throw new ArgumentException($"{name} requires unique non-empty references.", name);
        }
    }
}
