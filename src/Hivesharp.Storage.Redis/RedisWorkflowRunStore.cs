using System.Text.Json;
using Hivesharp.Abstractions.Workflow;
using StackExchange.Redis;

namespace Hivesharp.Storage.Redis;

internal sealed class RedisWorkflowRunStore(IConnectionMultiplexer multiplexer, RedisKeyBuilder keys) : IWorkflowRunStore
{
    private IDatabase Db => multiplexer.GetDatabase();

    public async Task SaveSnapshotAsync(WorkflowSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(snapshot);

        var db = Db;
        var tx = db.CreateTransaction();
        _ = tx.StringSetAsync(keys.WorkflowRun(snapshot.RunId), payload);
        _ = tx.SortedSetAddAsync(
            keys.WorkflowRunsIndex(snapshot.WorkflowId),
            snapshot.RunId,
            snapshot.CreatedAt.ToUnixTimeMilliseconds());
        await tx.ExecuteAsync();
    }

    public async Task<WorkflowSnapshot?> GetSnapshotAsync(string runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await Db.StringGetAsync(keys.WorkflowRun(runId));
        if (!value.HasValue) return null;
        return JsonSerializer.Deserialize<WorkflowSnapshot>((string)value!);
    }

    public async Task DeleteSnapshotAsync(string runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = Db;
        var snapshot = await GetSnapshotAsync(runId, cancellationToken);

        var tx = db.CreateTransaction();
        _ = tx.KeyDeleteAsync(keys.WorkflowRun(runId));
        if (snapshot is not null)
        {
            _ = tx.SortedSetRemoveAsync(keys.WorkflowRunsIndex(snapshot.WorkflowId), runId);
        }
        await tx.ExecuteAsync();
    }

    public async Task<IReadOnlyList<WorkflowSnapshot>> GetSnapshotsByWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = Db;
        var ids = await db.SortedSetRangeByScoreAsync(
            keys.WorkflowRunsIndex(workflowId),
            order: Order.Descending);

        if (ids.Length == 0) return [];

        var keysToFetch = new RedisKey[ids.Length];
        for (var i = 0; i < ids.Length; i++) keysToFetch[i] = keys.WorkflowRun(ids[i]!);

        var values = await db.StringGetAsync(keysToFetch);

        var result = new List<WorkflowSnapshot>(values.Length);
        foreach (var value in values)
        {
            if (!value.HasValue) continue;
            var snapshot = JsonSerializer.Deserialize<WorkflowSnapshot>((string)value!);
            if (snapshot is not null) result.Add(snapshot);
        }
        return result;
    }
}
