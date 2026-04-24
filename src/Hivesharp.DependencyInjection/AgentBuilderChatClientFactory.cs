using Hivesharp.Abstractions.AgentBuilder;
using Hivesharp.Agent.Contracts;
using Microsoft.Extensions.AI;

namespace Hivesharp.DependencyInjection;

internal class AgentBuilderChatClientFactory : IAgentBuilderChatClientFactory
{
    private readonly Dictionary<string, IChatClientProvider> _providers;

    public AgentBuilderChatClientFactory(IEnumerable<IChatClientProvider> providers)
        => _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    public IChatClient GetChatClient(string providerName, string modelName)
        => _providers.TryGetValue(providerName, out var provider)
            ? provider.Create(modelName)
            : throw new UnknownProviderException(providerName, _providers.Keys);
}
