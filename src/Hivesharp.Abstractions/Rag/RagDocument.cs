namespace Hivesharp.Abstractions.Rag;

public class RagDocument
{
    public required string Content { get; init; }
    public string? Source { get; init; }
    public string? MimeType { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = new();

    public static RagDocument FromText(string text, string? source = null)
        => new() { Content = text, Source = source, MimeType = "text/plain" };

    public static RagDocument FromMarkdown(string markdown, string? source = null)
        => new() { Content = markdown, Source = source, MimeType = "text/markdown" };

    public static RagDocument FromHtml(string html, string? source = null)
        => new() { Content = html, Source = source, MimeType = "text/html" };
}
