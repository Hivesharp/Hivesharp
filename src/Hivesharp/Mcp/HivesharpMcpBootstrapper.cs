using Hivesharp.Abstractions.Hive;
using Hivesharp.Mcp.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Hivesharp.Mcp;

internal sealed class HivesharpMcpBootstrapper(
    IHive hive,
    HivesharpMcpServerOptions options,
    IOptions<McpServerOptions> mcpServerOptions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        hive.Initialize();

        var tools = HivesharpMcpToolProvider.CreateTools(hive, options);

        var serverOptions = mcpServerOptions.Value;
        serverOptions.ToolCollection ??= new McpServerPrimitiveCollection<McpServerTool>();

        foreach (var tool in tools)
        {
            serverOptions.ToolCollection.Add(tool);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
