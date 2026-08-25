namespace Platform.SoftwareFactory.VerticalSlice;

public interface IVerticalSliceStageExecutor
{
    Task<VerticalSliceStageReceipt> ExecuteAsync(
        VerticalSliceExecutionContext context,
        CancellationToken cancellationToken);
}
