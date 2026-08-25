namespace Platform.EnterpriseModel.Registration;

public interface IAutomaticRegistrationRepository
{
    Task<AutomaticRegistrationCommit> RegisterAtomicallyAsync(
        AutomaticRegistrationProposal proposal,
        CancellationToken cancellationToken);
}
