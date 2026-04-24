using Hivesharp.Abstractions.Tool;

namespace Hivesharp.Abstractions.AgentBuilder;

public interface IAgentBuilderSetup: IAgentBuilderMemorySetup, IAgentBuilderRagSetup, IAgentBuilderMcpSetup
{
    IAgentBuilderSetup WithId(string id);
    IAgentBuilderSetup WithModel(string providerName, string modelName);
    IAgentBuilderSetup WithModel(string providerNameColonModelName);
    IAgentBuilderSetup WithInstructions(string instructions);
    IAgentBuilderSetup WithTool(Type type);
    IAgentBuilderSetup WithTool<T>() where T : ITool;
    IAgentBuilderSetup WithTool(ITool instance);
    IAgentBuilderSetup WithTool(string name, string description, Delegate handler);

    /// <summary>
    /// Caps the number of agentic iterations (tool-call turns) per <c>GenerateAsync</c>.
    /// Defaults to 10. Lower to bound cost; raise when complex tool chains need more depth.
    /// </summary>
    IAgentBuilderSetup WithMaxSteps(int maxSteps);
}
