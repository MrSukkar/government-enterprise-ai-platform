using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Platform.EnterpriseModel.Model;
using Platform.Integrations.Constitution;

namespace Platform.Integrations.Registry;

public sealed record ConsumerChannelLifecycleRequest(
    Guid TransitionId,
    Guid RegistrationId,
    string ContractId,
    IntegrationAssetKind Kind,
    string SubjectId,
    string TenantId,
    string Purpose,
    string Environment,
    LifecycleState From,
    LifecycleState To,
    long ExpectedVersion,
    DateTimeOffset RequestedAt)
{
    public ConsumerChannelLifecycleRequest Validate()
    {
        if (TransitionId == Guid.Empty || RegistrationId == Guid.Empty) throw new ArgumentException("Transition and registration identities are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(ContractId);
        ArgumentException.ThrowIfNullOrWhiteSpace(SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(Purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(Environment);
        if (Kind is not (IntegrationAssetKind.Consumer or IntegrationAssetKind.Channel)) throw new InvalidOperationException("Lifecycle is limited to Consumer and Channel contracts.");
        if (ExpectedVersion < 1) throw new ArgumentOutOfRangeException(nameof(ExpectedVersion));
        if (!IsAllowedTransition(From, To)) throw new InvalidOperationException($"Lifecycle transition {From} -> {To} is prohibited.");
        return this;
    }

    public string ComputeFingerprint()
    {
        Validate();
        var canonical = string.Join('|', TransitionId, RegistrationId, ContractId, Kind, SubjectId, TenantId, Purpose, Environment, From, To, ExpectedVersion, RequestedAt.ToUniversalTime().Ticks);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static bool IsAllowedTransition(LifecycleState from, LifecycleState to) =>
        (from, to) is
            (LifecycleState.Proposed, LifecycleState.Active) or
            (LifecycleState.Active, LifecycleState.Deprecated) or
            (LifecycleState.Deprecated, LifecycleState.Retired);
}

public sealed record ConsumerChannelLifecycleCommit(
    Guid TransitionId,
    Guid RegistrationId,
    string RequestFingerprint,
    LifecycleState From,
    LifecycleState To,
    long Version,
    ImmutableArray<string> EvidenceReferences,
    DateTimeOffset CommittedAt)
{
    public ConsumerChannelLifecycleCommit ValidateFor(ConsumerChannelLifecycleRequest request)
    {
        if (TransitionId != request.TransitionId || RegistrationId != request.RegistrationId ||
            Version != request.ExpectedVersion + 1 || From != request.From || To != request.To ||
            !string.Equals(RequestFingerprint, request.ComputeFingerprint(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Atomic lifecycle commit does not match the governed request.");
        }
        ReferenceValidation.Require(EvidenceReferences, nameof(EvidenceReferences));
        return this;
    }
}
