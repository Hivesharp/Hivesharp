using Hivesharp.Abstractions.Memory;

namespace Hivesharp.Abstractions.AgentBuilder;

public interface IAgentBuilderMemorySetup
{
    IAgentBuilderSetup WithMessageHistoryMemory(int? maxMessages = null);
    IAgentBuilderSetup WithMessageHistoryMemory(IMemoryStorage storage, int? maxMessages = null);
    IAgentBuilderSetup WithMessageHistoryMemory(Type storageType, int? maxMessages = null);
    IAgentBuilderSetup WithMessageHistoryMemory<TStorage>(int? maxMessages = null)
        where TStorage : class, IMemoryStorage;
    IAgentBuilderSetup WithWorkingMemory(string? instructions = null);
    IAgentBuilderSetup WithWorkingMemory(IMemoryStorage storage, string? instructions = null);
    IAgentBuilderSetup WithWorkingMemory(Type storageType, string? instructions = null);
    IAgentBuilderSetup WithWorkingMemory<TStorage>(string? instructions = null)
        where TStorage : class, IMemoryStorage;
}