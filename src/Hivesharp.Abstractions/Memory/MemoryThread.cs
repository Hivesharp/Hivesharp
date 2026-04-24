namespace Hivesharp.Abstractions.Memory;

public class MemoryThread
{
    public required string Id { get; init; }
    public string? ResourceId { get; init; }
    public string? Title { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
