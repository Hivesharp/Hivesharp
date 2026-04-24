using Hivesharp.Abstractions.Memory;

namespace Hivesharp.Tests.Helpers;

internal sealed class FakeMemoryStorage : IMemoryStorage
{
    public Task<MemoryThread> CreateThreadAsync(string? resourceId = null, string? title = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new MemoryThread { Id = Guid.NewGuid().ToString(), ResourceId = resourceId, Title = title });

    public Task<MemoryThread?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
        => Task.FromResult<MemoryThread?>(null);

    public Task<IReadOnlyList<MemoryThread>> GetThreadsByResourceAsync(string resourceId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MemoryThread>>([]);

    public Task DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveMessagesAsync(string threadId, IReadOnlyList<MemoryMessage> messages, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<MemoryMessage>> GetMessagesAsync(string threadId, int? limit = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MemoryMessage>>([]);

    public Task<string?> GetWorkingMemoryAsync(string threadId, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task SaveWorkingMemoryAsync(string threadId, string content, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
