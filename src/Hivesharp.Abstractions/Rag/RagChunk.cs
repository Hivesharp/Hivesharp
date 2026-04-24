namespace Hivesharp.Abstractions.Rag;

public class RagChunk
{
    public required string Text { get; init; }
    public int Index { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = new();
}
