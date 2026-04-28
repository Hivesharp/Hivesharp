using Hivesharp.Abstractions.Agent;
using Hivesharp.Abstractions.Memory;
using Hivesharp.Agent;
using Hivesharp.Tests.Helpers;
using Microsoft.Extensions.AI;
using Xunit;

namespace Hivesharp.Tests.Agent;

public class AgentTests
{
    private static AgentDescriptor Descriptor(string? instructions = null) => new()
    {
        Id = "agent",
        Model = "gpt-4o",
        Instructions = instructions,
        ToolNames = [],
        HasMemory = false,
        McpServers = []
    };

    [Fact]
    public async Task Without_Memory_Uses_Simple_Path_And_Returns_Completion()
    {
        var chat = new FakeChatClient
        {
            Responder = (_, _) => new ChatResponse(new ChatMessage(ChatRole.Assistant, "hello"))
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor("You are helpful."), null, chat);

        var result = await agent.GenerateAsync("hi", threadId: null);

        Assert.Equal("hello", result.Completion);
        Assert.Null(result.ThreadId);
        Assert.Single(chat.ReceivedOptions);
        Assert.Equal("You are helpful.", chat.ReceivedOptions[0]!.Instructions);
    }

    [Fact]
    public async Task With_Memory_Auto_Creates_Thread_When_ThreadId_Null()
    {
        var chat = new FakeChatClient();
        var storage = new InspectableMemoryStorage();

        var memory = new MemoryConfiguration
        {
            Storage = storage,
            MessageHistory = new MessageHistoryConfiguration { MaxMessages = 10 }
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat, memory);

        var result = await agent.GenerateAsync("first question", threadId: null);

        Assert.NotNull(result.ThreadId);
        var persisted = storage.GetAllMessages(result.ThreadId!);
        Assert.Equal(2, persisted.Count);
        Assert.Equal("user", persisted[0].Role);
        Assert.Equal("first question", persisted[0].Content);
        Assert.Equal("assistant", persisted[1].Role);
    }

    [Fact]
    public async Task With_Memory_Loads_Message_History_Into_Chat_Messages()
    {
        var chat = new FakeChatClient();
        var storage = new InspectableMemoryStorage();

        await storage.SaveMessagesAsync("t1",
        [
            new MemoryMessage { Role = "user", Content = "earlier" },
            new MemoryMessage { Role = "assistant", Content = "ack" }
        ]);

        var memory = new MemoryConfiguration
        {
            Storage = storage,
            MessageHistory = new MessageHistoryConfiguration { MaxMessages = 40 }
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat, memory);

        await agent.GenerateAsync("new message", threadId: "t1");

        var msgs = chat.ReceivedMessages[0].ToList();
        Assert.Equal(3, msgs.Count);
        Assert.Equal("earlier", msgs[0].Text);
        Assert.Equal("ack", msgs[1].Text);
        Assert.Equal("new message", msgs[2].Text);
        Assert.Equal(ChatRole.User, msgs[2].Role);
    }

    [Fact]
    public async Task Working_Memory_Is_Injected_Into_Instructions_From_Storage()
    {
        var chat = new FakeChatClient();
        var storage = new InspectableMemoryStorage();
        await storage.SaveWorkingMemoryAsync("t1", "User likes cats.");

        var memory = new MemoryConfiguration
        {
            Storage = storage,
            WorkingMemory = new WorkingMemoryConfiguration()
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor("Base."), null, chat, memory);

        await agent.GenerateAsync("hi", threadId: "t1");

        var opts = chat.ReceivedOptions[0]!;
        Assert.Contains("Base.", opts.Instructions);
        Assert.Contains("User likes cats.", opts.Instructions!);
        Assert.Contains("## Working Memory", opts.Instructions!);
    }

    [Fact]
    public async Task Working_Memory_Tag_Is_Extracted_Saved_And_Stripped_From_Completion()
    {
        var chat = new FakeChatClient
        {
            Responder = (_, _) => new ChatResponse(
                new ChatMessage(ChatRole.Assistant,
                    "Answer: yes\n<working_memory>User prefers short replies.</working_memory>"))
        };

        var storage = new InspectableMemoryStorage();
        var memory = new MemoryConfiguration
        {
            Storage = storage,
            WorkingMemory = new WorkingMemoryConfiguration()
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat, memory);

        var result = await agent.GenerateAsync("hi", threadId: "t1");

        Assert.Equal("Answer: yes", result.Completion);
        Assert.Equal("User prefers short replies.", storage.PeekWorkingMemory("t1"));
    }

