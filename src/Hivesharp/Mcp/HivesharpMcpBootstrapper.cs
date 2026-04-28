using Hivesharp.Abstractions.Hive;
using Hivesharp.Diagnostics;
using Hivesharp.Mcp.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Hivesharp.Mcp;

internal sealed class HivesharpMcpBootstrapper(
    IHive hive,
    HivesharpMcpServerOptions options,
    IOptions<McpServerOptions> mcpServerOptions,
    ILoggerFactory? loggerFactory = null) : IHostedService
{
    private readonly ILogger _logger = loggerFactory?.CreateLogger<HivesharpMcpBootstrapper>() ?? NullLogger<HivesharpMcpBootstrapper>.Instance;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        hive.Initialize();

        var tools = HivesharpMcpToolProvider.CreateTools(hive, options, loggerFactory).ToList();

        var serverOptions = mcpServerOptions.Value;
        serverOptions.ToolCollection ??= new McpServerPrimitiveCollection<McpServerTool>();

        foreach (var tool in tools)
        {
            serverOptions.ToolCollection.Add(tool);
        }

        McpLog.ServerBootstrapped(_logger,
            options.ExposeAgents ? hive.GetAgents().Count() : 0,
            options.ExposeWorkflows ? hive.GetWorkflows().Count() : 0);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
