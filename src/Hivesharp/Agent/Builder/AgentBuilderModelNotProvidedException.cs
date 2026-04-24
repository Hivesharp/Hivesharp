namespace Hivesharp.Agent.Builder;

internal class AgentBuilderModelNotProvidedException()
    : Exception("Agent model must be provided. Call WithModel() before Build().")
{
}