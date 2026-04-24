using Microsoft.Extensions.AI;

namespace Hivesharp.Abstractions.AgentBuilder;

public interface IChatClientProvider
{
    string Name { get; }
    IChatClient Create(string modelName);
}
