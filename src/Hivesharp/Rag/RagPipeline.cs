using System.Diagnostics;
using Hivesharp.Abstractions.Rag;
using Hivesharp.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hivesharp.Rag;

internal sealed class RagPipeline : IRagPipeline
{
    private readonly IVectorStore _vectorStore;
    private readonly ITextEmbedder _embedder;
    private readonly IChunkingStrategy _chunkingStrategy;
    private readonly string _indexName;
    private readonly int _dimensions;
    private readonly ILogger _logger;

    public RagPipeline(
        IVectorStore vectorStore,
        ITextEmbedder embedder,
        IChunkingStrategy chunkingStrategy,
        string indexName,
        int dimensions,
        int chunkSize = 512,
        int chunkOverlap = 50,
        ILogger<RagPipeline>? logger = null)
    {
        _vectorStore = vectorStore;
        _embedder = embedder;
        _chunkingStrategy = chunkingStrategy;
        _indexName = indexName;
        _dimensions = dimensions;
        _logger = logger ?? NullLogger<RagPipeline>.Instance;
        Descriptor = new RagPipelineDescriptor
        {
            IndexName = indexName,
            Dimensions = dimensions,
            ChunkSize = chunkSize,
            ChunkOverlap = chunkOverlap,
            VectorStoreBackend = DescribeBackend(vectorStore)
        };
    }

    public RagPipelineDescriptor Descriptor { get; }

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
        var sourceLength = document.Content.Length;
        RagLog.IngestStarted(_logger, _indexName, sourceLength);
        var sw = Stopwatch.StartNew();

        var chunks = _chunkingStrategy.Chunk(document);

        if (chunks.Count == 0)
        {
            sw.Stop();
            RagLog.IngestCompleted(_logger, _indexName, 0, sw.ElapsedMilliseconds);
            return;
        }

        RagLog.Chunked(_logger, _indexName, chunks.Count);

        var texts = chunks.Select(c => c.Text).ToList();
        var embedSw = Stopwatch.StartNew();
        var embeddings = await _embedder.EmbedManyAsync(texts, cancellationToken);
        embedSw.Stop();
        RagLog.Embedded(_logger, _indexName, embeddings.Count, embedSw.ElapsedMilliseconds);

        var records = chunks.Zip(embeddings, (chunk, embedding) => new VectorRecord
        {
            Id = $"{_indexName}_{chunk.Index}_{Guid.NewGuid():N}",
            Embedding = embedding,
            Text = chunk.Text,
            Metadata = chunk.Metadata
        }).ToList();

        if (!await _vectorStore.HasIndexAsync(_indexName, cancellationToken))
            await _vectorStore.CreateIndexAsync(_indexName, _dimensions, cancellationToken);

        await _vectorStore.UpsertAsync(_indexName, records, cancellationToken);

        sw.Stop();
        RagLog.IngestCompleted(_logger, _indexName, chunks.Count, sw.ElapsedMilliseconds);
    }

    public async Task IngestManyAsync(IReadOnlyList<RagDocument> documents, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        foreach (var document in documents)
            await IngestAsync(document, cancellationToken);
        sw.Stop();
        RagLog.IngestManyCompleted(_logger, _indexName, documents.Count, sw.ElapsedMilliseconds);
    }
}
