namespace Platform.Knowledge.Retrieval;

public interface IKnowledgeRetrievalSource
{
    RetrievalModality Modality { get; }

    Task<IReadOnlyCollection<KnowledgeCandidate>> RetrieveAsync(
        string queryText,
        AuthorizedRetrievalScope scope,
        CancellationToken cancellationToken);
}
