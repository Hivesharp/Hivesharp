namespace Hivesharp.Abstractions.Workflow;

public class WorkflowDescriptor
{
    public required string Id { get; init; }
    public IReadOnlyList<string> StepIds { get; init; } = [];
    public IReadOnlyList<WorkflowNodeDescriptor> Nodes { get; init; } = [];
    public IReadOnlyList<WorkflowEdgeDescriptor> Edges { get; init; } = [];
}

public class WorkflowNodeDescriptor
{
    public required string Id { get; init; }
    public required string Type { get; init; } // "step", "branch"
    public string? Label { get; init; }
}

public class WorkflowEdgeDescriptor
{
    public required string Source { get; init; }
    public required string Target { get; init; }
    public string? Label { get; init; }
}
