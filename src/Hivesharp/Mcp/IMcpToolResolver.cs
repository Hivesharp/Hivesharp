namespace Hivesharp.Mcp;

internal interface IMcpToolResolver : IAsyncDisposable
{
    Task<McpToolResolutionResult> ResolveToolsAsync(
        IReadOnlyList<McpServerDefinition> servers,
        CancellationToken cancellationToken = default);
}