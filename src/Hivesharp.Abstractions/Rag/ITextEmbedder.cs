namespace Hivesharp.Abstractions.Rag;

public interface ITextEmbedder
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
