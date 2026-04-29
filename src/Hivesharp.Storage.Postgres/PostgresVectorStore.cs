using System.Text.Json;
using Hivesharp.Abstractions.Rag;
using Npgsql;
using NpgsqlTypes;
using Pgvector;

namespace Hivesharp.Storage.Postgres;

internal sealed class PostgresVectorStore(
    NpgsqlDataSource dataSource,
    PostgresTableBuilder tables,
    PostgresStorageOptions options) : IVectorStore
{
    public async Task CreateIndexAsync(string indexName, int dimensions, CancellationToken cancellationToken = default)
    {
        if (dimensions <= 0)
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Dimensions must be positive.");

        var table = tables.VectorTable(indexName);
        var tableUq = tables.VectorTableUnqualified(indexName);

        var ddl = $"""
            CREATE TABLE IF NOT EXISTS {table} (
                id        text PRIMARY KEY,
                embedding vector({dimensions}) NOT NULL,
                text      text NOT NULL,
                metadata  jsonb NOT NULL
            );
            """;

        var metadataIndexDdl = $"""
            CREATE INDEX IF NOT EXISTS "{tables.IndexName(tableUq, "metadata_gin")}"
                ON {table} USING gin (metadata jsonb_path_ops);
            """;

        var indexDdl = options.VectorIndex switch
        {
            PostgresVectorIndexKind.Hnsw =>
                $"""
                CREATE INDEX IF NOT EXISTS "{tables.IndexName(tableUq, "hnsw")}"
                    ON {table} USING hnsw (embedding vector_cosine_ops)
                    WITH (m = {options.HnswOptions.M}, ef_construction = {options.HnswOptions.EfConstruction});
                """,
            PostgresVectorIndexKind.IvfFlat =>
                $"""
                CREATE INDEX IF NOT EXISTS "{tables.IndexName(tableUq, "ivf")}"
                    ON {table} USING ivfflat (embedding vector_cosine_ops)
                    WITH (lists = {options.IvfFlatOptions.Lists});
                """,
            _ => string.Empty
        };

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(ddl + "\n" + indexDdl + "\n" + metadataIndexDdl, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> HasIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        PostgresTableBuilder.ValidateIdentifier(indexName, nameof(indexName));
        var tableUq = tables.VectorTableUnqualified(indexName);

        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = @schema AND table_name = @table
            );
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("schema", tables.Schema);
        cmd.Parameters.AddWithValue("table", tableUq);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    public async Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        var table = tables.VectorTable(indexName);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand($"DROP TABLE IF EXISTS {table};", connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertAsync(string indexName, IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default)
    {
        if (records.Count == 0) return;

        var table = tables.VectorTable(indexName);
        var sql = $"""
            INSERT INTO {table} (id, embedding, text, metadata)
            VALUES (@id, @embedding, @text, @metadata)
            ON CONFLICT (id) DO UPDATE SET
                embedding = EXCLUDED.embedding,
                text      = EXCLUDED.text,
                metadata  = EXCLUDED.metadata;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var record in records)
        {
            await using var cmd = new NpgsqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("id", record.Id);
            cmd.Parameters.AddWithValue("embedding", new Vector(record.Embedding));
            cmd.Parameters.AddWithValue("text", record.Text);
            cmd.Parameters.Add(new NpgsqlParameter("metadata", NpgsqlDbType.Jsonb)
            {
                Value = JsonSerializer.Serialize(record.Metadata)
            });
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> QueryAsync(
        string indexName, float[] queryEmbedding, int topK = 10, IReadOnlyDictionary<string, object?>? filter = null, CancellationToken cancellationToken = default)
    {
        var table = tables.VectorTable(indexName);
        var hasFilter = filter is { Count: > 0 };

        // <=> is the cosine distance operator. Score = 1 - distance, so higher is better
        // (matches InMemoryVectorStore.CosineSimilarity semantics).
        // metadata @> @filter::jsonb is a JSONB containment match -- AND-equality across keys.
        var whereClause = hasFilter ? "WHERE metadata @> @filter::jsonb" : string.Empty;
        var sql = $"""
            SELECT id, text, metadata, 1 - (embedding <=> @query) AS score
            FROM {table}
            {whereClause}
            ORDER BY embedding <=> @query
            LIMIT @topK;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("query", new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue("topK", topK);
        if (hasFilter)
        {
            cmd.Parameters.Add(new NpgsqlParameter("filter", NpgsqlDbType.Jsonb)
            {
                Value = JsonSerializer.Serialize(filter)
            });
        }

        var results = new List<VectorSearchResult>(topK);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetString(0);
            var text = reader.GetString(1);
            var metadataJson = reader.GetString(2);
            var score = reader.GetDouble(3);

            var metadata = string.IsNullOrEmpty(metadataJson)
                ? new Dictionary<string, object?>()
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(metadataJson) ?? new();

            results.Add(new VectorSearchResult
            {
                Id = id,
                Text = text,
                Score = score,
                Metadata = metadata
            });
        }

        return results;
    }

    public async Task DeleteAsync(string indexName, IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return;

        var table = tables.VectorTable(indexName);
        var sql = $"DELETE FROM {table} WHERE id = ANY(@ids);";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("ids", ids.ToArray());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
