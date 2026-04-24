using Hivesharp.Abstractions.Memory;
using Hivesharp.Memory;

namespace Hivesharp.Registries;

internal class MemoryRegistry : IMemoryRegistry
{
    private readonly Dictionary<string, MemoryConfiguration> _registrations = new();

    public void RegisterMemory(string agentId, MemoryConfiguration memory)
    {
        _registrations[agentId] = memory;
    }

    public MemoryConfiguration? GetMemory(string agentId)
    {
        return _registrations.GetValueOrDefault(agentId);
    }

    public bool HasMemory(string agentId)
    {
        return _registrations.ContainsKey(agentId);
    }
}