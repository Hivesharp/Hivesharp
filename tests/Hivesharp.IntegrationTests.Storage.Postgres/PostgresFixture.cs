using Hivesharp.Storage.Postgres;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hivesharp.IntegrationTests.Storage.Postgres;

public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("hivesharp_test")
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        var b = new NpgsqlDataSourceBuilder(Container.GetConnectionString());
        b.UseVector();
        DataSource = b.Build();
    }

    public async Task DisposeAsync()
    {
        if (DataSource is not null) await DataSource.DisposeAsync();
        await Container.DisposeAsync();
    }

    /// <summary>
    /// Per-test TablePrefix isolation — short, valid identifier (starts with letter, [a-zA-Z0-9_], &lt;48 chars).
    /// Avoids collisions when tests run concurrently against a shared container.
    /// </summary>
    public static string NewPrefix() => "t" + Guid.NewGuid().ToString("N")[..16];

    internal async Task<(PostgresTableBuilder tables, PostgresStorageOptions opts)> InitializeSchemaAsync()
    {
        var prefix = NewPrefix();
        var opts = new PostgresStorageOptions { TablePrefix = prefix };
        var tables = new PostgresTableBuilder(opts.Schema, opts.TablePrefix);
        var initializer = new PostgresSchemaInitializer(DataSource, tables, opts);
        await initializer.StartAsync(CancellationToken.None);
        return (tables, opts);
    }
}

[CollectionDefinition("postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
