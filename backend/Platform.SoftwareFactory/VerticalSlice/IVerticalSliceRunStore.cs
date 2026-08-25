namespace Platform.SoftwareFactory.VerticalSlice;

public interface IVerticalSliceRunStore
{
    Task<VerticalSliceRun> CreateAtomicallyAsync(VerticalSliceRun run, CancellationToken cancellationToken);
    Task<VerticalSliceRun?> LoadAsync(Guid runId, string tenantId, CancellationToken cancellationToken);
    Task<VerticalSliceRun> AppendAtomicallyAsync(
        VerticalSliceRun run,
        long expectedVersion,
        CancellationToken cancellationToken);
}
