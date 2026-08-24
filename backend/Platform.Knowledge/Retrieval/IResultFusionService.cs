namespace Platform.Knowledge.Retrieval;

public interface IResultFusionService
{
    IReadOnlyList<KnowledgeCandidate> FuseAndRerank(
        IEnumerable<KnowledgeCandidate> candidates,
        int maximumResults);
}
