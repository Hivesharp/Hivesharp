using Hivesharp.Abstractions.Agent;
using Microsoft.Extensions.AI;

namespace Hivesharp.Agent;

internal static class AgentExtensions
{
    internal static TokenUsage? MapUsage(this UsageDetails? usage)
    {
        if (usage is null) return null;
        return new TokenUsage
        {
            InputTokens = usage.InputTokenCount,
            OutputTokens = usage.OutputTokenCount,
            TotalTokens = usage.TotalTokenCount
        };
    }

    internal static List<ToolCallInfo> ExtractToolCalls(this IList<ChatMessage> messages)
    {
        var toolCalls = new List<ToolCallInfo>();
        foreach (var msg in messages)
        {
            foreach (var content in msg.Contents)
            {
                if (content is not FunctionCallContent call) continue;

                var resultContent = messages
                    .SelectMany(m => m.Contents)
                    .OfType<FunctionResultContent>()
                    .FirstOrDefault(r => r.CallId == call.CallId);

                var isError = resultContent?.Exception is not null
                    || (resultContent?.Result is string s && s.StartsWith("[Tool '"));

                toolCalls.Add(new ToolCallInfo
                {
                    ToolName = call.Name,
                    Arguments = call.Arguments,
                    Result = resultContent?.Result,
                    IsError = isError
                });
            }
        }
        return toolCalls;
    }
}