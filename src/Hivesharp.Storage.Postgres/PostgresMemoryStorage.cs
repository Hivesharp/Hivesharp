using Hivesharp.Abstractions.Memory;
using Npgsql;

namespace Hivesharp.Storage.Postgres;

internal sealed class PostgresMemoryStorage(NpgsqlDataSource dataSource, PostgresTableBuilder tables) : IMemoryStorage
{
    public async Task<MemoryThread> CreateThreadAsync(
        string? resourceId = null, string? title = null, CancellationToken cancellationToken = default)
    {
        var thread = new MemoryThread
        {
            Id = Guid.NewGuid().ToString(),
            ResourceId = resourceId,
            Title = title,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var sql = $"""
            INSERT INTO {tables.ThreadsTable()} (id, resource_id, title, created_at)
            VALUES (@id, @resource_id, @title, @created_at);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", thread.Id);
        cmd.Parameters.AddWithValue("resource_id", (object?)resourceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("title", (object?)title ?? DBNull.Value);
        cmd.Parameters.AddWithValue("created_at", thread.CreatedAt);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return thread;
    }

    public async Task<MemoryThread?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT id, resource_id, title, created_at
            FROM {tables.ThreadsTable()}
            WHERE id = @id;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", threadId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return ReadThread(reader);
    }

    public async Task<IReadOnlyList<MemoryThread>> GetThreadsByResourceAsync(
        string resourceId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT id, resource_id, title, created_at
            FROM {tables.ThreadsTable()}
            WHERE resource_id = @resource_id
            ORDER BY created_at DESC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("resource_id", resourceId);

        var result = new List<MemoryThread>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(ReadThread(reader));

        return result;
    }

    public async Task DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        // Cascading FKs on messages and working_memory take care of related rows.
        var sql = $"DELETE FROM {tables.ThreadsTable()} WHERE id = @id;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", threadId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveMessagesAsync(
        string threadId, IReadOnlyList<MemoryMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0) return;

        var sql = $"""
            INSERT INTO {tables.MessagesTable()} (thread_id, role, content, created_at)
            VALUES (@thread_id, @role, @content, @created_at);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var message in messages)
        {
            await using var cmd = new NpgsqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("thread_id", threadId);
            cmd.Parameters.AddWithValue("role", message.Role);
            cmd.Parameters.AddWithValue("content", message.Content);
            cmd.Parameters.AddWithValue("created_at",
                message.CreatedAt == default ? DateTimeOffset.UtcNow : message.CreatedAt);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryMessage>> GetMessagesAsync(
        string threadId, int? limit = null, CancellationToken cancellationToken = default)
    {
        // For the "last N" semantics we sort DESC + LIMIT, then reverse to chronological order.
        var sql = limit.HasValue
            ? $"""
                SELECT role, content, created_at FROM (
                    SELECT role, content, created_at, seq
                    FROM {tables.MessagesTable()}
                    WHERE thread_id = @thread_id
                    ORDER BY seq DESC
                    LIMIT @limit
                ) AS recent
                ORDER BY seq ASC;
                """
            : $"""
                SELECT role, content, created_at
                FROM {tables.MessagesTable()}
                WHERE thread_id = @thread_id
                ORDER BY seq ASC;
                """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("thread_id", threadId);
        if (limit.HasValue)
            cmd.Parameters.AddWithValue("limit", limit.Value);

        var result = new List<MemoryMessage>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new MemoryMessage
            {
                Role = reader.GetString(0),
                Content = reader.GetString(1),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(2)
            });
        }

        return result;
    }

    public async Task<string?> GetWorkingMemoryAsync(string threadId, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT content FROM {tables.WorkingMemoryTable()} WHERE thread_id = @thread_id;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("thread_id", threadId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is string s ? s : null;
    }

    public async Task SaveWorkingMemoryAsync(string threadId, string content, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            INSERT INTO {tables.WorkingMemoryTable()} (thread_id, content, updated_at)
            VALUES (@thread_id, @content, @updated_at)
            ON CONFLICT (thread_id) DO UPDATE SET
                content    = EXCLUDED.content,
                updated_at = EXCLUDED.updated_at;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("thread_id", threadId);
        cmd.Parameters.AddWithValue("content", content);
        cmd.Parameters.AddWithValue("updated_at", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static MemoryThread ReadThread(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetString(0),
        ResourceId = reader.IsDBNull(1) ? null : reader.GetString(1),
        Title = reader.IsDBNull(2) ? null : reader.GetString(2),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(3)
    };
}
