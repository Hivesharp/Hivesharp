using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Hivesharp.Storage.Postgres;

internal sealed class PostgresSchemaInitializer(
    NpgsqlDataSource dataSource,
    PostgresTableBuilder tables,
    PostgresStorageOptions options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.AutoInitializeSchema)
            return;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var threads = tables.ThreadsTable();
        var messages = tables.MessagesTable();
        var working = tables.WorkingMemoryTable();
        var runs = tables.WorkflowRunsTable();

        var threadsUq = tables.ThreadsTableUnqualified();
        var messagesUq = tables.MessagesTableUnqualified();
        var runsUq = tables.WorkflowRunsTableUnqualified();

        var sql = $"""
            CREATE EXTENSION IF NOT EXISTS vector;

            CREATE TABLE IF NOT EXISTS {threads} (
                id          text PRIMARY KEY,
                resource_id text NULL,
                title       text NULL,
                created_at  timestamptz NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "{tables.IndexName(threadsUq, "resource")}"
                ON {threads} (resource_id, created_at DESC);

            CREATE TABLE IF NOT EXISTS {messages} (
                seq        bigserial PRIMARY KEY,
                thread_id  text NOT NULL REFERENCES {threads}(id) ON DELETE CASCADE,
                role       text NOT NULL,
                content    text NOT NULL,
                created_at timestamptz NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "{tables.IndexName(messagesUq, "thread")}"
                ON {messages} (thread_id, seq);

            CREATE TABLE IF NOT EXISTS {working} (
                thread_id  text PRIMARY KEY REFERENCES {threads}(id) ON DELETE CASCADE,
                content    text NOT NULL,
                updated_at timestamptz NOT NULL
            );

            CREATE TABLE IF NOT EXISTS {runs} (
                run_id      text PRIMARY KEY,
                workflow_id text NOT NULL,
                snapshot    jsonb NOT NULL,
                created_at  timestamptz NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "{tables.IndexName(runsUq, "workflow")}"
                ON {runs} (workflow_id, created_at DESC);
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