    [Fact]
    public async Task Usage_And_ToolCalls_Are_Mapped_Into_AgentResult()
    {
        var chat = new FakeChatClient
        {
            Responder = (_, _) =>
            {
                var resp = new ChatResponse(new ChatMessage(ChatRole.Assistant, "done"))
                {
                    Usage = new UsageDetails
                    {
                        InputTokenCount = 10,
                        OutputTokenCount = 3,
                        TotalTokenCount = 13
                    }
                };
                return resp;
            }
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat);

        var result = await agent.GenerateAsync("hi", threadId: null);

        Assert.NotNull(result.Usage);
        Assert.Equal(10, result.Usage!.InputTokens);
        Assert.Equal(3, result.Usage.OutputTokens);
        Assert.Equal(13, result.Usage.TotalTokens);
    }

    public sealed record Sentiment(string Label, double Confidence);

    [Fact]
    public async Task Generic_GenerateAsync_Returns_Typed_Result_From_Json_Completion()
    {
        var chat = new FakeChatClient
        {
            Responder = (_, _) => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                """{"label":"positive","confidence":0.92}"""))
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat);

        var result = await agent.GenerateAsync<Sentiment>("classify");

        Assert.True(result.IsValid);
        Assert.NotNull(result.Result);
        Assert.Equal("positive", result.Result!.Label);
        Assert.Equal(0.92, result.Result.Confidence);
        Assert.Contains("positive", result.Completion);
    }

    [Fact]
    public async Task Generic_GenerateAsync_Returns_Invalid_Result_When_Json_Malformed()
    {
        var chat = new FakeChatClient
        {
            Responder = (_, _) => new ChatResponse(new ChatMessage(ChatRole.Assistant, "not-json-at-all"))
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat);

        var result = await agent.GenerateAsync<Sentiment>("classify");

        Assert.False(result.IsValid);
        Assert.Null(result.Result);
        Assert.Equal("not-json-at-all", result.Completion);
    }

    [Fact]
    public async Task Generic_GenerateAsync_Persists_Raw_Json_Into_History()
    {
        const string json = """{"label":"neutral","confidence":0.5}""";
        var chat = new FakeChatClient
        {
            Responder = (_, _) => new ChatResponse(new ChatMessage(ChatRole.Assistant, json))
        };

        var storage = new InspectableMemoryStorage();
        var memory = new MemoryConfiguration
        {
            Storage = storage,
            MessageHistory = new MessageHistoryConfiguration { MaxMessages = 10 }
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat, memory);

        var result = await agent.GenerateAsync<Sentiment>("classify", threadId: null);

        Assert.True(result.IsValid);
        Assert.NotNull(result.ThreadId);
        var persisted = storage.GetAllMessages(result.ThreadId!);
        Assert.Equal(2, persisted.Count);
        Assert.Equal("user", persisted[0].Role);
        Assert.Equal("classify", persisted[0].Content);
        Assert.Equal("assistant", persisted[1].Role);
        Assert.Contains("neutral", persisted[1].Content);
    }

    [Fact]
    public async Task Generic_GenerateAsync_Throws_When_Working_Memory_Configured()
    {
        var chat = new FakeChatClient();
        var storage = new InspectableMemoryStorage();
        var memory = new MemoryConfiguration
        {
            Storage = storage,
            WorkingMemory = new WorkingMemoryConfiguration()
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat, memory);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => agent.GenerateAsync<Sentiment>("hi", threadId: "t1"));
    }

    [Fact]
    public async Task Cancelled_Token_Before_Chat_Returns_Empty_Completion()
    {
        var chat = new FakeChatClient
        {
            Responder = (_, _) => throw new InvalidOperationException("should not be called")
        };

        var storage = new InspectableMemoryStorage();
        var memory = new MemoryConfiguration
        {
            Storage = storage,
            MessageHistory = new MessageHistoryConfiguration()
        };

        var agent = new global::Hivesharp.Agent.Agent(Descriptor(), null, chat, memory);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await agent.GenerateAsync("hi", threadId: "t1", cts.Token);

        Assert.Equal(string.Empty, result.Completion);
    }
}
