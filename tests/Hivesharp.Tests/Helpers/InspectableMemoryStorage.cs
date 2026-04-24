using System.Collections.Concurrent;
using Hivesharp.Abstractions.Memory;

namespace Hivesharp.Tests.Helpers;

/// <summary>Minimal in-memory IMemoryStorage used to assert what the Agent saves/reads.</summary>
internal sealed class InspectableMemoryStorage : IMemoryStorage
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
        return Task.FromResult(thread);
    }

    public Task<MemoryThread?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
        => Task.FromResult(_threads.GetValueOrDefault(threadId));

    public Task<IReadOnlyList<MemoryThread>> GetThreadsByResourceAsync(string resourceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MemoryThread>>(_threads.Values.Where(t => t.ResourceId == resourceId).ToList());

    public Task DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        _threads.TryRemove(threadId, out _);
        _messages.TryRemove(threadId, out _);
        _workingMemory.TryRemove(threadId, out _);
        return Task.CompletedTask;
    }

    public Task SaveMessagesAsync(string threadId, IReadOnlyList<MemoryMessage> messages, CancellationToken cancellationToken = default)
    {
        var list = _messages.GetOrAdd(threadId, _ => []);
        lock (list) list.AddRange(messages);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MemoryMessage>> GetMessagesAsync(string threadId, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (!_messages.TryGetValue(threadId, out var list))
            return Task.FromResult<IReadOnlyList<MemoryMessage>>([]);
        IReadOnlyList<MemoryMessage> result;
        lock (list) result = limit is null ? [.. list] : list.TakeLast(limit.Value).ToList();
        return Task.FromResult(result);
    }

    public Task<string?> GetWorkingMemoryAsync(string threadId, CancellationToken cancellationToken = default)
        => Task.FromResult(_workingMemory.GetValueOrDefault(threadId));

    public Task SaveWorkingMemoryAsync(string threadId, string content, CancellationToken cancellationToken = default)
    {
        _workingMemory[threadId] = content;
        return Task.CompletedTask;
    }

    public IReadOnlyList<MemoryMessage> GetAllMessages(string threadId) =>
        _messages.TryGetValue(threadId, out var l) ? [.. l] : [];

    public string? PeekWorkingMemory(string threadId) => _workingMemory.GetValueOrDefault(threadId);
}
