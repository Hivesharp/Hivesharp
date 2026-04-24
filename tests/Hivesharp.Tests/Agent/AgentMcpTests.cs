using Hivesharp.Abstractions.Agent;
using Hivesharp.Agent;
using Hivesharp.Mcp;
using Hivesharp.Tests.Helpers;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace Hivesharp.Tests.Agent;

public class AgentMcpTests
{
    private static AgentDescriptor Descriptor(params string[] serverNames) => new()
    {
        Id = "agent",
        Model = "gpt-4o",
        ToolNames = [],
        HasMemory = false,
        McpServers = serverNames.Select(n => new Abstractions.Mcp.McpServerDescriptor
        {
            Name = n,
            TransportType = "pipe"
        }).ToList()
    };

    private static IReadOnlyList<McpServerDefinition> Definitions(params string[] serverNames)
        => serverNames.Select(n => new McpServerDefinition(n, null, PipeName: n)).ToList();

    [Fact]
    public void RuntimeState_Initially_Has_All_Servers_Unavailable()
    {
        var resolver = new Mock<IMcpToolResolver>();
        var agent = new global::Hivesharp.Agent.Agent(
            Descriptor("calc", "conv"),
            null,
            new FakeChatClient(),
            mcpServers: Definitions("calc", "conv"),
            mcpToolResolver: resolver.Object);

        var state = agent.RuntimeState;

        Assert.Equal(2, state.McpServers.Count);
        Assert.All(state.McpServers, s => Assert.False(s.IsAvailable));
        Assert.All(state.McpServers, s => Assert.Empty(s.ToolNames));
        Assert.Null(state.LastInitializedAt);
    }

