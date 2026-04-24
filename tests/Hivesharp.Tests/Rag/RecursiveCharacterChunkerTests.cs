using Hivesharp.Abstractions.Rag;
using Hivesharp.Rag;
using Xunit;

namespace Hivesharp.Tests.Rag;

public class RecursiveCharacterChunkerTests
{
    [Fact]
    public void Chunk_Text_Shorter_Than_ChunkSize_Returns_Single_Chunk()
    {
        var chunker = new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 100, ChunkOverlap = 10 });
        var doc = RagDocument.FromText("short text");

        var chunks = chunker.Chunk(doc);

        Assert.Single(chunks);
        Assert.Equal("short text", chunks[0].Text);
        Assert.Equal(0, chunks[0].Index);
    }

    [Fact]
    public void Chunk_Splits_By_Double_Newline_Paragraphs()
    {
        var chunker = new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 20, ChunkOverlap = 0 });
        var doc = RagDocument.FromText("first paragraph\n\nsecond paragraph\n\nthird paragraph");

        var chunks = chunker.Chunk(doc);

        Assert.True(chunks.Count >= 2);
        for (var i = 0; i < chunks.Count; i++)
            Assert.Equal(i, chunks[i].Index);
    }

    [Fact]
    public void Chunk_Propagates_Source_Into_Metadata()
    {
        var chunker = new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 50, ChunkOverlap = 0 });
        var doc = RagDocument.FromText("some content here", source: "docs/a.md");

        var chunks = chunker.Chunk(doc);

        Assert.All(chunks, c => Assert.Equal("docs/a.md", c.Metadata["source"]));
    }

    [Fact]
    public void Chunk_Propagates_Document_Metadata_To_Each_Chunk()
    {
        var chunker = new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 10, ChunkOverlap = 0 });
        var doc = new RagDocument
        {
            Content = "aaa\n\nbbb\n\nccc",
            Metadata = new Dictionary<string, object?> { ["author"] = "alice" }
        };

        var chunks = chunker.Chunk(doc);

        Assert.All(chunks, c => Assert.Equal("alice", c.Metadata["author"]));
    }

    [Fact]
    public void Chunk_Produces_Indexes_Starting_At_Zero()
    {
        var chunker = new RecursiveCharacterChunker(new ChunkingOptions { ChunkSize = 5, ChunkOverlap = 0 });
        var doc = RagDocument.FromText("aa bb cc dd ee ff gg hh");

        var chunks = chunker.Chunk(doc);

        Assert.Equal(0, chunks[0].Index);
        Assert.Equal(chunks.Count - 1, chunks[^1].Index);
    }
}
