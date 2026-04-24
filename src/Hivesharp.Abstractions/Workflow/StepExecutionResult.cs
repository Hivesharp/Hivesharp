namespace Hivesharp.Abstractions.Workflow;

public sealed class StepExecutionResult
{
    public object? Output { get; }
    public bool IsSuspended { get; }
    public object? SuspendPayload { get; }

    private StepExecutionResult(object? output, bool isSuspended, object? suspendPayload)
    {
        Output = output;
        IsSuspended = isSuspended;
        SuspendPayload = suspendPayload;
    }

    public static StepExecutionResult Continue(object? output)
        => new(output, false, null);

    public static StepExecutionResult Suspend(object? payload = null)
        => new(null, true, payload);
}
