using Microsoft.Extensions.Logging;

namespace Hivesharp.Diagnostics;

internal static partial class AgentLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Agent {AgentId} generate started (thread={ThreadId}, msgLen={MessageLength})")]
    public static partial void GenerateStarted(ILogger logger, string agentId, string? threadId, int messageLength);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Agent {AgentId} generate completed (thread={ThreadId}, promptTokens={PromptTokens}, completionTokens={CompletionTokens}, durationMs={DurationMs})")]
    public static partial void GenerateCompleted(ILogger logger, string agentId, string? threadId, long promptTokens, long completionTokens, long durationMs);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error,
        Message = "Agent {AgentId} generate failed")]
    public static partial void GenerateFailed(ILogger logger, Exception ex, string agentId);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Information,
        Message = "Agent {AgentId} simple generate started (msgLen={MessageLength})")]
    public static partial void SimpleGenerateStarted(ILogger logger, string agentId, int messageLength);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Information,
        Message = "Agent {AgentId} simple generate completed (promptTokens={PromptTokens}, completionTokens={CompletionTokens}, durationMs={DurationMs})")]
    public static partial void SimpleGenerateCompleted(ILogger logger, string agentId, long promptTokens, long completionTokens, long durationMs);

    [LoggerMessage(EventId = 1013, Level = LogLevel.Error,
        Message = "Agent {AgentId} simple generate failed")]
    public static partial void SimpleGenerateFailed(ILogger logger, Exception ex, string agentId);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Debug,
        Message = "Tool {ToolName} invocation started (timeoutSec={TimeoutSeconds})")]
    public static partial void ToolInvocationStarted(ILogger logger, string toolName, int timeoutSeconds);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Debug,
        Message = "Tool {ToolName} invocation completed (durationMs={DurationMs})")]
    public static partial void ToolInvocationCompleted(ILogger logger, string toolName, long durationMs);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Warning,
        Message = "Tool {ToolName} invocation timed out after {TimeoutSeconds}s")]
    public static partial void ToolInvocationTimedOut(ILogger logger, string toolName, int timeoutSeconds);

    [LoggerMessage(EventId = 1104, Level = LogLevel.Error,
        Message = "Tool {ToolName} invocation failed")]
    public static partial void ToolInvocationFailed(ILogger logger, Exception ex, string toolName);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Debug,
        Message = "Loaded {MessageCount} message(s) from history for thread {ThreadId}")]
    public static partial void HistoryLoaded(ILogger logger, int messageCount, string threadId);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Debug,
        Message = "Persisted {MessageCount} message(s) for thread {ThreadId}")]
    public static partial void HistoryPersisted(ILogger logger, int messageCount, string threadId);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Debug,
        Message = "Working memory injected for thread {ThreadId} (present={Present})")]
    public static partial void WorkingMemoryInjected(ILogger logger, string threadId, bool present);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Debug,
        Message = "Working memory flushed for thread {ThreadId} (updated={Updated})")]
    public static partial void WorkingMemoryFlushed(ILogger logger, string threadId, bool updated);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Warning,
        Message = "Working memory parse failed for thread {ThreadId}")]
    public static partial void WorkingMemoryParseFailed(ILogger logger, Exception ex, string threadId);
}
