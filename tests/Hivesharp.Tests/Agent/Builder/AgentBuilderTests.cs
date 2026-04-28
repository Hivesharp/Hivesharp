using Hivesharp.Agent.Builder;
using Hivesharp.Agent.Contracts;
using Hivesharp.Tests.Helpers;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace Hivesharp.Tests.Agent.Builder;

public class AgentBuilderTests
{
    private static AgentBuilder CreateBuilder()
    {
        var factory = new Mock<IAgentBuilderChatClientFactory>();
        factory.Setup(f => f.GetChatClient(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(new FakeChatClient());
        return new AgentBuilder(factory.Object);
    }

    [Fact]
    public void Build_Without_Model_Throws()
    {
        var builder = CreateBuilder();
        Assert.Throws<AgentBuilderModelNotProvidedException>(() => builder.Build());
    }

    [Fact]
    public void WithModel_Combined_Format_Requires_Colon()
    {
        var builder = CreateBuilder();
        Assert.Throws<AgentBuilderModelNotProvidedException>(() => builder.WithModel("openai-gpt4"));
    }

    [Fact]
    public void WithModel_Combined_Format_Parses_Provider_And_Model()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o");
        var agent = builder.Build();
        Assert.Equal("gpt-4o", agent.AgentDescriptor.Model);
    }

    [Fact]
    public void WithTool_Invalid_Type_Throws()
    {
        var builder = CreateBuilder();
        Assert.Throws<AgentBuilderIncorrectToolTypeException>(() => builder.WithTool(typeof(NotATool)));
    }

    [Fact]
    public void WithTool_Valid_Type_Is_Registered_In_Descriptor()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o")
               .WithTool(typeof(EchoTool))
               .WithTool(typeof(NoopTool));

        var agent = builder.Build();

        Assert.Contains("echo", agent.AgentDescriptor.ToolNames);
        Assert.Contains("noop", agent.AgentDescriptor.ToolNames);
    }

    [Fact]
    public void WithTool_Duplicate_Type_Is_Not_Added_Twice()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o")
               .WithTool(typeof(EchoTool))
               .WithTool(typeof(EchoTool));

        var agent = builder.Build();

