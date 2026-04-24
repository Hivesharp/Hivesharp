using Hivesharp.Abstractions.Hive;
using Hivesharp.Mcp.Options;
using ModelContextProtocol.Server;

namespace Hivesharp.Mcp;

internal static class HivesharpMcpToolProvider
{
    public static IEnumerable<McpServerTool> CreateTools(IHive hive, HivesharpMcpServerOptions options)
    {
        var tools = new List<McpServerTool>();

        if (options.ExposeAgents)
        {
            foreach (var descriptor in hive.GetAgents())
            {
                var agentId = descriptor.Id;
                var agent = hive.GetAgentById(agentId);

                tools.Add(McpServerTool.Create(
                    async (string message, CancellationToken cancellationToken) =>
                    {
                        var result = await agent.GenerateAsync(message, null, cancellationToken);
                        return result.Completion;
                    },
                    new McpServerToolCreateOptions
                    {
                        Name = $"ask_{agentId}",
                        Description = $"Ask agent '{agentId}' a question. Instructions: {descriptor.Instructions ?? "General assistant"}"
                    }));
            }
        }

        if (options.ExposeWorkflows)
        {
            foreach (var descriptor in hive.GetWorkflows())
            {
                var workflowId = descriptor.Id;
                var workflow = hive.GetWorkflowById(workflowId);

                tools.Add(McpServerTool.Create(
                    async (string? input, CancellationToken cancellationToken) =>
                    {
                        var result = await workflow.ExecuteAsync(input, cancellationToken);
                        return new { result.Status, result.Output };
                    },
                    new McpServerToolCreateOptions
                    {
                        Name = $"run_{workflowId}",
                        Description = $"Run workflow '{workflowId}'"
                    }));
            }
        }

        return tools;
    }
}
