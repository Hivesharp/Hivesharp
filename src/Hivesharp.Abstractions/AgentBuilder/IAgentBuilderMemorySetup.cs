using Hivesharp.Abstractions.Memory;

namespace Hivesharp.Abstractions.AgentBuilder;

public interface IAgentBuilderMemorySetup
{
    IAgentBuilderSetup WithMessageHistoryMemory(int? maxMessages = null);
    IAgentBuilderSetup WithMessageHistoryMemory(IMemoryStorage storage, int? maxMessages = null);
    IAgentBuilderSetup WithWorkingMemory(string? instructions = null);
    IAgentBuilderSetup WithWorkingMemory(IMemoryStorage storage, string? instructions = null);
}