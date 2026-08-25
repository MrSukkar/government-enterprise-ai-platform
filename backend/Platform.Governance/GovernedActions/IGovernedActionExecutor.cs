namespace Platform.Governance.GovernedActions;

public interface IGovernedActionExecutor
{
    Task<GovernedActionResult> ExecuteAsync(AuthorizedActionCommand command, CancellationToken cancellationToken);
}
