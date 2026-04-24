namespace Hivesharp.Abstractions.Workflow;

public class WorkflowResult
{
    public required WorkflowStatus Status { get; init; }
    public string? RunId { get; init; }
    public object? Output { get; init; }
    public IReadOnlyList<StepResult> Steps { get; init; } = [];
    public string? SuspendedStepId { get; init; }
    public object? SuspendPayload { get; init; }
}

public class StepResult
{
    public required string StepId { get; init; }
    public required StepStatus Status { get; init; }
    public object? Output { get; init; }
    public TimeSpan Duration { get; init; }
}

public enum WorkflowStatus
{
    Completed,
    Failed,
    Suspended
}

public enum StepStatus
{
    Completed,
    Failed,
    Skipped,
    Suspended
}
