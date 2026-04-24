namespace Hivesharp.Abstractions.AgentBuilder;

public interface IAgentBuilderMcpSetup
{
    IAgentBuilderSetup WithMcpServer(string name, Uri httpEndpoint);
    IAgentBuilderSetup WithMcpServer(string name, string pipeName);
}
