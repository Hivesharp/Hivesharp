using Hivesharp.Abstractions.Memory;
using Hivesharp.Storage.Postgres;
using Xunit;

namespace Hivesharp.IntegrationTests.Storage.Postgres;

[Collection("postgres")]
public class PostgresMemoryStorageTests(PostgresFixture fixture)
{
    private async Task<PostgresMemoryStorage> NewStorage()
    {
        var (tables, _) = await fixture.InitializeSchemaAsync();
        return new PostgresMemoryStorage(fixture.DataSource, tables);
    }

    [Fact]
    public async Task CreateThread_Persists_Row()
    {
        var storage = await NewStorage();
        var thread = await storage.CreateThreadAsync(resourceId: "user-1", title: "First");

        Assert.False(string.IsNullOrEmpty(thread.Id));
        var loaded = await storage.GetThreadAsync(thread.Id);
        Assert.NotNull(loaded);
        Assert.Equal("user-1", loaded!.ResourceId);
        Assert.Equal("First", loaded.Title);
    }

    [Fact]
    public async Task CreateThread_Without_Resource_Stores_Null()
    {
        var storage = await NewStorage();
        var thread = await storage.CreateThreadAsync();

        var loaded = await storage.GetThreadAsync(thread.Id);
        Assert.NotNull(loaded);
        Assert.Null(loaded!.ResourceId);
        Assert.Null(loaded.Title);
    }

    [Fact]
    public async Task GetThread_Returns_Null_For_Missing_Id()
    {
        var storage = await NewStorage();
        Assert.Null(await storage.GetThreadAsync("does-not-exist"));
    }

    [Fact]
    public async Task GetThreadsByResource_Returns_Newest_First()
    {
        var storage = await NewStorage();
        var t1 = await storage.CreateThreadAsync(resourceId: "user-1", title: "first");
        await Task.Delay(10);
        var t2 = await storage.CreateThreadAsync(resourceId: "user-1", title: "second");
        await Task.Delay(10);
        var t3 = await storage.CreateThreadAsync(resourceId: "user-1", title: "third");
        await storage.CreateThreadAsync(resourceId: "user-2", title: "other");

        var threads = await storage.GetThreadsByResourceAsync("user-1");

        Assert.Equal(3, threads.Count);
        Assert.Equal(t3.Id, threads[0].Id);
        Assert.Equal(t2.Id, threads[1].Id);
        Assert.Equal(t1.Id, threads[2].Id);
    }

    [Fact]
    public async Task DeleteThread_Cascades_Messages_And_Working_Memory()
    {
        var storage = await NewStorage();
        var thread = await storage.CreateThreadAsync(resourceId: "user-1");
        await storage.SaveMessagesAsync(thread.Id,
        [
            new MemoryMessage { Role = "user", Content = "hi", CreatedAt = DateTimeOffset.UtcNow }
        ]);
        await storage.SaveWorkingMemoryAsync(thread.Id, "scratchpad");

        await storage.DeleteThreadAsync(thread.Id);

        Assert.Null(await storage.GetThreadAsync(thread.Id));
        Assert.Empty(await storage.GetMessagesAsync(thread.Id));
        Assert.Null(await storage.GetWorkingMemoryAsync(thread.Id));
    }

    [Fact]
    public async Task SaveMessages_Appends_In_Order()
    {
        var storage = await NewStorage();
        var thread = await storage.CreateThreadAsync();

        await storage.SaveMessagesAsync(thread.Id,
        [
            new MemoryMessage { Role = "user", Content = "one", CreatedAt = DateTimeOffset.UtcNow },
            new MemoryMessage { Role = "assistant", Content = "two", CreatedAt = DateTimeOffset.UtcNow }
        ]);
        await storage.SaveMessagesAsync(thread.Id,
        [
            new MemoryMessage { Role = "user", Content = "three", CreatedAt = DateTimeOffset.UtcNow }
        ]);

        var all = await storage.GetMessagesAsync(thread.Id);
        Assert.Equal(3, all.Count);
        Assert.Equal("one", all[0].Content);
        Assert.Equal("two", all[1].Content);
        Assert.Equal("three", all[2].Content);
    }

    [Fact]
    public async Task GetMessages_With_Limit_Returns_Last_N_Chronologically()
    {
        var storage = await NewStorage();
        var thread = await storage.CreateThreadAsync();
        await storage.SaveMessagesAsync(thread.Id, Enumerable.Range(1, 5)
            .Select(i => new MemoryMessage { Role = "user", Content = $"msg-{i}", CreatedAt = DateTimeOffset.UtcNow })
            .ToList());

        var last2 = await storage.GetMessagesAsync(thread.Id, limit: 2);

        // Result must be in chronological order (oldest of the last-N first).
        Assert.Equal(2, last2.Count);
        Assert.Equal("msg-4", last2[0].Content);
        Assert.Equal("msg-5", last2[1].Content);
    }

    [Fact]
    public async Task GetMessages_Without_Limit_Returns_All_Chronologically()
    {
        var storage = await NewStorage();
        var thread = await storage.CreateThreadAsync();
        await storage.SaveMessagesAsync(thread.Id, Enumerable.Range(1, 7)
            .Select(i => new MemoryMessage { Role = "user", Content = $"m{i}", CreatedAt = DateTimeOffset.UtcNow })
            .ToList());

        var all = await storage.GetMessagesAsync(thread.Id);

        Assert.Equal(7, all.Count);
        Assert.Equal("m1", all[0].Content);
        Assert.Equal("m7", all[6].Content);
    }

    [Fact]
    public async Task WorkingMemory_Upserts()
    {
        var storage = await NewStorage();
        var thread = await storage.CreateThreadAsync();

        Assert.Null(await storage.GetWorkingMemoryAsync(thread.Id));

        await storage.SaveWorkingMemoryAsync(thread.Id, "first");
        Assert.Equal("first", await storage.GetWorkingMemoryAsync(thread.Id));

        await storage.SaveWorkingMemoryAsync(thread.Id, "second");
        Assert.Equal("second", await storage.GetWorkingMemoryAsync(thread.Id));
    }

    [Fact]
    public async Task Cancelled_Token_Throws_OperationCanceled()
    {
        var storage = await NewStorage();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => storage.CreateThreadAsync(cancellationToken: cts.Token));
    }
}
