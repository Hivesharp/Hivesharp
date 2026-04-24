namespace Hivesharp.Abstractions.Agent;

public class ToolCallInfo
{
    public required string ToolName { get; init; }
    public IDictionary<string, object?>? Arguments { get; init; }
    public object? Result { get; init; }
    public bool IsError { get; init; }
}