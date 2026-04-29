using Hivesharp.Abstractions.Rag;
using Hivesharp.Storage.InMemory;
using Xunit;

namespace Hivesharp.Tests.Rag;

public class InMemoryVectorStoreFilterTests
{
    private static async Task<InMemoryVectorStore> Seed()
    {
        var store = new InMemoryVectorStore();
        await store.CreateIndexAsync("docs", 2);
        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "1", Embedding = [1f, 0f], Text = "alpha-pl",
                Metadata = new() { ["tenant"] = "a", ["lang"] = "pl" } },
            new VectorRecord { Id = "2", Embedding = [0.9f, 0.1f], Text = "alpha-en",
                Metadata = new() { ["tenant"] = "a", ["lang"] = "en" } },
            new VectorRecord { Id = "3", Embedding = [0.8f, 0.2f], Text = "beta-pl",
                Metadata = new() { ["tenant"] = "b", ["lang"] = "pl" } },
            new VectorRecord { Id = "4", Embedding = [0.7f, 0.3f], Text = "beta-en",
                Metadata = new() { ["tenant"] = "b", ["lang"] = "en" } },
            new VectorRecord { Id = "5", Embedding = [0.0f, 1f], Text = "no-meta",
                Metadata = new() { ["other"] = "x" } }
        ]);
        return store;
    }

    [Fact]
    public async Task Null_Filter_Returns_All_Within_TopK()
    {
        var store = await Seed();
        var results = await store.QueryAsync("docs", [1f, 0f], topK: 10, filter: null);
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task Filter_Single_Key_Equality_Returns_Subset()
    {
        var store = await Seed();
        var results = await store.QueryAsync("docs", [1f, 0f], topK: 10,
            filter: new Dictionary<string, object?> { ["tenant"] = "a" });

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("a", r.Metadata["tenant"]));
    }

    [Fact]
    public async Task Filter_Multiple_Keys_Are_Anded()
    {
        var store = await Seed();
        var results = await store.QueryAsync("docs", [1f, 0f], topK: 10,
            filter: new Dictionary<string, object?> { ["tenant"] = "b", ["lang"] = "pl" });

        var single = Assert.Single(results);
        Assert.Equal("3", single.Id);
    }

    [Fact]
    public async Task Filter_With_Missing_Key_In_Records_Returns_Empty()
    {
        var store = await Seed();
        var results = await store.QueryAsync("docs", [1f, 0f], topK: 10,
            filter: new Dictionary<string, object?> { ["nonexistent"] = "x" });

        Assert.Empty(results);
    }

    [Fact]
    public async Task TopK_Is_Applied_After_Filter()
    {
        var store = await Seed();
        // tenant=a → 2 records remain. topK=1 → expect best one only.
        var results = await store.QueryAsync("docs", [1f, 0f], topK: 1,
            filter: new Dictionary<string, object?> { ["tenant"] = "a" });

        var single = Assert.Single(results);
        Assert.Equal("1", single.Id); // [1,0] is closer to [1,0] than [0.9,0.1]
    }

    [Fact]
    public async Task Filter_Value_Is_Compared_By_Equals()
    {
        var store = new InMemoryVectorStore();
        await store.CreateIndexAsync("docs", 1);
        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "1", Embedding = [1f], Text = "x",
                Metadata = new() { ["count"] = 42 } }
        ]);

        var hit = await store.QueryAsync("docs", [1f], topK: 5,
            filter: new Dictionary<string, object?> { ["count"] = 42 });
        Assert.Single(hit);

        var miss = await store.QueryAsync("docs", [1f], topK: 5,
            filter: new Dictionary<string, object?> { ["count"] = 7 });
        Assert.Empty(miss);
    }
}
