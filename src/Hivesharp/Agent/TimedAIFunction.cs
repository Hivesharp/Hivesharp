using System.Diagnostics;
using Hivesharp.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hivesharp.Agent;

/// <summary>
/// A <see cref="FunctionInvokingChatClient"/> that:
/// <list type="bullet">
///   <item>Limits the maximum number of tool-calling iterations per request.</item>
///   <item>Cancels individual tool invocations that exceed a configured timeout, returning a
///         descriptive error string so the LLM can respond gracefully.</item>
/// </list>
/// </summary>
internal sealed class TimeoutFunctionInvokingChatClient : FunctionInvokingChatClient
{
    private readonly TimeSpan _toolTimeout;
    private readonly ILogger _logger;

    public TimeoutFunctionInvokingChatClient(IChatClient inner, int maxIterations, TimeSpan toolTimeout, ILogger? logger = null)
        : base(inner)
    {
        MaximumIterationsPerRequest = maxIterations;
        _toolTimeout = toolTimeout;
        _logger = logger ?? NullLogger.Instance;
    }

    protected override async ValueTask<object?> InvokeFunctionAsync(
        FunctionInvocationContext context,
        CancellationToken cancellationToken)
    {
        var toolName = context.CallContent.Name;
        var timeoutSec = (int)_toolTimeout.TotalSeconds;
        AgentLog.ToolInvocationStarted(_logger, toolName, timeoutSec);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_toolTimeout);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await base.InvokeFunctionAsync(context, cts.Token);
            sw.Stop();
            AgentLog.ToolInvocationCompleted(_logger, toolName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            AgentLog.ToolInvocationTimedOut(_logger, toolName, timeoutSec);
            return $"[Tool '{toolName}' timed out after {timeoutSec}s. The service may be unavailable or slow. Inform the user and proceed without that tool result.]";
        }
        catch (Exception ex)
        {
            AgentLog.ToolInvocationFailed(_logger, ex, toolName);
            throw;
        }
    }
}
