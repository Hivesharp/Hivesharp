namespace Hivesharp.Abstractions.Agent;

public class AgentResult
{
    public required string Completion { get; init; }
    public string? ThreadId { get; init; }
    public TokenUsage? Usage { get; init; }
    public IReadOnlyList<ToolCallInfo> ToolCalls { get; init; } = [];
}