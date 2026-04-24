using Hivesharp.Abstractions.Memory;

namespace Hivesharp.Registries;

internal interface IMemoryRegistry
{
    void RegisterMemory(string agentId, MemoryConfiguration memory);
    MemoryConfiguration? GetMemory(string agentId);
    bool HasMemory(string agentId);
}