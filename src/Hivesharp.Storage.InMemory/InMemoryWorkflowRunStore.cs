using System.Collections.Concurrent;
using Hivesharp.Abstractions.Workflow;

namespace Hivesharp.Storage.InMemory;

public class InMemoryWorkflowRunStore : IWorkflowRunStore
{
    private readonly ConcurrentDictionary<string, WorkflowSnapshot> _snapshots = new();

    public Task SaveSnapshotAsync(WorkflowSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshots[snapshot.RunId] = snapshot;
        return Task.CompletedTask;
    }

    public Task<WorkflowSnapshot?> GetSnapshotAsync(string runId, CancellationToken cancellationToken = default)
    {
        _snapshots.TryGetValue(runId, out var snapshot);
        return Task.FromResult(snapshot);
    }

    public Task DeleteSnapshotAsync(string runId, CancellationToken cancellationToken = default)
    {
        _snapshots.TryRemove(runId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WorkflowSnapshot>> GetSnapshotsByWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var results = _snapshots.Values
            .Where(s => s.WorkflowId == workflowId)
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<WorkflowSnapshot>>(results);
    }
}
