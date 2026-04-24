using Hivesharp.Abstractions.Rag;
using Microsoft.Extensions.AI;

namespace Hivesharp.Rag;

internal sealed class MicrosoftAITextEmbedder(IEmbeddingGenerator<string, Embedding<float>> inner) : ITextEmbedder
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await inner.GenerateAsync(text, cancellationToken: cancellationToken);
        return result.Vector.ToArray();
    }

    public async Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var results = await inner.GenerateAsync(texts, cancellationToken: cancellationToken);
        return results.Select(r => r.Vector.ToArray()).ToList();
    }
}
