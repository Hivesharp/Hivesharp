using Hivesharp.Abstractions.Rag;

namespace Hivesharp.Rag;

internal sealed class RagPipeline(
    IVectorStore vectorStore,
    ITextEmbedder embedder,
    IChunkingStrategy chunkingStrategy,
    string indexName,
    int dimensions,
    int chunkSize = 512,
    int chunkOverlap = 50) : IRagPipeline
{
    public RagPipelineDescriptor Descriptor { get; } = new()
    {
        IndexName = indexName,
        Dimensions = dimensions,
        ChunkSize = chunkSize,
        ChunkOverlap = chunkOverlap,
        VectorStoreBackend = DescribeBackend(vectorStore)
    };

    private static string DescribeBackend(IVectorStore store)
    {
        var typeName = store.GetType().Name;
        return typeName switch
        {
            "InMemoryVectorStore" => "In-memory",
            "PostgresVectorStore" => "Postgres",
            "RedisVectorStore" => "Redis",
            _ => typeName.EndsWith("VectorStore", StringComparison.Ordinal)
                ? typeName[..^"VectorStore".Length]
                : typeName
        };
    }

    public async Task IngestAsync(RagDocument document, CancellationToken cancellationToken = default)
    {
        var chunks = chunkingStrategy.Chunk(document);

        if (chunks.Count == 0)
            return;

        var texts = chunks.Select(c => c.Text).ToList();
        var embeddings = await embedder.EmbedManyAsync(texts, cancellationToken);

        var records = chunks.Zip(embeddings, (chunk, embedding) => new VectorRecord
        {
            Id = $"{indexName}_{chunk.Index}_{Guid.NewGuid():N}",
            Embedding = embedding,
            Text = chunk.Text,
            Metadata = chunk.Metadata
        }).ToList();

        if (!await vectorStore.HasIndexAsync(indexName, cancellationToken))
            await vectorStore.CreateIndexAsync(indexName, dimensions, cancellationToken);

        await vectorStore.UpsertAsync(indexName, records, cancellationToken);
    }

    public async Task IngestManyAsync(IReadOnlyList<RagDocument> documents, CancellationToken cancellationToken = default)
    {
        foreach (var document in documents)
            await IngestAsync(document, cancellationToken);
    }
}
