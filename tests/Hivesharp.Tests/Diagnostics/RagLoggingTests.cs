using Hivesharp.Abstractions.Rag;
using Hivesharp.Rag;
using Hivesharp.Tests.Helpers;
using Moq;
using Xunit;

namespace Hivesharp.Tests.Diagnostics;

public class RagLoggingTests
{
    [Fact]
    public async Task IngestAsync_Emits_Started_Chunked_Embedded_Completed()
    {
        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(v => v.HasIndexAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedManyAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<float[]> { new float[] { 0.1f } });

        var logger = new RecordingLogger<RagPipeline>();
        var pipeline = new RagPipeline(vectorStore.Object, embedder.Object,
            new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 500 }),
            indexName: "kb", dimensions: 1, logger: logger);

        await pipeline.IngestAsync(RagDocument.FromText("hello world"));

        Assert.Contains(logger.Records, r => r.EventId.Id == 4001);
        Assert.Contains(logger.Records, r => r.EventId.Id == 4002);
        Assert.Contains(logger.Records, r => r.EventId.Id == 4003);
        var completed = Assert.Single(logger.Records, r => r.EventId.Id == 4004);
        Assert.Contains("index=kb", completed.Message);
    }

    [Fact]
    public async Task IngestManyAsync_Emits_Aggregate_Completion()
    {
        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(v => v.HasIndexAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);

        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedManyAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<float[]> { new float[] { 0.1f } });

        var logger = new RecordingLogger<RagPipeline>();
        var pipeline = new RagPipeline(vectorStore.Object, embedder.Object,
            new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 500 }),
            indexName: "kb", dimensions: 1, logger: logger);

        await pipeline.IngestManyAsync(new[]
        {
            RagDocument.FromText("a"),
            RagDocument.FromText("b")
        });

        var record = Assert.Single(logger.Records, r => r.EventId.Id == 4010);
        Assert.Contains("documents=2", record.Message);
    }
}
