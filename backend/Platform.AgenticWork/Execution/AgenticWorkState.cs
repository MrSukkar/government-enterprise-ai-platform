namespace Platform.AgenticWork.Execution;

public enum AgenticWorkState
{
    AwaitingApproval,
    Ready,
    Running,
    Suspended,
    Completed,
    Failed,
    Cancelled
}
