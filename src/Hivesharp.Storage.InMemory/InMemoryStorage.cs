using System.Collections.Concurrent;
using Hivesharp.Abstractions.Memory;

namespace Hivesharp.Storage.InMemory;

public class InMemoryStorage : IMemoryStorage
{
    private readonly ConcurrentDictionary<string, MemoryThread> _threads = new();
    private readonly ConcurrentDictionary<string, List<MemoryMessage>> _messages = new();
    private readonly ConcurrentDictionary<string, string> _workingMemory = new();

    public Task<MemoryThread> CreateThreadAsync(string? resourceId = null, string? title = null, CancellationToken cancellationToken = default)
    {
        var thread = new MemoryThread
        {
            Id = Guid.NewGuid().ToString(),
            ResourceId = resourceId,
            Title = title,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _threads[thread.Id] = thread;
        _messages[thread.Id] = [];

        return Task.FromResult(thread);
    }

    public Task<MemoryThread?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        _threads.TryGetValue(threadId, out var thread);
        return Task.FromResult(thread);
    }

    public Task<IReadOnlyList<MemoryThread>> GetThreadsByResourceAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        var threads = _threads.Values
            .Where(t => t.ResourceId == resourceId)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<MemoryThread>>(threads);
    }

    public Task DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        _threads.TryRemove(threadId, out _);
        _messages.TryRemove(threadId, out _);
        _workingMemory.TryRemove(threadId, out _);

        return Task.CompletedTask;
    }

    public Task SaveMessagesAsync(string threadId, IReadOnlyList<MemoryMessage> messages, CancellationToken cancellationToken = default)
    {
        var threadMessages = _messages.GetOrAdd(threadId, _ => []);

        lock (threadMessages)
        {
            threadMessages.AddRange(messages);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MemoryMessage>> GetMessagesAsync(string threadId, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (!_messages.TryGetValue(threadId, out var threadMessages))
            return Task.FromResult<IReadOnlyList<MemoryMessage>>([]);

        lock (threadMessages)
        {
            IReadOnlyList<MemoryMessage> result = limit.HasValue
                ? threadMessages.TakeLast(limit.Value).ToList()
                : threadMessages.ToList();

            return Task.FromResult(result);
        }
    }

    public Task<string?> GetWorkingMemoryAsync(string threadId, CancellationToken cancellationToken = default)
    {
        _workingMemory.TryGetValue(threadId, out var content);
        return Task.FromResult(content);
    }

    public Task SaveWorkingMemoryAsync(string threadId, string content, CancellationToken cancellationToken = default)
    {
        _workingMemory[threadId] = content;
        return Task.CompletedTask;
    }
}
