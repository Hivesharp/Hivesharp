using System.Text.Json;
using Hivesharp.Abstractions.Memory;
using StackExchange.Redis;

namespace Hivesharp.Storage.Redis;

internal sealed class RedisMemoryStorage(IConnectionMultiplexer multiplexer, RedisKeyBuilder keys) : IMemoryStorage
{
    private IDatabase Db => multiplexer.GetDatabase();

    public async Task<MemoryThread> CreateThreadAsync(string? resourceId = null, string? title = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var thread = new MemoryThread
        {
            Id = Guid.NewGuid().ToString(),
            ResourceId = resourceId,
            Title = title,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var entries = new List<HashEntry>
        {
            new("id", thread.Id),
            new("createdAt", thread.CreatedAt.ToString("O"))
        };
        if (resourceId is not null) entries.Add(new HashEntry("resourceId", resourceId));
        if (title is not null) entries.Add(new HashEntry("title", title));

        var db = Db;
        var tx = db.CreateTransaction();
        _ = tx.HashSetAsync(keys.Thread(thread.Id), [.. entries]);
        if (resourceId is not null)
        {
            _ = tx.SortedSetAddAsync(keys.ResourceThreadsIndex(resourceId), thread.Id, thread.CreatedAt.ToUnixTimeMilliseconds());
        }
        await tx.ExecuteAsync();

        return thread;
    }

    public async Task<MemoryThread?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hash = await Db.HashGetAllAsync(keys.Thread(threadId));
        return ToThread(hash);
    }

    public async Task<IReadOnlyList<MemoryThread>> GetThreadsByResourceAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = Db;
        var ids = await db.SortedSetRangeByScoreAsync(
            keys.ResourceThreadsIndex(resourceId),
            order: Order.Descending);

        if (ids.Length == 0) return [];

        var tasks = ids.Select(id => db.HashGetAllAsync(keys.Thread(id!))).ToArray();
        var hashes = await Task.WhenAll(tasks);

        var result = new List<MemoryThread>(hashes.Length);
        foreach (var hash in hashes)
        {
            var thread = ToThread(hash);
            if (thread is not null) result.Add(thread);
        }
        return result;
    }

    public async Task DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = Db;
        var resourceIdValue = await db.HashGetAsync(keys.Thread(threadId), "resourceId");

        var tx = db.CreateTransaction();
        _ = tx.KeyDeleteAsync(keys.Thread(threadId));
        _ = tx.KeyDeleteAsync(keys.ThreadMessages(threadId));
        _ = tx.KeyDeleteAsync(keys.ThreadWorkingMemory(threadId));
        if (resourceIdValue.HasValue)
        {
            _ = tx.SortedSetRemoveAsync(keys.ResourceThreadsIndex(resourceIdValue!), threadId);
        }
        await tx.ExecuteAsync();
    }

    public async Task SaveMessagesAsync(string threadId, IReadOnlyList<MemoryMessage> messages, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (messages.Count == 0) return;

        var values = new RedisValue[messages.Count];
        for (var i = 0; i < messages.Count; i++)
        {
            values[i] = JsonSerializer.Serialize(messages[i]);
        }

        await Db.ListRightPushAsync(keys.ThreadMessages(threadId), values);
    }

    public async Task<IReadOnlyList<MemoryMessage>> GetMessagesAsync(string threadId, int? limit = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = keys.ThreadMessages(threadId);
        RedisValue[] values = limit.HasValue
            ? await Db.ListRangeAsync(key, -limit.Value, -1)
            : await Db.ListRangeAsync(key);

        if (values.Length == 0) return [];

        var result = new List<MemoryMessage>(values.Length);
        foreach (var value in values)
        {
            if (!value.HasValue) continue;
            var message = JsonSerializer.Deserialize<MemoryMessage>((string)value!);
            if (message is not null) result.Add(message);
        }
        return result;
    }

    public async Task<string?> GetWorkingMemoryAsync(string threadId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await Db.StringGetAsync(keys.ThreadWorkingMemory(threadId));
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SaveWorkingMemoryAsync(string threadId, string content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.StringSetAsync(keys.ThreadWorkingMemory(threadId), content);
    }

    private static MemoryThread? ToThread(HashEntry[] hash)
    {
        if (hash.Length == 0) return null;

        string? id = null;
        string? resourceId = null;
        string? title = null;
        DateTimeOffset createdAt = default;

        foreach (var entry in hash)
        {
            switch (entry.Name.ToString())
            {
                case "id": id = entry.Value.ToString(); break;
                case "resourceId": resourceId = entry.Value.ToString(); break;
                case "title": title = entry.Value.ToString(); break;
                case "createdAt":
                    if (DateTimeOffset.TryParse(entry.Value.ToString(), out var parsed))
                        createdAt = parsed;
                    break;
            }
        }

        if (id is null) return null;

        return new MemoryThread
        {
            Id = id,
            ResourceId = resourceId,
            Title = title,
            CreatedAt = createdAt
        };
    }
}
