using Hivesharp.Abstractions.Rag;
using Hivesharp.Rag;
using Moq;
using Xunit;

namespace Hivesharp.Tests.Rag;

public class VectorQueryToolTests
{
    private static VectorQueryTool Make(IVectorStore store, ITextEmbedder embedder, int topK = 5) => new()
    {
        VectorStore = store,
        Embedder = embedder,
        IndexName = "kb",
        TopK = topK
    };

    [Fact]
    public void Has_Expected_Name()
    {
        var tool = Make(new Mock<IVectorStore>().Object, new Mock<ITextEmbedder>().Object);
        Assert.Equal("vector_query", tool.Name);
    }

    [Fact]
    public async Task No_Results_Returns_Placeholder_Message()
    {
        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new float[] { 1f });

        var store = new Mock<IVectorStore>();
        store.Setup(s => s.QueryAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var tool = Make(store.Object, embedder.Object);
        var del = (Func<string, Task<string>>)tool.GetDelegate();

        var result = await del("anything");

        Assert.Equal("No relevant results found.", result);
    }

    [Fact]
    public async Task Results_Are_Joined_With_Separator()
    {
        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedAsync("q", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new float[] { 0.5f });

        var store = new Mock<IVectorStore>();
        store.Setup(s => s.QueryAsync("kb", It.IsAny<float[]>(), 5, It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(
             [
                 new VectorSearchResult { Id = "1", Text = "first", Score = 0.9 },
                 new VectorSearchResult { Id = "2", Text = "second", Score = 0.8 }
             ]);

        var tool = Make(store.Object, embedder.Object);
        var del = (Func<string, Task<string>>)tool.GetDelegate();

        var result = await del("q");

        Assert.Equal("first\n\n---\n\nsecond", result);
    }

    [Fact]
    public async Task Uses_Configured_TopK_When_Querying()
    {
        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new float[] { 1f });

        var store = new Mock<IVectorStore>();
        store.Setup(s => s.QueryAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var tool = Make(store.Object, embedder.Object, topK: 3);
        var del = (Func<string, Task<string>>)tool.GetDelegate();

        await del("anything");

        store.Verify(s => s.QueryAsync("kb", It.IsAny<float[]>(), 3, It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Embeds_Query_Text_Before_Search()
    {
        string? capturedText = null;
        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((t, _) => capturedText = t)
                .ReturnsAsync(new float[] { 0.1f });

        var store = new Mock<IVectorStore>();
        store.Setup(s => s.QueryAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var tool = Make(store.Object, embedder.Object);
        var del = (Func<string, Task<string>>)tool.GetDelegate();

        await del("my question");

        Assert.Equal("my question", capturedText);
    }
}
