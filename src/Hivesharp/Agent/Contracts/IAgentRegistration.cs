using Hivesharp.Abstractions.Agent;
using Hivesharp.Abstractions.Memory;

namespace Hivesharp.Agent.Contracts;

internal interface IAgentRegistration
{
    IAgent Agent { get; }
    MemoryConfiguration? Memory { get; }
}
