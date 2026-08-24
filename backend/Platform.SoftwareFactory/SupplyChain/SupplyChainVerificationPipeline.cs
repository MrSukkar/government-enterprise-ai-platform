namespace Platform.SoftwareFactory.SupplyChain;

public sealed class SupplyChainVerificationPipeline(IEnumerable<ISupplyChainControlVerifier> verifiers)
{
    private readonly IReadOnlyCollection<ISupplyChainControlVerifier> _verifiers = verifiers.ToArray();

    public async Task<SupplyChainVerificationReport> VerifyAsync(
        ArtifactSupplyChainRecord artifact,
        CancellationToken cancellationToken)
    {
        artifact.Validate();
        var required = Enum.GetValues<SupplyChainControl>();
        var duplicate = _verifiers.GroupBy(item => item.Control).FirstOrDefault(group => group.Count() != 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Supply-chain control '{duplicate.Key}' must have exactly one verifier.");
        if (required.Any(control => _verifiers.All(verifier => verifier.Control != control)))
            throw new InvalidOperationException("Every supply-chain control requires a registered verifier.");

        var results = await Task.WhenAll(_verifiers.Select(verifier => verifier.VerifyAsync(artifact, cancellationToken)));
        if (results.Any(result => _verifiers.All(verifier => verifier.Control != result.Control)))
            throw new InvalidOperationException("A verifier returned an unregistered control result.");

        return new SupplyChainVerificationReport(artifact, [.. results]);
    }
}