    [Fact]
    public async Task RuntimeState_Updated_After_First_Generate()
    {
        var resolver = new Mock<IMcpToolResolver>();
        resolver.Setup(r => r.ResolveToolsAsync(It.IsAny<IReadOnlyList<McpServerDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new McpToolResolutionResult(
                [],
                [new McpServerStatus("calc", true, ["calc_add", "calc_mul"], null)]));

        var agent = new global::Hivesharp.Agent.Agent(
            Descriptor("calc"),
            null,
            new FakeChatClient(),
            mcpServers: Definitions("calc"),
            mcpToolResolver: resolver.Object);

        await agent.GenerateAsync("hi", threadId: null);

        var state = agent.RuntimeState;
        Assert.NotNull(state.LastInitializedAt);
        Assert.Single(state.McpServers);
        Assert.True(state.McpServers[0].IsAvailable);
        Assert.Equal(["calc_add", "calc_mul"], state.McpServers[0].ToolNames);
    }

    [Fact]
    public async Task RuntimeState_Reflects_Unavailability_When_Server_Fails()
    {
        var resolver = new Mock<IMcpToolResolver>();
        resolver.Setup(r => r.ResolveToolsAsync(It.IsAny<IReadOnlyList<McpServerDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new McpToolResolutionResult(
                [],
                [new McpServerStatus("calc", false, [], "Connection refused")]));

        var agent = new global::Hivesharp.Agent.Agent(
            Descriptor("calc"),
            null,
            new FakeChatClient(),
            mcpServers: Definitions("calc"),
            mcpToolResolver: resolver.Object);

        await agent.GenerateAsync("hi", threadId: null);

        var status = agent.RuntimeState.McpServers[0];
        Assert.False(status.IsAvailable);
        Assert.Equal("Connection refused", status.UnavailableReason);
        Assert.Empty(status.ToolNames);
    }

    [Fact]
    public async Task AgentDescriptor_McpServers_Unchanged_After_Generate()
    {
        var resolver = new Mock<IMcpToolResolver>();
        resolver.Setup(r => r.ResolveToolsAsync(It.IsAny<IReadOnlyList<McpServerDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new McpToolResolutionResult(
                [],
                [new McpServerStatus("calc", true, ["calc_add"], null)]));

        var agent = new global::Hivesharp.Agent.Agent(
            Descriptor("calc"),
            null,
            new FakeChatClient(),
            mcpServers: Definitions("calc"),
            mcpToolResolver: resolver.Object);

        await agent.GenerateAsync("hi", threadId: null);

        // Descriptor is a static config snapshot — no runtime fields
        var server = agent.AgentDescriptor.McpServers[0];
        Assert.Equal("calc", server.Name);
        Assert.Equal("pipe", server.TransportType);
    }

    [Fact]
    public async Task Instructions_Contain_System_Note_For_Unavailable_Servers()
    {
        var resolver = new Mock<IMcpToolResolver>();
        resolver.Setup(r => r.ResolveToolsAsync(It.IsAny<IReadOnlyList<McpServerDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new McpToolResolutionResult(
                [],
                [new McpServerStatus("calc", false, [], "Pipe not found")]));

        var chat = new FakeChatClient();
        var agent = new global::Hivesharp.Agent.Agent(
            Descriptor("calc"),
            null,
            chat,
            mcpServers: Definitions("calc"),
            mcpToolResolver: resolver.Object);

        await agent.GenerateAsync("hi", threadId: null);

        var instructions = chat.ReceivedOptions[0]?.Instructions ?? string.Empty;
        Assert.Contains("calc", instructions);
        Assert.Contains("Pipe not found", instructions);
        Assert.Contains("NOT available", instructions);
    }

    [Fact]
    public async Task Instructions_Have_No_System_Note_When_All_Servers_Available()
    {
        var resolver = new Mock<IMcpToolResolver>();
        resolver.Setup(r => r.ResolveToolsAsync(It.IsAny<IReadOnlyList<McpServerDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new McpToolResolutionResult(
                [],
                [new McpServerStatus("calc", true, ["calc_add"], null)]));

        var chat = new FakeChatClient();
        var agent = new global::Hivesharp.Agent.Agent(
            Descriptor("calc"),
            null,
            chat,
            mcpServers: Definitions("calc"),
            mcpToolResolver: resolver.Object);

        await agent.GenerateAsync("hi", threadId: null);

        var instructions = chat.ReceivedOptions[0]?.Instructions ?? string.Empty;
        Assert.DoesNotContain("NOT available", instructions);
    }

    [Fact]
    public async Task RetryMcp_Updates_RuntimeState_For_Failed_Server()
    {
        var resolver = new Mock<IMcpToolResolver>();
        // GenerateAsync triggers: init (fail) then AutoRetryFailedMcp (also fail) — both in one call
        // Manual RetryMcpAsync recovers the server
        resolver.SetupSequence(r => r.ResolveToolsAsync(It.IsAny<IReadOnlyList<McpServerDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new McpToolResolutionResult([], [new McpServerStatus("calc", false, [], "Pipe not found")]))
            .ReturnsAsync(new McpToolResolutionResult([], [new McpServerStatus("calc", false, [], "Pipe not found")]))
            .ReturnsAsync(new McpToolResolutionResult([], [new McpServerStatus("calc", true, ["calc_add"], null)]));

        var agent = new global::Hivesharp.Agent.Agent(
            Descriptor("calc"),
            null,
            new FakeChatClient(),
            mcpServers: Definitions("calc"),
            mcpToolResolver: resolver.Object);

        await agent.GenerateAsync("hi", threadId: null);
        Assert.False(agent.RuntimeState.McpServers[0].IsAvailable);

        await agent.RetryMcpAsync();

        Assert.True(agent.RuntimeState.McpServers[0].IsAvailable);
        Assert.Equal(["calc_add"], agent.RuntimeState.McpServers[0].ToolNames);
    }

    [Fact]
    public void Agent_Without_Mcp_Has_Empty_RuntimeState()
    {
        var agent = new global::Hivesharp.Agent.Agent(
            new AgentDescriptor { Id = "a", Model = "gpt-4o", ToolNames = [], HasMemory = false },
            null,
            new FakeChatClient());

        Assert.Equal(AgentRuntimeState.Empty, agent.RuntimeState);
        Assert.Empty(agent.RuntimeState.McpServers);
        Assert.Null(agent.RuntimeState.LastInitializedAt);
    }
}
