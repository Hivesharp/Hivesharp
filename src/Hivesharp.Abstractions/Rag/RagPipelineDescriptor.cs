namespace Hivesharp.Abstractions.Rag;

public class RagPipelineDescriptor
{
    public required string IndexName { get; init; }
    public int Dimensions { get; init; }
    public int ChunkSize { get; init; }
    public int ChunkOverlap { get; init; }

    /// <summary>
    /// Human-readable name of the active <see cref="IVectorStore"/> backend
    /// (e.g. <c>"In-memory"</c>, <c>"Postgres"</c>, <c>"Redis"</c>). Used by Studio
    /// to surface which backend is serving the RAG index.
    /// </summary>
    public string VectorStoreBackend { get; init; } = "Unknown";
}
