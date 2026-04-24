namespace Hivesharp.Abstractions.Mcp;

public class McpServerDescriptor
{
    public required string Name { get; init; }
    public required string TransportType { get; init; }
}
