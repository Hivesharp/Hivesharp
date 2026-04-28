using Hivesharp.Abstractions.Agent;
using Hivesharp.Tests.Helpers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Hivesharp.Tests.Diagnostics;

public class AgentLoggingTests
{
    private static AgentDescriptor Descriptor(string id = "agent") => new()
    {
        Id = id,
        Model = "gpt-4o",
        Instructions = null,
        ToolNames = [],
        HasMemory = false,
        McpServers = []
    };

    [Fact]
    public async Task Simple_Generate_Emits_Started_And_Completed()
    {
        var chat = new FakeChatClient
        {
            Responder = (_, _) => new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi"))
            {
                Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5 }
            }
        };
        var logger = new RecordingLogger<global::Hivesharp.Agent.Agent>();

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat, logger: logger);
        await agent.GenerateAsync("hello", threadId: null);

        Assert.Contains(logger.Records, r => r.EventId.Id == 1011);
        var completed = Assert.Single(logger.Records, r => r.EventId.Id == 1012);
        Assert.Equal(LogLevel.Information, completed.Level);
        Assert.Contains("promptTokens=10", completed.Message);
        Assert.Contains("completionTokens=5", completed.Message);
    }

    [Fact]
    public async Task Simple_Generate_Emits_Failed_On_Exception()
    {
        var chat = new FakeChatClient
        {
            Responder = (_, _) => throw new InvalidOperationException("boom")
        };
        var logger = new RecordingLogger<global::Hivesharp.Agent.Agent>();

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat, logger: logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => agent.GenerateAsync("hi", null));

        var failed = Assert.Single(logger.Records, r => r.EventId.Id == 1013);
        Assert.Equal(LogLevel.Error, failed.Level);
        Assert.IsType<InvalidOperationException>(failed.Exception);
    }
}
