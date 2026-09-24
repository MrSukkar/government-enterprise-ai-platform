namespace Platform.Integrations.Registry;

public interface IConsumerChannelPolicyAuthorizer
{
    Task<ConsumerChannelPolicyDecision> AuthorizeRegistrationAsync(ConsumerChannelRegistrationRequest request, CancellationToken cancellationToken);
    Task<ConsumerChannelPolicyDecision> AuthorizeLifecycleAsync(ConsumerChannelLifecycleRequest request, CancellationToken cancellationToken);
}

public interface IConsumerChannelRegistryRepository
{
    Task<ConsumerChannelRegistrationCommit> RegisterAtomicallyAsync(
        ConsumerChannelRegistrationRequest request,
        ConsumerChannelPolicyDecision decision,
        CancellationToken cancellationToken);

    Task<ConsumerChannelLifecycleCommit> TransitionAtomicallyAsync(
        ConsumerChannelLifecycleRequest request,
        ConsumerChannelPolicyDecision decision,
        CancellationToken cancellationToken);
}

public sealed class GovernedConsumerChannelRegistry(
    IConsumerChannelPolicyAuthorizer policyAuthorizer,
    IConsumerChannelRegistryRepository repository)
{
    public async Task<ConsumerChannelRegistrationCommit> RegisterAsync(
        ConsumerChannelRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var fingerprint = request.ComputeFingerprint();
        var decision = await policyAuthorizer.AuthorizeRegistrationAsync(request, cancellationToken);
        decision.ValidateFor("integration.consumer-channel.register", fingerprint, request.Contract.TenantId,
            request.Contract.Purpose, request.Contract.Environment);
        var commit = await repository.RegisterAtomicallyAsync(request, decision, cancellationToken);
        return commit.ValidateFor(request);
    }

    public async Task<ConsumerChannelLifecycleCommit> TransitionAsync(
        ConsumerChannelLifecycleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        var fingerprint = request.ComputeFingerprint();
        var decision = await policyAuthorizer.AuthorizeLifecycleAsync(request, cancellationToken);
        decision.ValidateFor("integration.consumer-channel.lifecycle", fingerprint, request.TenantId,
            request.Purpose, request.Environment);
        var commit = await repository.TransitionAtomicallyAsync(request, decision, cancellationToken);
        return commit.ValidateFor(request);
    }
}
