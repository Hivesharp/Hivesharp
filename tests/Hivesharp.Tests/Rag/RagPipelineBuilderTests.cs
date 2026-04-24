using Hivesharp.Abstractions.Rag;
using Hivesharp.Rag;
using Moq;
using Xunit;

namespace Hivesharp.Tests.Rag;

public class RagPipelineBuilderTests
{
    private sealed class TinyServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];
        public TinyServiceProvider Register(Type t, object instance) { _services[t] = instance; return this; }
        public object? GetService(Type serviceType) => _services.GetValueOrDefault(serviceType);
    }

    [Fact]
    public void Build_Without_VectorStore_Throws_Descriptive_Error()
    {
        var services = new TinyServiceProvider()
            .Register(typeof(ITextEmbedder), new Mock<ITextEmbedder>().Object);

        var builder = new RagPipelineBuilder(services).WithIndex("kb");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("IVectorStore", ex.Message);
    }

    [Fact]
    public void Build_Without_Embedder_Throws_Descriptive_Error()
    {
        var services = new TinyServiceProvider()
            .Register(typeof(IVectorStore), new Mock<IVectorStore>().Object);

        var builder = new RagPipelineBuilder(services).WithIndex("kb");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("ITextEmbedder", ex.Message);
    }

    [Fact]
    public void Build_Resolves_VectorStore_And_Embedder_From_DI()
    {
        var vs = new Mock<IVectorStore>().Object;
        var emb = new Mock<ITextEmbedder>().Object;
        var services = new TinyServiceProvider()
            .Register(typeof(IVectorStore), vs)
            .Register(typeof(ITextEmbedder), emb);

        var builder = new RagPipelineBuilder(services).WithIndex("kb", dimensions: 768);
        var (vectorStore, embedder, chunker, index, dims, size, overlap) = builder.Build();

        Assert.Same(vs, vectorStore);
        Assert.Same(emb, embedder);
        Assert.Equal("kb", index);
        Assert.Equal(768, dims);
        Assert.IsType<RecursiveCharacterChunker>(chunker);
        Assert.Equal(512, size);
        Assert.Equal(50, overlap);
    }

    [Fact]
    public void Explicit_WithVectorStore_And_WithEmbedder_Override_DI()
    {
        var diVs = new Mock<IVectorStore>().Object;
        var diEmb = new Mock<ITextEmbedder>().Object;
        var services = new TinyServiceProvider()
            .Register(typeof(IVectorStore), diVs)
            .Register(typeof(ITextEmbedder), diEmb);

        var explicitVs = new Mock<IVectorStore>().Object;
        var explicitEmb = new Mock<ITextEmbedder>().Object;

        var (vectorStore, embedder, _, _, _, _, _) = new RagPipelineBuilder(services)
            .WithVectorStore(explicitVs)
            .WithEmbedder(explicitEmb)
            .Build();

        Assert.Same(explicitVs, vectorStore);
        Assert.Same(explicitEmb, embedder);
    }

    [Fact]
    public void WithChunking_Customizes_Size_And_Overlap()
    {
        var services = new TinyServiceProvider()
            .Register(typeof(IVectorStore), new Mock<IVectorStore>().Object)
            .Register(typeof(ITextEmbedder), new Mock<ITextEmbedder>().Object);

        var (_, _, _, _, _, size, overlap) = new RagPipelineBuilder(services)
            .WithChunking(chunkSize: 1000, chunkOverlap: 200)
            .Build();

        Assert.Equal(1000, size);
        Assert.Equal(200, overlap);
    }

    [Fact]
    public void WithChunkingStrategy_Replaces_Default_Chunker()
    {
        var services = new TinyServiceProvider()
            .Register(typeof(IVectorStore), new Mock<IVectorStore>().Object)
            .Register(typeof(ITextEmbedder), new Mock<ITextEmbedder>().Object);

        var customChunker = new Mock<IChunkingStrategy>().Object;

        var (_, _, chunker, _, _, _, _) = new RagPipelineBuilder(services)
            .WithChunkingStrategy(customChunker)
            .Build();

        Assert.Same(customChunker, chunker);
    }
}
