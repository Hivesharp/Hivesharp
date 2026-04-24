namespace Hivesharp.Abstractions.Memory;

public class MemoryConfiguration
{
    public required IMemoryStorage Storage { get; init; }
    public MessageHistoryConfiguration MessageHistory { get; init; } = new();
    public WorkingMemoryConfiguration? WorkingMemory { get; init; }
}

public class MessageHistoryConfiguration
{
    public IMemoryStorage? Storage { get; init; }
    public int MaxMessages { get; init; } = 40;

    public IMemoryStorage ResolveStorage(IMemoryStorage fallback) => Storage ?? fallback;
}

public class WorkingMemoryConfiguration
{
    public IMemoryStorage? Storage { get; init; }
    public string? Instructions { get; init; }

    public IMemoryStorage ResolveStorage(IMemoryStorage fallback) => Storage ?? fallback;
}
