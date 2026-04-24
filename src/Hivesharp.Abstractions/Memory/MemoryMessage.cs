namespace Hivesharp.Abstractions.Memory;

public class MemoryMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
