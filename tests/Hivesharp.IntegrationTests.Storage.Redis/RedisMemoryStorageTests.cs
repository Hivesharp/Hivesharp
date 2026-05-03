using Hivesharp.Abstractions.Memory;
using Hivesharp.Storage.Redis;
using Xunit;

namespace Hivesharp.IntegrationTests.Storage.Redis;

[Collection("redis")]
public class RedisMemoryStorageTests(RedisFixture fixture)
{
    private RedisMemoryStorage NewStorage() =>
        new(fixture.Multiplexer, new RedisKeyBuilder(RedisFixture.NewPrefix()));

    [Fact]
    public async Task CreateThread_Persists_Hash_Fields()
    {
        var storage = NewStorage();

        var thread = await storage.CreateThreadAsync(resourceId: "user-1", title: "First");

        Assert.False(string.IsNullOrEmpty(thread.Id));
        Assert.Equal("user-1", thread.ResourceId);
        Assert.Equal("First", thread.Title);

        var loaded = await storage.GetThreadAsync(thread.Id);
        Assert.NotNull(loaded);
        Assert.Equal(thread.Id, loaded!.Id);
        Assert.Equal("user-1", loaded.ResourceId);
        Assert.Equal("First", loaded.Title);
        Assert.True((DateTimeOffset.UtcNow - loaded.CreatedAt).Duration() < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CreateThread_Without_Resource_Skips_Index()
    {
        var storage = NewStorage();

        var thread = await storage.CreateThreadAsync();

        Assert.Null(thread.ResourceId);
        // No resource → not retrievable through GetThreadsByResourceAsync (covered separately)
        var loaded = await storage.GetThreadAsync(thread.Id);
        Assert.NotNull(loaded);
        Assert.Null(loaded!.ResourceId);
    }

    [Fact]
    public async Task GetThread_Returns_Null_For_Missing_Id()
    {
        var storage = NewStorage();
        var loaded = await storage.GetThreadAsync("does-not-exist");
        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetThreadsByResource_Returns_Newest_First()
    {
        var storage = NewStorage();
        var t1 = await storage.CreateThreadAsync(resourceId: "user-1", title: "first");
        await Task.Delay(5);
        var t2 = await storage.CreateThreadAsync(resourceId: "user-1", title: "second");
        await Task.Delay(5);
        var t3 = await storage.CreateThreadAsync(resourceId: "user-1", title: "third");

        // Different resource — must not appear
        await storage.CreateThreadAsync(resourceId: "user-2", title: "other");

        var threads = await storage.GetThreadsByResourceAsync("user-1");

        Assert.Equal(3, threads.Count);
        Assert.Equal(t3.Id, threads[0].Id);
        Assert.Equal(t2.Id, threads[1].Id);
        Assert.Equal(t1.Id, threads[2].Id);
    }

    [Fact]
    public async Task DeleteThread_Removes_All_Related_Keys()
    {
        var storage = NewStorage();
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
        var resourceThreads = await storage.GetThreadsByResourceAsync("user-1");
        Assert.DoesNotContain(resourceThreads, t => t.Id == thread.Id);
    }

    [Fact]
    public async Task SaveMessages_Appends_To_List_In_Order()
    {
        var storage = NewStorage();
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
    public async Task GetMessages_With_Limit_Returns_Last_N()
    {
        var storage = NewStorage();
        var thread = await storage.CreateThreadAsync();

        await storage.SaveMessagesAsync(thread.Id, Enumerable.Range(1, 5)
            .Select(i => new MemoryMessage { Role = "user", Content = $"msg-{i}", CreatedAt = DateTimeOffset.UtcNow })
            .ToList());

        var last2 = await storage.GetMessagesAsync(thread.Id, limit: 2);

        Assert.Equal(2, last2.Count);
        Assert.Equal("msg-4", last2[0].Content);
        Assert.Equal("msg-5", last2[1].Content);
    }

    [Fact]
    public async Task GetMessages_Without_Limit_Returns_All()
    {
        var storage = NewStorage();
        var thread = await storage.CreateThreadAsync();
        await storage.SaveMessagesAsync(thread.Id, Enumerable.Range(1, 7)
            .Select(i => new MemoryMessage { Role = "user", Content = $"m{i}", CreatedAt = DateTimeOffset.UtcNow })
            .ToList());

        var all = await storage.GetMessagesAsync(thread.Id);

        Assert.Equal(7, all.Count);
    }

    [Fact]
    public async Task WorkingMemory_RoundTrip()
    {
        var storage = NewStorage();
        var thread = await storage.CreateThreadAsync();

        Assert.Null(await storage.GetWorkingMemoryAsync(thread.Id));

        await storage.SaveWorkingMemoryAsync(thread.Id, "User likes cats.");
        Assert.Equal("User likes cats.", await storage.GetWorkingMemoryAsync(thread.Id));

        await storage.SaveWorkingMemoryAsync(thread.Id, "User now likes dogs.");
        Assert.Equal("User now likes dogs.", await storage.GetWorkingMemoryAsync(thread.Id));
    }

    [Fact]
    public async Task Concurrent_SaveMessages_Preserves_All()
    {
        var storage = NewStorage();
        var thread = await storage.CreateThreadAsync();

        var tasks = Enumerable.Range(0, 10).Select(i =>
            storage.SaveMessagesAsync(thread.Id,
            [
                new MemoryMessage { Role = "user", Content = $"a{i}", CreatedAt = DateTimeOffset.UtcNow },
                new MemoryMessage { Role = "assistant", Content = $"b{i}", CreatedAt = DateTimeOffset.UtcNow }
            ])).ToArray();

        await Task.WhenAll(tasks);

        var all = await storage.GetMessagesAsync(thread.Id);
        Assert.Equal(20, all.Count);
    }

    [Fact]
    public async Task Cancelled_Token_Throws_OperationCanceled()
    {
        var storage = NewStorage();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => storage.CreateThreadAsync(cancellationToken: cts.Token));
    }
}
