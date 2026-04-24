namespace Hivesharp.Abstractions.Rag;

public class VectorRecord
{
    public required string Id { get; init; }
    public required float[] Embedding { get; init; }
    public required string Text { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = new();
}
