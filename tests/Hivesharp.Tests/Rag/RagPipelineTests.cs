using Hivesharp.Abstractions.Rag;
using Hivesharp.Rag;
using Moq;
using Xunit;

namespace Hivesharp.Tests.Rag;

public class RagPipelineTests
{
    [Fact]
    public async Task IngestAsync_Creates_Index_When_Missing()
    {
        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(v => v.HasIndexAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false);

        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedManyAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<float[]> { new float[] { 0.1f, 0.2f } });

        var pipeline = new RagPipeline(vectorStore.Object, embedder.Object,
            new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 500 }),
            indexName: "kb", dimensions: 2);

        await pipeline.IngestAsync(RagDocument.FromText("hello"));

        vectorStore.Verify(v => v.CreateIndexAsync("kb", 2, It.IsAny<CancellationToken>()), Times.Once);
        vectorStore.Verify(v => v.UpsertAsync("kb",
            It.Is<IReadOnlyList<VectorRecord>>(r => r.Count == 1 && r[0].Text == "hello"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_Skips_Create_When_Index_Exists()
    {
        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(v => v.HasIndexAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedManyAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<float[]> { new float[] { 1f } });

        var pipeline = new RagPipeline(vectorStore.Object, embedder.Object,
            new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 500 }),
            indexName: "kb", dimensions: 1);

        await pipeline.IngestAsync(RagDocument.FromText("x"));

        vectorStore.Verify(v => v.CreateIndexAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        vectorStore.Verify(v => v.UpsertAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<VectorRecord>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_Passes_Chunk_Texts_To_Embedder()
    {
        IReadOnlyList<string>? capturedTexts = null;

        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(v => v.HasIndexAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedManyAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<string>, CancellationToken>((t, _) => capturedTexts = t)
                .ReturnsAsync((IReadOnlyList<string> t, CancellationToken _) =>
                    t.Select(_ => new float[] { 1f }).ToList());

        var chunker = new Mock<IChunkingStrategy>();
        chunker.Setup(c => c.Chunk(It.IsAny<RagDocument>())).Returns(
            [
                new RagChunk { Text = "chunk-a", Index = 0, Metadata = new() },
                new RagChunk { Text = "chunk-b", Index = 1, Metadata = new() },
            ]);

        var pipeline = new RagPipeline(vectorStore.Object, embedder.Object, chunker.Object, "kb", 1);

        await pipeline.IngestAsync(RagDocument.FromText("anything"));

        Assert.NotNull(capturedTexts);
        Assert.Equal(["chunk-a", "chunk-b"], capturedTexts);
    }

    [Fact]
    public async Task IngestAsync_With_Empty_Chunks_Does_Nothing()
    {
        var vectorStore = new Mock<IVectorStore>();
        var embedder = new Mock<ITextEmbedder>();
        var chunker = new Mock<IChunkingStrategy>();
        chunker.Setup(c => c.Chunk(It.IsAny<RagDocument>())).Returns(Array.Empty<RagChunk>());

        var pipeline = new RagPipeline(vectorStore.Object, embedder.Object, chunker.Object, "kb", 1);

        await pipeline.IngestAsync(RagDocument.FromText(""));

        vectorStore.VerifyNoOtherCalls();
        embedder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IngestManyAsync_Processes_All_Documents()
    {
        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(v => v.HasIndexAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedManyAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<string> t, CancellationToken _) =>
                    t.Select(_ => new float[] { 1f }).ToList());

        var pipeline = new RagPipeline(vectorStore.Object, embedder.Object,
            new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 500 }),
            indexName: "kb", dimensions: 1);

        await pipeline.IngestManyAsync(
            [RagDocument.FromText("a"), RagDocument.FromText("b"), RagDocument.FromText("c")]);

        vectorStore.Verify(v => v.UpsertAsync("kb", It.IsAny<IReadOnlyList<VectorRecord>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }
}
