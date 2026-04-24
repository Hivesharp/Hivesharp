using Hivesharp.Agent;
using Microsoft.Extensions.AI;
using Xunit;

namespace Hivesharp.Tests.Agent;

public class AgentExtensionsTests
{
    [Fact]
    public void MapUsage_Null_Input_Returns_Null()
    {
        UsageDetails? input = null;
        Assert.Null(input.MapUsage());
    }

    [Fact]
    public void MapUsage_Copies_Token_Counts()
    {
        var usage = new UsageDetails
        {
            InputTokenCount = 7,
            OutputTokenCount = 11,
            TotalTokenCount = 18
        };

        var mapped = usage.MapUsage();

        Assert.NotNull(mapped);
        Assert.Equal(7, mapped!.InputTokens);
        Assert.Equal(11, mapped.OutputTokens);
        Assert.Equal(18, mapped.TotalTokens);
    }

    [Fact]
    public void ExtractToolCalls_No_Function_Calls_Returns_Empty()
    {
        IList<ChatMessage> msgs = [new ChatMessage(ChatRole.Assistant, "hi")];
        Assert.Empty(msgs.ExtractToolCalls());
    }

    [Fact]
    public void ExtractToolCalls_Pairs_Call_With_Result_By_CallId()
    {
        var args = new Dictionary<string, object?> { ["city"] = "Warsaw" };
        var call = new FunctionCallContent("call-1", "get_weather", args);
        var result = new FunctionResultContent("call-1", "sunny");

        IList<ChatMessage> msgs =
        [
            new ChatMessage(ChatRole.Assistant, [call]),
            new ChatMessage(ChatRole.Tool, [result])
        ];

        var calls = msgs.ExtractToolCalls();

        var tc = Assert.Single(calls);
        Assert.Equal("get_weather", tc.ToolName);
        Assert.Equal("sunny", tc.Result);
        Assert.False(tc.IsError);
    }

    [Fact]
    public void ExtractToolCalls_Flags_Error_Result()
    {
        var call = new FunctionCallContent("c", "tool", new Dictionary<string, object?>());
        var errorResult = new FunctionResultContent("c", "[Tool 'tool' failed]");

        IList<ChatMessage> msgs =
        [
            new ChatMessage(ChatRole.Assistant, [call]),
            new ChatMessage(ChatRole.Tool, [errorResult])
        ];

        var tc = Assert.Single(msgs.ExtractToolCalls());
        Assert.True(tc.IsError);
    }

    [Fact]
    public void ExtractToolCalls_Flags_Exception_Result()
    {
        var call = new FunctionCallContent("c", "tool", new Dictionary<string, object?>());
        var failedResult = new FunctionResultContent("c", null) { Exception = new InvalidOperationException("boom") };

        IList<ChatMessage> msgs =
        [
            new ChatMessage(ChatRole.Assistant, [call]),
            new ChatMessage(ChatRole.Tool, [failedResult])
        ];

        var tc = Assert.Single(msgs.ExtractToolCalls());
        Assert.True(tc.IsError);
    }
}
