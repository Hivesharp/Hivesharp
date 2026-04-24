namespace Hivesharp.Abstractions.Rag;

public class VectorSearchResult
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required double Score { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = new();
}
