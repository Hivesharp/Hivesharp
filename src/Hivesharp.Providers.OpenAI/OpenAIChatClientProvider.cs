using Hivesharp.Abstractions.AgentBuilder;
using Microsoft.Extensions.AI;

namespace Hivesharp.Providers.OpenAI;

internal sealed class OpenAIChatClientProvider(OpenAIProviderOptions options) : IChatClientProvider
{
    public string Name => "openai";

    public IChatClient Create(string modelName)
        => new global::OpenAI.Chat.ChatClient(modelName, options.ApiKey).AsIChatClient();
}
