using Microsoft.Extensions.AI;

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

    public TimeoutFunctionInvokingChatClient(IChatClient inner, int maxIterations, TimeSpan toolTimeout)
        : base(inner)
    {
        MaximumIterationsPerRequest = maxIterations;
        _toolTimeout = toolTimeout;
    }

    protected override async ValueTask<object?> InvokeFunctionAsync(
        FunctionInvocationContext context,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_toolTimeout);
        try
        {
            return await base.InvokeFunctionAsync(context, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var toolName = context.CallContent.Name;
            return $"[Tool '{toolName}' timed out after {(int)_toolTimeout.TotalSeconds}s. The service may be unavailable or slow. Inform the user and proceed without that tool result.]";
        }
    }
}
