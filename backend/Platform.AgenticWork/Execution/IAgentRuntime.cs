namespace Platform.AgenticWork.Execution;

public interface IAgentRuntime
{
    Task<AgentStepResult> ExecuteStepAsync(
        AgentStepExecutionContext context,
        CancellationToken cancellationToken);
}
