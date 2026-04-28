using Hivesharp.Abstractions.Rag;
using Hivesharp.Abstractions.Tool;
using Hivesharp.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hivesharp.Rag;

internal sealed class VectorQueryTool : ITool
{
    internal required IVectorStore VectorStore { get; init; }
    internal required ITextEmbedder Embedder { get; init; }
    internal required string IndexName { get; init; }
    internal int TopK { get; init; } = 5;
    internal ILogger? Logger { get; init; }

    public string Name => "vector_query";
    public string? Description { get; init; }

    public Delegate GetDelegate() => async (string query) =>
    {
        var logger = Logger ?? NullLogger.Instance;
        var embedding = await Embedder.EmbedAsync(query);
        var results = await VectorStore.QueryAsync(IndexName, embedding, TopK);

        RagLog.VectorQuery(logger, IndexName, TopK, query.Length, results.Count);

        if (results.Count == 0)
            return "No relevant results found.";

        return string.Join("\n\n---\n\n", results.Select(r => r.Text));
    };
}
