namespace Platform.AgenticWork.Execution;

public interface IDurableAgenticWorkStore
{
    Task<AgenticWorkInstance> CreateAtomicallyAsync(
        AgenticWorkInstance instance,
        AgenticWorkTransition transition,
        CancellationToken cancellationToken);

    Task<AgenticWorkInstance?> LoadAsync(Guid workId, CancellationToken cancellationToken);

    Task<AgenticWorkInstance> AppendAtomicallyAsync(
        AgenticWorkInstance updatedInstance,
        AgenticWorkTransition transition,
        CancellationToken cancellationToken);
}
