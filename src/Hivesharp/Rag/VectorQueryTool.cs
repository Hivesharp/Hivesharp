using Hivesharp.Abstractions.Rag;
using Hivesharp.Abstractions.Tool;

namespace Hivesharp.Rag;

internal sealed class VectorQueryTool : ITool
{
    internal required IVectorStore VectorStore { get; init; }
    internal required ITextEmbedder Embedder { get; init; }
    internal required string IndexName { get; init; }
    internal int TopK { get; init; } = 5;

    public string Name => "vector_query";
    public string? Description { get; init; }

    public Delegate GetDelegate() => async (string query) =>
    {
        var embedding = await Embedder.EmbedAsync(query);
        var results = await VectorStore.QueryAsync(IndexName, embedding, TopK);

        if (results.Count == 0)
            return "No relevant results found.";

        return string.Join("\n\n---\n\n", results.Select(r => r.Text));
    };
}
