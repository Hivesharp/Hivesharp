using Hivesharp.Abstractions.Agent;
using Hivesharp.Agent;
using Microsoft.Extensions.DependencyInjection;

namespace Hivesharp.Registries;

internal class AgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, IAgent> _registrations = new();
    
    public IReadOnlyList<AgentDescriptor> GetAgents()
    {
        return _registrations.Values.Select(kvp => kvp.AgentDescriptor).ToList();
    }

    public bool RegisterAgent(IAgent agent)
    {
        return _registrations.TryAdd(agent.AgentDescriptor.Id, agent);
    }

    public IAgent GetAgent(string agentId)
    {
        return  _registrations[agentId];
    }
}
