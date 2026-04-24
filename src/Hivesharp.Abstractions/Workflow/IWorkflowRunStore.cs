namespace Hivesharp.Abstractions.Workflow;

public interface IWorkflowRunStore
{
    Task SaveSnapshotAsync(WorkflowSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<WorkflowSnapshot?> GetSnapshotAsync(string runId, CancellationToken cancellationToken = default);
    Task DeleteSnapshotAsync(string runId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowSnapshot>> GetSnapshotsByWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);
}
