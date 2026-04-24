namespace Hivesharp.Abstractions.Workflow;

public class WorkflowSnapshot
{
    public required string RunId { get; init; }
    public required string WorkflowId { get; init; }
    public required IReadOnlyList<StepResult> CompletedSteps { get; init; }
    public required string SuspendedAtStepId { get; init; }
    public object? SuspendPayload { get; init; }
    public object? LastOutput { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
