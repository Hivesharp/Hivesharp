using Hivesharp.Abstractions.Memory;

namespace Hivesharp.Abstractions.Agent;

public interface IAgent
{
    AgentDescriptor AgentDescriptor { get; }
    AgentRuntimeState RuntimeState { get; }
    MemoryConfiguration? Memory { get; }
    Task<AgentResult> GenerateAsync(string message, string? threadId = null, CancellationToken cancellationToken = default);
    Task RetryMcpAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
