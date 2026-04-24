using Hivesharp.Abstractions.AgentBuilder;
using Anthropic.SDK;
using Microsoft.Extensions.AI;

namespace Hivesharp.Providers.Anthropic;

internal sealed class AnthropicChatClientProvider(AnthropicProviderOptions options) : IChatClientProvider
{
    public string Name => "anthropic";

    public IChatClient Create(string modelName)
        => new ChatClientBuilder(new AnthropicClient(options.ApiKey).Messages)
            .ConfigureOptions(o => o.ModelId ??= modelName)
            .Build();
}
