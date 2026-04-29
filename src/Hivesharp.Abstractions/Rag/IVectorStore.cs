namespace Hivesharp.Abstractions.Rag;

public interface IVectorStore
{
    Task CreateIndexAsync(string indexName, int dimensions, CancellationToken cancellationToken = default);
    Task<bool> HasIndexAsync(string indexName, CancellationToken cancellationToken = default);
    Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default);
    Task UpsertAsync(string indexName, IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VectorSearchResult>> QueryAsync(string indexName, float[] queryEmbedding, int topK = 10, IReadOnlyDictionary<string, object?>? filter = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(string indexName, IReadOnlyList<string> ids, CancellationToken cancellationToken = default);
}
