namespace Hivesharp.Abstractions.Workflow;

public interface IWorkflow
{
    WorkflowDescriptor Descriptor { get; }
    Task<WorkflowResult> ExecuteAsync(object? input = null, CancellationToken cancellationToken = default);
    Task<WorkflowResult> ResumeAsync(string runId, object? resumeData = null, CancellationToken cancellationToken = default);
}
