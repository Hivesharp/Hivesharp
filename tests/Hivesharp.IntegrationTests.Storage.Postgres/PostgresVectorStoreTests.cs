using Hivesharp.Abstractions.Rag;
using Hivesharp.Storage.Postgres;
using Npgsql;
using Xunit;

namespace Hivesharp.IntegrationTests.Storage.Postgres;

[Collection("postgres")]
public class PostgresVectorStoreTests(PostgresFixture fixture)
{
    private async Task<(PostgresVectorStore store, PostgresTableBuilder tables, PostgresStorageOptions opts)> NewStore()
    {
        var (tables, opts) = await fixture.InitializeSchemaAsync();
        return (new PostgresVectorStore(fixture.DataSource, tables, opts), tables, opts);
    }

    private async Task<bool> IndexExists(string indexName)
    {
        await using var conn = await fixture.DataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = @n);", conn);
        cmd.Parameters.AddWithValue("n", indexName);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task CreateIndex_And_HasIndex_Returns_True()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 4);

        Assert.True(await store.HasIndexAsync("docs"));
    }

    [Fact]
    public async Task CreateIndex_Is_Idempotent()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 4);
        // Must not throw.
        await store.CreateIndexAsync("docs", 4);
        Assert.True(await store.HasIndexAsync("docs"));
    }

    [Fact]
    public async Task CreateIndex_Adds_Gin_Index_On_Metadata()
    {
        var (store, tables, _) = await NewStore();
        await store.CreateIndexAsync("docs", 4);

        var ginName = tables.IndexName(tables.VectorTableUnqualified("docs"), "metadata_gin");
        Assert.True(await IndexExists(ginName));
    }

    [Fact]
    public async Task DeleteIndex_Drops_Table()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 4);
        await store.DeleteIndexAsync("docs");

        Assert.False(await store.HasIndexAsync("docs"));
    }

    [Fact]
    public async Task Upsert_Inserts_Then_Updates_OnConflict()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 2);

        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "1", Embedding = [1f, 0f], Text = "first", Metadata = new() { ["v"] = 1 } }
        ]);
        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "1", Embedding = [1f, 0f], Text = "first-updated", Metadata = new() { ["v"] = 2 } }
        ]);

        var results = await store.QueryAsync("docs", [1f, 0f], topK: 5);
        var single = Assert.Single(results);
        Assert.Equal("first-updated", single.Text);
    }

    [Fact]
    public async Task Query_Returns_TopK_By_Cosine_Similarity_Highest_First()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 2);

        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "near", Embedding = [1f, 0f], Text = "n", Metadata = new() },
            new VectorRecord { Id = "mid", Embedding = [0.7f, 0.7f], Text = "m", Metadata = new() },
            new VectorRecord { Id = "far", Embedding = [0f, 1f], Text = "f", Metadata = new() }
        ]);

        var results = await store.QueryAsync("docs", [1f, 0f], topK: 3);

        Assert.Equal(3, results.Count);
        Assert.Equal("near", results[0].Id);
        Assert.Equal("mid", results[1].Id);
        Assert.Equal("far", results[2].Id);
        // Score = 1 - cosine_distance, higher is better
        Assert.True(results[0].Score > results[1].Score);
        Assert.True(results[1].Score > results[2].Score);
    }

    [Fact]
    public async Task Query_Without_Filter_Returns_All_Within_TopK()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 2);
        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "1", Embedding = [1f, 0f], Text = "a", Metadata = new() { ["t"] = "x" } },
            new VectorRecord { Id = "2", Embedding = [0.9f, 0.1f], Text = "b", Metadata = new() { ["t"] = "y" } }
        ]);

        var results = await store.QueryAsync("docs", [1f, 0f], topK: 10, filter: null);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Query_With_Single_Key_Filter_Returns_Subset()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 2);
        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "1", Embedding = [1f, 0f], Text = "a", Metadata = new() { ["tenant"] = "a" } },
            new VectorRecord { Id = "2", Embedding = [0.9f, 0.1f], Text = "b", Metadata = new() { ["tenant"] = "b" } }
        ]);

        var results = await store.QueryAsync("docs", [1f, 0f], topK: 10,
            filter: new Dictionary<string, object?> { ["tenant"] = "a" });

        var single = Assert.Single(results);
        Assert.Equal("1", single.Id);
    }

    [Fact]
    public async Task Query_With_Multi_Key_Filter_AND_Semantics()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 2);
        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "1", Embedding = [1f, 0f], Text = "a-pl", Metadata = new() { ["tenant"] = "a", ["lang"] = "pl" } },
            new VectorRecord { Id = "2", Embedding = [0.9f, 0.1f], Text = "a-en", Metadata = new() { ["tenant"] = "a", ["lang"] = "en" } },
            new VectorRecord { Id = "3", Embedding = [0.8f, 0.2f], Text = "b-pl", Metadata = new() { ["tenant"] = "b", ["lang"] = "pl" } }
        ]);

        var results = await store.QueryAsync("docs", [1f, 0f], topK: 10,
            filter: new Dictionary<string, object?> { ["tenant"] = "a", ["lang"] = "pl" });

        var single = Assert.Single(results);
        Assert.Equal("1", single.Id);
    }

    [Fact]
    public async Task Query_With_Filter_Applies_Before_TopK()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 2);

        // 6 records with tenant=a (varying distance), 2 with tenant=b (closest).
        // With topK=3 and filter=tenant=a, we must get 3 closest A-rows, NOT topK from full set then filter.
        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "b-near1", Embedding = [1.0f, 0f],   Text = "b1", Metadata = new() { ["tenant"] = "b" } },
            new VectorRecord { Id = "b-near2", Embedding = [0.99f, 0f],  Text = "b2", Metadata = new() { ["tenant"] = "b" } },
            new VectorRecord { Id = "a1", Embedding = [0.95f, 0.31f], Text = "a1", Metadata = new() { ["tenant"] = "a" } },
            new VectorRecord { Id = "a2", Embedding = [0.90f, 0.43f], Text = "a2", Metadata = new() { ["tenant"] = "a" } },
            new VectorRecord { Id = "a3", Embedding = [0.80f, 0.60f], Text = "a3", Metadata = new() { ["tenant"] = "a" } },
            new VectorRecord { Id = "a4", Embedding = [0.70f, 0.71f], Text = "a4", Metadata = new() { ["tenant"] = "a" } },
            new VectorRecord { Id = "a5", Embedding = [0.50f, 0.86f], Text = "a5", Metadata = new() { ["tenant"] = "a" } },
            new VectorRecord { Id = "a6", Embedding = [0.20f, 0.97f], Text = "a6", Metadata = new() { ["tenant"] = "a" } }
        ]);

        var results = await store.QueryAsync("docs", [1f, 0f], topK: 3,
            filter: new Dictionary<string, object?> { ["tenant"] = "a" });

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal("a", r.Metadata["tenant"]?.ToString()));
        // Closest a-records first.
        Assert.Equal("a1", results[0].Id);
        Assert.Equal("a2", results[1].Id);
        Assert.Equal("a3", results[2].Id);
    }

    [Fact]
    public async Task Query_With_Empty_Filter_Behaves_Like_Null()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 2);
        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "1", Embedding = [1f, 0f], Text = "a", Metadata = new() { ["t"] = "x" } }
        ]);

        var withEmpty = await store.QueryAsync("docs", [1f, 0f], topK: 10,
            filter: new Dictionary<string, object?>());
        var withNull = await store.QueryAsync("docs", [1f, 0f], topK: 10, filter: null);

        Assert.Equal(withNull.Count, withEmpty.Count);
        Assert.Single(withEmpty);
    }

    [Fact]
    public async Task Delete_Removes_Records_By_Id()
    {
        var (store, _, _) = await NewStore();
        await store.CreateIndexAsync("docs", 2);
        await store.UpsertAsync("docs",
        [
            new VectorRecord { Id = "1", Embedding = [1f, 0f], Text = "a", Metadata = new() },
            new VectorRecord { Id = "2", Embedding = [0.9f, 0.1f], Text = "b", Metadata = new() }
        ]);

        await store.DeleteAsync("docs", ["1"]);

        var results = await store.QueryAsync("docs", [1f, 0f], topK: 5);
        var single = Assert.Single(results);
        Assert.Equal("2", single.Id);
    }

    [Fact]
    public async Task Cancelled_Token_Throws_OperationCanceled()
    {
        var (store, _, _) = await NewStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.CreateIndexAsync("docs", 2, cts.Token));
    }

    [Fact]
    public async Task Invalid_Index_Name_Throws_From_TableBuilder()
    {
        var (store, _, _) = await NewStore();
        await Assert.ThrowsAsync<ArgumentException>(() => store.CreateIndexAsync("docs-bad", 2));
    }
}
