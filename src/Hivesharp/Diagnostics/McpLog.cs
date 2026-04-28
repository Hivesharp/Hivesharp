using Microsoft.Extensions.Logging;

namespace Hivesharp.Diagnostics;

internal static partial class McpLog
{
    [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
        Message = "MCP server {ServerName} connect started (transport={Transport})")]
    public static partial void ConnectStarted(ILogger logger, string serverName, string transport);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information,
        Message = "MCP server {ServerName} connect completed (toolCount={ToolCount})")]
    public static partial void ConnectCompleted(ILogger logger, string serverName, int toolCount);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning,
        Message = "MCP server {ServerName} connect failed: {Reason}")]
    public static partial void ConnectFailed(ILogger logger, Exception ex, string serverName, string reason);

    [LoggerMessage(EventId = 3010, Level = LogLevel.Information,
        Message = "MCP retry attempted for {ServerCount} server(s)")]
    public static partial void RetryAttempted(ILogger logger, int serverCount);

    [LoggerMessage(EventId = 3011, Level = LogLevel.Warning,
        Message = "MCP retry for {ServerName} did not restore availability")]
    public static partial void RetryFailed(ILogger logger, string serverName);

    [LoggerMessage(EventId = 3020, Level = LogLevel.Debug,
        Message = "MCP transport created for {ServerName} (type={TransportType})")]
    public static partial void TransportCreated(ILogger logger, string serverName, string transportType);

    [LoggerMessage(EventId = 3021, Level = LogLevel.Warning,
        Message = "MCP pipe connect timed out for {ServerName} (pipe={PipeName})")]
    public static partial void PipeConnectTimedOut(ILogger logger, string serverName, string pipeName);

    [LoggerMessage(EventId = 3101, Level = LogLevel.Information,
        Message = "Hivesharp MCP server bootstrapped (agents={AgentCount}, workflows={WorkflowCount})")]
    public static partial void ServerBootstrapped(ILogger logger, int agentCount, int workflowCount);

    [LoggerMessage(EventId = 3110, Level = LogLevel.Information,
        Message = "MCP agent tool 'ask_{AgentId}' invocation started")]
    public static partial void AgentToolStarted(ILogger logger, string agentId);

    [LoggerMessage(EventId = 3111, Level = LogLevel.Information,
        Message = "MCP agent tool 'ask_{AgentId}' invocation completed (durationMs={DurationMs})")]
    public static partial void AgentToolCompleted(ILogger logger, string agentId, long durationMs);

    [LoggerMessage(EventId = 3112, Level = LogLevel.Error,
        Message = "MCP agent tool 'ask_{AgentId}' invocation failed")]
    public static partial void AgentToolFailed(ILogger logger, Exception ex, string agentId);

    [LoggerMessage(EventId = 3120, Level = LogLevel.Information,
        Message = "MCP workflow tool 'run_{WorkflowId}' invocation started")]
    public static partial void WorkflowToolStarted(ILogger logger, string workflowId);

    [LoggerMessage(EventId = 3121, Level = LogLevel.Information,
        Message = "MCP workflow tool 'run_{WorkflowId}' invocation completed (status={Status}, durationMs={DurationMs})")]
    public static partial void WorkflowToolCompleted(ILogger logger, string workflowId, string status, long durationMs);

    [LoggerMessage(EventId = 3122, Level = LogLevel.Error,
        Message = "MCP workflow tool 'run_{WorkflowId}' invocation failed")]
    public static partial void WorkflowToolFailed(ILogger logger, Exception ex, string workflowId);
}