        Assert.Single(agent.AgentDescriptor.ToolNames);
    }

    [Fact]
    public void WithTool_Generic_Overload_Registers_Tool()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o")
               .WithTool<EchoTool>();

        var agent = builder.Build();

        Assert.Contains("echo", agent.AgentDescriptor.ToolNames);
    }

    [Fact]
    public void WithTool_Instance_Overload_Registers_Tool()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o")
               .WithTool(new EchoTool());

        var agent = builder.Build();

        Assert.Contains("echo", agent.AgentDescriptor.ToolNames);
    }

    [Fact]
    public void WithTool_Delegate_Overload_Registers_By_Name()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o")
               .WithTool("inline_echo", "Echoes inline.", (string text) => $"x:{text}");

        var agent = builder.Build();

        Assert.Contains("inline_echo", agent.AgentDescriptor.ToolNames);
    }

    [Fact]
    public void WithTool_Duplicate_Name_Is_Not_Added_Twice()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o")
               .WithTool(new EchoTool())
               .WithTool("echo", "different desc", (string s) => s);

        var agent = builder.Build();

        Assert.Single(agent.AgentDescriptor.ToolNames);
    }

    [Fact]
    public void WithTool_Via_ServiceProvider_Resolves_Ctor_Dependencies()
    {
        var factory = new Mock<IAgentBuilderChatClientFactory>();
        factory.Setup(f => f.GetChatClient(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(new FakeChatClient());

        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(GreetingService))).Returns(new GreetingService());

        var builder = new AgentBuilder(factory.Object, serviceProvider: sp.Object);
        builder.WithModel("openai:gpt-4o")
               .WithTool<GreetingTool>();

        var agent = builder.Build();

        Assert.Contains("greet", agent.AgentDescriptor.ToolNames);
    }

    [Fact]
    public void WithMaxSteps_Zero_Throws()
    {
        var builder = CreateBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithMaxSteps(0));
    }

    [Fact]
    public void WithMaxSteps_Negative_Throws()
    {
        var builder = CreateBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithMaxSteps(-5));
    }

    [Fact]
    public void WithId_Sets_Descriptor_Id()
    {
        var builder = CreateBuilder();
        builder.WithId("custom-id").WithModel("openai:gpt-4o");
        var agent = builder.Build();
        Assert.Equal("custom-id", agent.AgentDescriptor.Id);
    }

    [Fact]
    public void Missing_Id_Is_Auto_Generated_Guid()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o");
        var agent = builder.Build();
        Assert.True(Guid.TryParse(agent.AgentDescriptor.Id, out _));
    }

    [Fact]
    public void WithInstructions_Is_Reflected_In_Descriptor()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o").WithInstructions("You are helpful.");
        var agent = builder.Build();
        Assert.Equal("You are helpful.", agent.AgentDescriptor.Instructions);
    }

    [Fact]
    public void HasMemory_Is_True_When_MessageHistory_Configured()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o")
               .WithMessageHistoryMemory(new FakeMemoryStorage(), maxMessages: 10);
        var agent = builder.Build();
        Assert.True(agent.AgentDescriptor.HasMemory);
    }

    [Fact]
    public void HasMemory_Is_False_By_Default()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o");
        var agent = builder.Build();
        Assert.False(agent.AgentDescriptor.HasMemory);
    }

    [Fact]
    public void MessageHistory_And_WorkingMemory_Compose_With_Shared_Storage()
    {
        var storage = new FakeMemoryStorage();
        var factory = new Mock<IAgentBuilderChatClientFactory>();
        factory.Setup(f => f.GetChatClient(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(new FakeChatClient());

        var builder = new AgentBuilder(factory.Object, defaultStorage: storage);
        builder.WithModel("openai:gpt-4o")
               .WithMessageHistoryMemory(maxMessages: 12)
               .WithWorkingMemory(instructions: "Track preferences.");

        var agent = builder.Build();

        Assert.True(agent.AgentDescriptor.HasMemory);
        Assert.NotNull(agent.Memory);
        Assert.Same(storage, agent.Memory!.Storage);
        Assert.Equal(12, agent.Memory.MessageHistory.MaxMessages);
        Assert.NotNull(agent.Memory.WorkingMemory);
        Assert.Equal("Track preferences.", agent.Memory.WorkingMemory!.Instructions);
    }

    [Fact]
    public void WithMessageHistoryMemory_Generic_Resolves_Storage_From_DI()
    {
        var storage = new FakeMemoryStorage();
        var factory = new Mock<IAgentBuilderChatClientFactory>();
        factory.Setup(f => f.GetChatClient(It.IsAny<string>(), It.IsAny<string>())).Returns(new FakeChatClient());
        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(FakeMemoryStorage))).Returns(storage);

        var builder = new AgentBuilder(factory.Object, serviceProvider: sp.Object);
        builder.WithModel("openai:gpt-4o")
               .WithMessageHistoryMemory<FakeMemoryStorage>(maxMessages: 7);

        var agent = builder.Build();

        Assert.Same(storage, agent.Memory!.Storage);
        Assert.Equal(7, agent.Memory.MessageHistory.MaxMessages);
    }

    [Fact]
    public void WithMessageHistoryMemory_Type_Overload_Resolves_Same_As_Generic()
    {
        var storage = new FakeMemoryStorage();
        var factory = new Mock<IAgentBuilderChatClientFactory>();
        factory.Setup(f => f.GetChatClient(It.IsAny<string>(), It.IsAny<string>())).Returns(new FakeChatClient());
        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(FakeMemoryStorage))).Returns(storage);

        var builder = new AgentBuilder(factory.Object, serviceProvider: sp.Object);
        builder.WithModel("openai:gpt-4o")
               .WithMessageHistoryMemory(typeof(FakeMemoryStorage), maxMessages: 9);

        var agent = builder.Build();

        Assert.Same(storage, agent.Memory!.Storage);
        Assert.Equal(9, agent.Memory.MessageHistory.MaxMessages);
    }

    [Fact]
    public void WithMessageHistoryMemory_Type_Invalid_Throws()
    {
        var builder = CreateBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithMessageHistoryMemory(typeof(NotATool)));
    }

    [Fact]
    public void WithWorkingMemory_Generic_Resolves_Storage_From_DI()
    {
        var storage = new FakeMemoryStorage();
        var factory = new Mock<IAgentBuilderChatClientFactory>();
        factory.Setup(f => f.GetChatClient(It.IsAny<string>(), It.IsAny<string>())).Returns(new FakeChatClient());
        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(FakeMemoryStorage))).Returns(storage);

        var builder = new AgentBuilder(factory.Object, serviceProvider: sp.Object);
        builder.WithModel("openai:gpt-4o")
               .WithWorkingMemory<FakeMemoryStorage>(instructions: "Track.");

        var agent = builder.Build();

        Assert.Same(storage, agent.Memory!.Storage);
        Assert.Equal("Track.", agent.Memory.WorkingMemory!.Instructions);
    }

    [Fact]
    public void WithWorkingMemory_Type_Invalid_Throws()
    {
        var builder = CreateBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithWorkingMemory(typeof(NotATool)));
    }

    [Fact]
    public void McpServer_Without_Resolver_Throws()
    {
        var builder = CreateBuilder();
        builder.WithModel("openai:gpt-4o")
               .WithMcpServer("calc", "hivesharp_mcp_calc");
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void McpServer_Descriptor_Reflects_Transport_Type()
    {
        var factory = new Mock<IAgentBuilderChatClientFactory>();
        factory.Setup(f => f.GetChatClient(It.IsAny<string>(), It.IsAny<string>())).Returns(new FakeChatClient());
        var resolver = new Mock<global::Hivesharp.Mcp.IMcpToolResolver>();
        resolver.Setup(r => r.ResolveToolsAsync(It.IsAny<IReadOnlyList<global::Hivesharp.Mcp.McpServerDefinition>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new global::Hivesharp.Mcp.McpToolResolutionResult([], []));

        var builder = new AgentBuilder(factory.Object, mcpToolResolver: resolver.Object);
        builder.WithModel("openai:gpt-4o")
               .WithMcpServer("calc", "hivesharp_mcp_calc")
               .WithMcpServer("conv", new Uri("http://localhost:5002/mcp"));

        var agent = builder.Build();

        var servers = agent.AgentDescriptor.McpServers.ToList();
        Assert.Equal(2, servers.Count);
        Assert.Contains(servers, s => s.Name == "calc" && s.TransportType == "pipe");
        Assert.Contains(servers, s => s.Name == "conv" && s.TransportType == "http");
    }
}
