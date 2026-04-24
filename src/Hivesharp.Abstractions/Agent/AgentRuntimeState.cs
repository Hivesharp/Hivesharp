namespace Hivesharp.Abstractions.Agent;

public sealed record McpServerStatus(
    string Name,
    bool IsAvailable,
    IReadOnlyList<string> ToolNames,
    string? UnavailableReason);

public sealed record AgentRuntimeState(
    IReadOnlyList<McpServerStatus> McpServers,
    DateTimeOffset? LastInitializedAt)
{
    public static AgentRuntimeState Empty { get; } = new([], null);
}
