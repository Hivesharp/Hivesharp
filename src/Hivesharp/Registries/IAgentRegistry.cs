using Hivesharp.Abstractions.Agent;

namespace Hivesharp.Registries;

internal interface IAgentRegistry
{
    bool RegisterAgent(IAgent agentDescriptor);
    IAgent GetAgent(string agentId);
    IReadOnlyList<AgentDescriptor> GetAgents();
}