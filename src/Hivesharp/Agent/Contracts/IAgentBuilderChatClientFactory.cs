using Microsoft.Extensions.AI;

namespace Hivesharp.Agent.Contracts;

internal interface IAgentBuilderChatClientFactory
{
    IChatClient GetChatClient(string providerName, string modelName);
}