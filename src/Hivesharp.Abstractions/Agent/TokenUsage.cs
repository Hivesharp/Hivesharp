namespace Hivesharp.Abstractions.Agent;

public class TokenUsage
{
    public long? InputTokens { get; init; }
    public long? OutputTokens { get; init; }
    public long? TotalTokens { get; init; }
}