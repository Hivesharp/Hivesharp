using Hivesharp.Abstractions.Rag;
using Microsoft.Extensions.DependencyInjection;

namespace Hivesharp.Rag;

public sealed class RagPipelineBuilder(IServiceProvider serviceProvider)
{
    private string _indexName = "default";
    private int _dimensions = 1536;
    private IVectorStore? _vectorStore;
    private ITextEmbedder? _embedder;
    private int _chunkSize = 512;
    private int _chunkOverlap = 50;
    private IChunkingStrategy? _chunkingStrategy;

    public RagPipelineBuilder WithIndex(string indexName, int dimensions = 1536)
    {
        _indexName = indexName;
        _dimensions = dimensions;
        return this;
    }

    public RagPipelineBuilder WithVectorStore(IVectorStore store)
    {
        _vectorStore = store;
        return this;
    }

    public RagPipelineBuilder WithVectorStore(Type storeType)
        => WithVectorStore(Resolve<IVectorStore>(storeType));

    public RagPipelineBuilder WithVectorStore<TStore>() where TStore : class, IVectorStore
        => WithVectorStore(typeof(TStore));

    public RagPipelineBuilder WithEmbedder(ITextEmbedder embedder)
    {
        _embedder = embedder;
        return this;
    }

    public RagPipelineBuilder WithEmbedder(Type embedderType)
        => WithEmbedder(Resolve<ITextEmbedder>(embedderType));

    public RagPipelineBuilder WithEmbedder<TEmbedder>() where TEmbedder : class, ITextEmbedder
        => WithEmbedder(typeof(TEmbedder));

    public RagPipelineBuilder WithChunking(int chunkSize = 512, int chunkOverlap = 50)
    {
        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;
        return this;
    }

    public RagPipelineBuilder WithChunkingStrategy(IChunkingStrategy strategy)
    {
        _chunkingStrategy = strategy;
        return this;
    }

    public RagPipelineBuilder WithChunkingStrategy(Type strategyType)
        => WithChunkingStrategy(Resolve<IChunkingStrategy>(strategyType));

    public RagPipelineBuilder WithChunkingStrategy<TStrategy>() where TStrategy : class, IChunkingStrategy
        => WithChunkingStrategy(typeof(TStrategy));

    private T Resolve<T>(Type type) where T : class
    {
        if (!typeof(T).IsAssignableFrom(type))
            throw new ArgumentException($"Type '{type.FullName}' does not implement {typeof(T).Name}.", nameof(type));

        return (T)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type);
    }

    internal (IVectorStore vectorStore, ITextEmbedder embedder, IChunkingStrategy chunkingStrategy, string indexName, int dimensions, int chunkSize, int chunkOverlap) Build()
    {
        var vectorStore = _vectorStore
            ?? serviceProvider.GetService(typeof(IVectorStore)) as IVectorStore
            ?? throw new InvalidOperationException(
                "No IVectorStore available. Register one with AddVectorStore<T>() or pass one via WithVectorStore().");

        var embedder = _embedder
            ?? serviceProvider.GetService(typeof(ITextEmbedder)) as ITextEmbedder
            ?? throw new InvalidOperationException(
                "No ITextEmbedder available. Register one with AddTextEmbedder<T>() or pass one via WithEmbedder().");

        var chunkingStrategy = _chunkingStrategy
            ?? new global::Hivesharp.Rag.RecursiveCharacterChunker(new ChunkingOptions
            {
                ChunkSize = _chunkSize,
                ChunkOverlap = _chunkOverlap
            });

        return (vectorStore, embedder, chunkingStrategy, _indexName, _dimensions, _chunkSize, _chunkOverlap);
    }
}
