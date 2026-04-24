using Hivesharp.Abstractions.Rag;

namespace Hivesharp.Rag;

internal sealed class RecursiveCharacterChunker(ChunkingOptions? options = null) : IChunkingStrategy
{
    private static readonly string[] DefaultSeparators = ["\n\n", "\n", ". ", " ", ""];

    private readonly int _chunkSize = options?.ChunkSize ?? 512;
    private readonly int _chunkOverlap = options?.ChunkOverlap ?? 50;

    public IReadOnlyList<RagChunk> Chunk(RagDocument document)
    {
        var texts = SplitText(document.Content, DefaultSeparators);
        var chunks = new List<RagChunk>();

        for (var i = 0; i < texts.Count; i++)
        {
            var metadata = new Dictionary<string, object?>(document.Metadata);
            if (document.Source is not null)
                metadata["source"] = document.Source;

            chunks.Add(new RagChunk
            {
                Text = texts[i],
                Index = i,
                Metadata = metadata
            });
        }

        return chunks;
    }

    private List<string> SplitText(string text, string[] separators)
    {
        if (text.Length <= _chunkSize)
            return [text];

        var separator = FindBestSeparator(text, separators);
        var remainingSeparators = separators.SkipWhile(s => s != separator).Skip(1).ToArray();
        var splits = SplitBySeparator(text, separator);
        var chunks = new List<string>();
        var currentChunk = "";

        foreach (var split in splits)
        {
            var candidate = currentChunk.Length == 0 ? split : currentChunk + separator + split;

            if (candidate.Length > _chunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk);

                    // Apply overlap
                    if (_chunkOverlap > 0 && currentChunk.Length > _chunkOverlap)
                        currentChunk = currentChunk[^_chunkOverlap..];
                    else
                        currentChunk = "";

                    candidate = currentChunk.Length == 0 ? split : currentChunk + separator + split;
                }

                // If single split is still too large, recurse with next separator
                if (candidate.Length > _chunkSize && remainingSeparators.Length > 0)
                {
                    var subChunks = SplitText(candidate, remainingSeparators);
                    chunks.AddRange(subChunks);
                    currentChunk = "";
                }
                else
                {
                    currentChunk = candidate;
                }
            }
            else
            {
                currentChunk = candidate;
            }
        }

        if (currentChunk.Length > 0)
            chunks.Add(currentChunk);

        return chunks;
    }

    private static string FindBestSeparator(string text, string[] separators)
    {
        foreach (var sep in separators)
        {
            if (sep == "" || text.Contains(sep, StringComparison.Ordinal))
                return sep;
        }

        return "";
    }

    private static List<string> SplitBySeparator(string text, string separator)
    {
        if (separator == "")
            return text.Select(c => c.ToString()).ToList();

        return text.Split(separator).Where(s => s.Length > 0).ToList();
    }
}
