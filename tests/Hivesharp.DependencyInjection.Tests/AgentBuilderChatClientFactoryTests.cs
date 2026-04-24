using Hivesharp.Abstractions.AgentBuilder;
using Hivesharp.DependencyInjection;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace Hivesharp.DependencyInjection.Tests;

public class AgentBuilderChatClientFactoryTests
{
    private static Mock<IChatClientProvider> ProviderMock(string name, IChatClient? client = null)
    {
        var mock = new Mock<IChatClientProvider>();
        mock.SetupGet(p => p.Name).Returns(name);
        mock.Setup(p => p.Create(It.IsAny<string>())).Returns(client ?? new Mock<IChatClient>().Object);
        return mock;
    }

    [Fact]
    public void GetChatClient_With_Registered_Provider_Returns_Created_Client()
    {
        var expected = new Mock<IChatClient>().Object;
        var provider = ProviderMock("openai", expected);
        var factory = new AgentBuilderChatClientFactory([provider.Object]);

        var actual = factory.GetChatClient("openai", "gpt-4o");

        Assert.Same(expected, actual);
        provider.Verify(p => p.Create("gpt-4o"), Times.Once);
    }

    [Fact]
    public void GetChatClient_Is_Case_Insensitive()
    {
        var provider = ProviderMock("OpenAI");
        var factory = new AgentBuilderChatClientFactory([provider.Object]);

        var ex = Record.Exception(() => factory.GetChatClient("openai", "gpt-4o"));

        Assert.Null(ex);
        provider.Verify(p => p.Create("gpt-4o"), Times.Once);
    }

    [Fact]
    public void GetChatClient_Unknown_Provider_Throws_With_Name_And_Registered_List()
    {
        var factory = new AgentBuilderChatClientFactory(
            [ProviderMock("openai").Object, ProviderMock("anthropic").Object]);

        var ex = Assert.Throws<UnknownProviderException>(() => factory.GetChatClient("mystery", "x"));

        Assert.Equal("mystery", ex.ProviderName);
        Assert.Contains("openai", ex.RegisteredProviders);
        Assert.Contains("anthropic", ex.RegisteredProviders);
        Assert.Contains("mystery", ex.Message);
    }

    [Fact]
    public void GetChatClient_Empty_Registry_Throws_With_None_In_Message()
    {
        var factory = new AgentBuilderChatClientFactory([]);

        var ex = Assert.Throws<UnknownProviderException>(() => factory.GetChatClient("openai", "x"));

        Assert.Empty(ex.RegisteredProviders);
        Assert.Contains("<none>", ex.Message);
    }

    [Fact]
    public void GetChatClient_Resolves_Correct_Provider_Among_Many()
    {
        var openai = ProviderMock("openai");
        var anthropic = ProviderMock("anthropic");
        var mock = ProviderMock("mock");

        var factory = new AgentBuilderChatClientFactory(
            [openai.Object, anthropic.Object, mock.Object]);

        factory.GetChatClient("anthropic", "claude-sonnet-4.6");

        openai.Verify(p => p.Create(It.IsAny<string>()), Times.Never);
        mock.Verify(p => p.Create(It.IsAny<string>()), Times.Never);
        anthropic.Verify(p => p.Create("claude-sonnet-4.6"), Times.Once);
    }
}
