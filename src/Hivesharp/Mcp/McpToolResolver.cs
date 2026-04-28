using System.IO.Pipes;
using Hivesharp.Abstractions.Agent;
using Hivesharp.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Hivesharp.Mcp;

internal sealed class McpToolResolver(ILoggerFactory? loggerFactory = null) : IMcpToolResolver
{
    private readonly List<McpClient> _clients = [];
    private readonly ILogger _logger = loggerFactory?.CreateLogger<McpToolResolver>() ?? NullLogger<McpToolResolver>.Instance;

    public async Task<McpToolResolutionResult> ResolveToolsAsync(
        IReadOnlyList<McpServerDefinition> servers,
        CancellationToken cancellationToken = default)
    {
        var allTools = new List<AITool>();
        var statuses = new List<McpServerStatus>();

        foreach (var server in servers)
        {
            var transportType = server.PipeName is not null ? "pipe" : "http";
            McpLog.ConnectStarted(_logger, server.Name, transportType);
            try
            {
                var transport = await CreateTransportAsync(server, cancellationToken);
                var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
                _clients.Add(client);

                var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                var toolNames = new List<string>();

                foreach (var tool in tools)
                {
                    var namespacedName = $"{server.Name}_{tool.Name}";
                    var namespacedTool = tool.WithName(namespacedName);
                    allTools.Add(namespacedTool);
                    toolNames.Add(namespacedName);
                }

                McpLog.ConnectCompleted(_logger, server.Name, toolNames.Count);
                statuses.Add(new McpServerStatus(
                    Name: server.Name,
                    IsAvailable: true,
                    ToolNames: toolNames,
                    UnavailableReason: null));
            }
            catch (Exception ex)
            {
                McpLog.ConnectFailed(_logger, ex, server.Name, ex.Message);
                statuses.Add(new McpServerStatus(
                    Name: server.Name,
                    IsAvailable: false,
                    ToolNames: [],
                    UnavailableReason: ex.Message));
            }
        }

        return new McpToolResolutionResult(allTools, statuses);
    }

    private async Task<IClientTransport> CreateTransportAsync(McpServerDefinition server, CancellationToken ct)
    {
        if (server.HttpEndpoint is not null)
        {
            McpLog.TransportCreated(_logger, server.Name, "http");
            return new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = server.Name,
                Endpoint = server.HttpEndpoint
            });
        }

        if (server.PipeName is not null)
        {
            McpLog.TransportCreated(_logger, server.Name, "pipe");
            var pipe = new NamedPipeClientStream(".", server.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            connectCts.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await pipe.ConnectAsync(connectCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                McpLog.PipeConnectTimedOut(_logger, server.Name, server.PipeName);
                throw new TimeoutException($"Timed out connecting to named pipe '{server.PipeName}'.");
            }
            return new StreamClientTransport(pipe, pipe, loggerFactory);
        }

        throw new InvalidOperationException(
            $"MCP server '{server.Name}' must have either an HttpEndpoint or PipeName configured.");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }
        _clients.Clear();
    }
}
