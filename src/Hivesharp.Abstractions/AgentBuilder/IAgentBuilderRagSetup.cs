namespace Hivesharp.Abstractions.AgentBuilder;

public interface IAgentBuilderRagSetup
{
    IAgentBuilderSetup WithVectorQueryTool(string indexName, string? toolDescription = null, int topK = 5, IReadOnlyDictionary<string, object?>? filter = null);
}
