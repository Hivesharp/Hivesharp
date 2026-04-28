using System.Diagnostics;
using Hivesharp.Abstractions.Hive;
using Hivesharp.Diagnostics;
using Hivesharp.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;

namespace Hivesharp.Mcp;

internal static class HivesharpMcpToolProvider
{
    public static IEnumerable<McpServerTool> CreateTools(IHive hive, HivesharpMcpServerOptions options, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger("Hivesharp.Mcp.ToolProvider") ?? NullLogger.Instance;
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
                        McpLog.AgentToolStarted(logger, agentId);
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            var result = await agent.GenerateAsync(message, null, cancellationToken);
                            sw.Stop();
                            McpLog.AgentToolCompleted(logger, agentId, sw.ElapsedMilliseconds);
                            return result.Completion;
                        }
                        catch (Exception ex)
                        {
                            McpLog.AgentToolFailed(logger, ex, agentId);
                            throw;
                        }
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
                        McpLog.WorkflowToolStarted(logger, workflowId);
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            var result = await workflow.ExecuteAsync(input, cancellationToken);
                            sw.Stop();
                            McpLog.WorkflowToolCompleted(logger, workflowId, result.Status.ToString(), sw.ElapsedMilliseconds);
                            return new { result.Status, result.Output };
                        }
                        catch (Exception ex)
                        {
                            McpLog.WorkflowToolFailed(logger, ex, workflowId);
                            throw;
                        }
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
