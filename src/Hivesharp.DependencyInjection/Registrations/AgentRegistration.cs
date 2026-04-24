using Hivesharp.Abstractions.Agent;
using Hivesharp.Abstractions.Memory;
using Hivesharp.Agent;
using Hivesharp.Agent.Contracts;

namespace Hivesharp.DependencyInjection.Registrations;

internal class AgentRegistration(IAgent agent) : IAgentRegistration
{
    public IAgent Agent { get; } = agent;
    public MemoryConfiguration? Memory { get; } = agent.Memory;
}