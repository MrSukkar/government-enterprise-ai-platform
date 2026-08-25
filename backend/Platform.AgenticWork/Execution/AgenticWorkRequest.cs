using System.Collections.Immutable;

namespace Platform.AgenticWork.Execution;

public sealed record AgenticWorkRequest(
    AgenticWorkDefinition Definition,
    ImmutableHashSet<string> Permissions)
{
    public AgenticWorkRequest Validate()
    {
        ArgumentNullException.ThrowIfNull(Definition);
        ArgumentNullException.ThrowIfNull(Permissions);
        Definition.Validate();
        if (!Permissions.Contains("agentic.work.start"))
            throw new UnauthorizedAccessException("The agentic.work.start permission is required.");
        return this;
    }
}
