using Hivesharp.Abstractions.Agent;
using Hivesharp.Abstractions.Tool;

namespace Hivesharp.Abstractions.AgentBuilder;

public interface IAgentBuilder: IAgentBuilderSetup
{
    IAgent Build();
}
