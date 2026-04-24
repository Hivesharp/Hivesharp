using System.Text.Json;
using Hivesharp.Abstractions.Workflow;
using Npgsql;
using NpgsqlTypes;

namespace Hivesharp.Storage.Postgres;

internal sealed class PostgresWorkflowRunStore(NpgsqlDataSource dataSource, PostgresTableBuilder tables) : IWorkflowRunStore
{
    public async Task SaveSnapshotAsync(WorkflowSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            INSERT INTO {tables.WorkflowRunsTable()} (run_id, workflow_id, snapshot, created_at)
            VALUES (@run_id, @workflow_id, @snapshot, @created_at)
            ON CONFLICT (run_id) DO UPDATE SET
                workflow_id = EXCLUDED.workflow_id,
                snapshot    = EXCLUDED.snapshot,
                created_at  = EXCLUDED.created_at;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("run_id", snapshot.RunId);
        cmd.Parameters.AddWithValue("workflow_id", snapshot.WorkflowId);
        cmd.Parameters.Add(new NpgsqlParameter("snapshot", NpgsqlDbType.Jsonb)
        {
            Value = JsonSerializer.Serialize(snapshot)
        });
        cmd.Parameters.AddWithValue("created_at", snapshot.CreatedAt);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<WorkflowSnapshot?> GetSnapshotAsync(string runId, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT snapshot FROM {tables.WorkflowRunsTable()} WHERE run_id = @run_id;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("run_id", runId);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is not string json) return null;
        return JsonSerializer.Deserialize<WorkflowSnapshot>(json);
    }

    public async Task DeleteSnapshotAsync(string runId, CancellationToken cancellationToken = default)
    {
        var sql = $"DELETE FROM {tables.WorkflowRunsTable()} WHERE run_id = @run_id;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("run_id", runId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowSnapshot>> GetSnapshotsByWorkflowAsync(
        string workflowId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT snapshot
            FROM {tables.WorkflowRunsTable()}
            WHERE workflow_id = @workflow_id
            ORDER BY created_at DESC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("workflow_id", workflowId);

        var result = new List<WorkflowSnapshot>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var json = reader.GetString(0);
            var snapshot = JsonSerializer.Deserialize<WorkflowSnapshot>(json);
            if (snapshot is not null) result.Add(snapshot);
        }

        return result;
    }
}
