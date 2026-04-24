using Npgsql;

namespace Hivesharp.Storage.Postgres;

public class PostgresStorageOptions
{
    /// <summary>
    /// Postgres connection string. Required if <see cref="DataSource"/> is not provided.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Pre-built <see cref="NpgsqlDataSource"/>. Useful when the application owns the data source
    /// (e.g. shared with other services). When provided, Hivesharp will reuse it and will NOT dispose it.
    /// If not provided, Hivesharp builds its own data source from <see cref="ConnectionString"/>.
    /// </summary>
    public NpgsqlDataSource? DataSource { get; set; }

    /// <summary>
    /// Postgres schema. Defaults to <c>public</c>.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// Prefix applied to every table created by Hivesharp. Defaults to <c>hivesharp</c>.
    /// </summary>
    public string TablePrefix { get; set; } = "hivesharp";

    /// <summary>
    /// ANN index kind to attach to each vector index table. Defaults to <see cref="PostgresVectorIndexKind.Hnsw"/>.
    /// </summary>
    public PostgresVectorIndexKind VectorIndex { get; set; } = PostgresVectorIndexKind.Hnsw;

    /// <summary>
    /// HNSW tuning parameters. Used only when <see cref="VectorIndex"/> is <see cref="PostgresVectorIndexKind.Hnsw"/>.
    /// </summary>
    public HnswIndexOptions HnswOptions { get; set; } = new();

    /// <summary>
    /// IVFFlat tuning parameters. Used only when <see cref="VectorIndex"/> is <see cref="PostgresVectorIndexKind.IvfFlat"/>.
    /// </summary>
    public IvfFlatIndexOptions IvfFlatOptions { get; set; } = new();

    /// <summary>
    /// When true, Hivesharp will run idempotent DDL on startup (CREATE EXTENSION, CREATE TABLE IF NOT EXISTS).
    /// Set to false when the schema is managed externally (e.g. migrations). Defaults to true.
    /// </summary>
    public bool AutoInitializeSchema { get; set; } = true;
}

public enum PostgresVectorIndexKind
{
    None = 0,
    Hnsw = 1,
    IvfFlat = 2
}

public class HnswIndexOptions
{
    /// <summary>Maximum number of connections per layer. pgvector default: 16.</summary>
    public int M { get; set; } = 16;

    /// <summary>Size of the dynamic candidate list during construction. pgvector default: 64.</summary>
    public int EfConstruction { get; set; } = 64;
}

public class IvfFlatIndexOptions
{
    /// <summary>Number of inverted lists. pgvector default: 100.</summary>
    public int Lists { get; set; } = 100;
}
