namespace Hivesharp.Abstractions.Rag;

public class ChunkingOptions
{
    public int ChunkSize { get; init; } = 512;
    public int ChunkOverlap { get; init; } = 50;
}
