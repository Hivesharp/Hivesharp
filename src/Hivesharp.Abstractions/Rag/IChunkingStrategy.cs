namespace Hivesharp.Abstractions.Rag;

public interface IChunkingStrategy
{
    IReadOnlyList<RagChunk> Chunk(RagDocument document);
}
