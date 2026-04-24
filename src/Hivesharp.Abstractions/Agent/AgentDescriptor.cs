using Hivesharp.Abstractions.Mcp;

namespace Hivesharp.Abstractions.Agent;

public class AgentDescriptor
{
    public required string Id { get; init; }
    public required string Model { get; init; }
    public string? Instructions { get; init; }
    public IReadOnlyList<string> ToolNames { get; init; } = [];
    public bool HasMemory { get; init; }
    public IReadOnlyList<McpServerDescriptor> McpServers { get; init; } = [];
}
