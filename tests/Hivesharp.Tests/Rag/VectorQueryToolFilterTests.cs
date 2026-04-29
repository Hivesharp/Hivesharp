using Hivesharp.Abstractions.Rag;
using Hivesharp.Rag;
using Moq;
using Xunit;

namespace Hivesharp.Tests.Rag;

public class VectorQueryToolFilterTests
{
    [Fact]
    public async Task Filter_Is_Forwarded_To_VectorStore_QueryAsync()
    {
        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new float[] { 0.1f });

        IReadOnlyDictionary<string, object?>? captured = null;
        var store = new Mock<IVectorStore>();
        store.Setup(s => s.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<CancellationToken>()))
             .Callback<string, float[], int, IReadOnlyDictionary<string, object?>?, CancellationToken>(
                 (_, _, _, f, _) => captured = f)
             .ReturnsAsync([]);

        var filter = new Dictionary<string, object?> { ["tenant"] = "acme" };
        var tool = new VectorQueryTool
        {
            VectorStore = store.Object,
            Embedder = embedder.Object,
            IndexName = "kb",
            TopK = 5,
            Filter = filter
        };

        var del = (Func<string, Task<string>>)tool.GetDelegate();
        await del("any");

        Assert.NotNull(captured);
        Assert.Equal("acme", captured!["tenant"]);
    }

    [Fact]
    public async Task Null_Filter_By_Default_Sends_Null_To_Store()
    {
        var embedder = new Mock<ITextEmbedder>();
        embedder.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new float[] { 0.1f });

        IReadOnlyDictionary<string, object?>? captured = new Dictionary<string, object?>();
        var store = new Mock<IVectorStore>();
        store.Setup(s => s.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<CancellationToken>()))
             .Callback<string, float[], int, IReadOnlyDictionary<string, object?>?, CancellationToken>(
                 (_, _, _, f, _) => captured = f)
             .ReturnsAsync([]);

        var tool = new VectorQueryTool
        {
            VectorStore = store.Object,
            Embedder = embedder.Object,
            IndexName = "kb",
            TopK = 5
        };

        var del = (Func<string, Task<string>>)tool.GetDelegate();
        await del("any");

        Assert.Null(captured);
    }
}
