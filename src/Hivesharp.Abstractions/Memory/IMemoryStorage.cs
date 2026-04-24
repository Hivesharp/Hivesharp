namespace Hivesharp.Abstractions.Memory;

public interface IMemoryStorage
{
    Task<MemoryThread> CreateThreadAsync(string? resourceId = null, string? title = null, CancellationToken cancellationToken = default);
    Task<MemoryThread?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryThread>> GetThreadsByResourceAsync(string resourceId, CancellationToken cancellationToken = default);
    Task DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default);

    Task SaveMessagesAsync(string threadId, IReadOnlyList<MemoryMessage> messages, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryMessage>> GetMessagesAsync(string threadId, int? limit = null, CancellationToken cancellationToken = default);

    Task<string?> GetWorkingMemoryAsync(string threadId, CancellationToken cancellationToken = default);
    Task SaveWorkingMemoryAsync(string threadId, string content, CancellationToken cancellationToken = default);
}
